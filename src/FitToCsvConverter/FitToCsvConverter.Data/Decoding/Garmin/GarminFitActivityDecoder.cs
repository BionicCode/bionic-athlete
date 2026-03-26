namespace FitToCsvConverter.Data.Decoding.Garmin;

using System.Collections.Immutable;
using Dynastream.Fit;
using FitFileType = Dynastream.Fit.File;
using FitToCsvConverter.Data.Activities;
using FitToCsvConverter.Data.Fields;

public sealed class GarminFitActivityDecoder : IFitActivityDecoder
{
    public async Task<FitActivityDecodeResult> DecodeFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        string fullPath = Path.GetFullPath(filePath);
        FileInfo fileInfo = new(fullPath);
        if (!fileInfo.Exists)
        {
            throw new FileNotFoundException($"FIT file '{fullPath}' does not exist.", fullPath);
        }

        FitFileSource source = new(fileInfo.Name, fileInfo.FullName, fileInfo.Length, fileInfo.LastWriteTimeUtc);
        await using FileStream fitStream = new(
            fullPath,
            new FileStreamOptions
            {
                Mode = FileMode.Open,
                Access = FileAccess.Read,
                Share = FileShare.Read,
                Options = FileOptions.Asynchronous | FileOptions.SequentialScan
            });

        return DecodePreparedStream(fitStream, source, cancellationToken);
    }

    public async Task<FitActivityDecodeResult> DecodeAsync(Stream stream, string? sourceName = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        Stream preparedStream = await EnsureSeekableStreamAsync(stream, cancellationToken).ConfigureAwait(false);
        bool disposePreparedStream = !ReferenceEquals(preparedStream, stream);

        try
        {
            long? contentLength = preparedStream.CanSeek ? preparedStream.Length - preparedStream.Position : null;
            FitFileSource source = new(
                string.IsNullOrWhiteSpace(sourceName) ? "stream" : sourceName.Trim(),
                contentLength: contentLength);

            return DecodePreparedStream(preparedStream, source, cancellationToken);
        }
        finally
        {
            if (disposePreparedStream)
            {
                await preparedStream.DisposeAsync().ConfigureAwait(false);
            }
        }
    }

    private static FitActivityDecodeResult DecodePreparedStream(
        Stream fitStream,
        FitFileSource source,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(fitStream);
        ArgumentNullException.ThrowIfNull(source);

        cancellationToken.ThrowIfCancellationRequested();

        if (!fitStream.CanSeek)
        {
            throw new InvalidOperationException("FIT decoding requires a seekable stream.");
        }

        Decode decode = new();
        if (!decode.IsFIT(fitStream))
        {
            return CreateFailureResult(source, "The supplied input is not a FIT file.");
        }

        if (!decode.CheckIntegrity(fitStream))
        {
            string message = decode.InvalidDataSize
                ? "The FIT file header reported an invalid data size."
                : "The FIT file failed FIT SDK integrity validation.";

            return CreateFailureResult(source, message);
        }

        GarminMessageCollector collector = new();
        decode.MesgEvent += collector.OnMesg;

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!decode.Read(fitStream))
            {
                return CreateFailureResult(source, "The Garmin FIT SDK could not decode the FIT file.");
            }
        }
        catch (FitException fitException)
        {
            return CreateFailureResult(source, $"The Garmin FIT SDK failed to decode the FIT file: {fitException.Message}");
        }
        finally
        {
            decode.MesgEvent -= collector.OnMesg;
        }

        return BuildActivity(collector, source, cancellationToken);
    }

    private static FitActivityDecodeResult BuildActivity(
        GarminMessageCollector collector,
        FitFileSource source,
        CancellationToken cancellationToken)
    {
        FitMessages fitMessages = collector.FitListener.FitMessages;
        List<FitDecodeIssue> issues = [];

        FileIdMesg? fileIdMessage = fitMessages.FileIdMesgs.FirstOrDefault();
        if (fileIdMessage is null)
        {
            return CreateFailureResult(source, "The FIT file does not contain a FileId message.");
        }

        if (fileIdMessage.GetType() != FitFileType.Activity)
        {
            return CreateFailureResult(source, $"The FIT file type '{fileIdMessage.GetType()}' is not supported. Only activity FIT files are supported.");
        }

        if (fitMessages.ActivityMesgs.Count == 0)
        {
            return CreateFailureResult(source, "The FIT file does not contain an Activity message.");
        }

        if (fitMessages.SessionMesgs.Count == 0)
        {
            return CreateFailureResult(source, "The FIT file does not contain any Session messages.");
        }

        if (fitMessages.RecordMesgs.Count == 0)
        {
            return CreateFailureResult(source, "The FIT file does not contain any Record messages.");
        }

        if (fitMessages.LapMesgs.Count == 0)
        {
            issues.Add(new FitDecodeIssue(FitDecodeIssueSeverity.Warning, "The FIT file does not contain any Lap messages."));
        }

        ActivityMesg activityMessage = fitMessages.ActivityMesgs[^1];
        if (fitMessages.ActivityMesgs.Count > 1)
        {
            issues.Add(new FitDecodeIssue(FitDecodeIssueSeverity.Warning, $"The FIT file contains {fitMessages.ActivityMesgs.Count} Activity messages. The last Activity message was used."));
        }

        ushort? declaredSessionCount = activityMessage.GetNumSessions();
        if (declaredSessionCount is ushort sessionCount && sessionCount != fitMessages.SessionMesgs.Count)
        {
            issues.Add(new FitDecodeIssue(FitDecodeIssueSeverity.Warning, $"Activity declared {sessionCount} session(s), but {fitMessages.SessionMesgs.Count} Session message(s) were decoded."));
        }

        GarminDeveloperFieldCatalog developerFieldCatalog = GarminDeveloperFieldCatalog.Create(fitMessages);
        GarminFieldMapper fieldMapper = new(developerFieldCatalog);

        List<SessionContext> sessionContexts = BuildSessionContexts(fitMessages.SessionMesgs, fieldMapper, cancellationToken);
        List<LapContext> lapContexts = BuildLapContexts(fitMessages.LapMesgs, fieldMapper, cancellationToken);

        AssignLapsToSessions(sessionContexts, lapContexts, issues, cancellationToken);
        AssignRecordsToSessions(sessionContexts, fitMessages.RecordMesgs, fieldMapper, issues, cancellationToken);

        ImmutableArray<FitSession> sessions = sessionContexts
            .OrderBy(context => context.OriginalOrder)
            .Select(context => context.Build())
            .ToImmutableArray();

        FitNodeSnapshot activitySnapshot = CreateActivitySnapshot(activityMessage, fitMessages.ActivityMesgs.Count - 1, fileIdMessage);
        FitActivity activity = new(
            activitySnapshot,
            fieldMapper.MapFields(activityMessage, FitNodeType.Activity),
            sessions,
            source,
            BuildAncillaryData(collector.OrderedMessages, fieldMapper, cancellationToken));

        return new FitActivityDecodeResult(activity, source, issues.ToImmutableArray());
    }

    private static List<SessionContext> BuildSessionContexts(
        IReadOnlyList<SessionMesg> sessionMessages,
        GarminFieldMapper fieldMapper,
        CancellationToken cancellationToken)
    {
        List<SessionContext> sessionContexts = new(sessionMessages.Count);
        for (int index = 0; index < sessionMessages.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            SessionMesg sessionMessage = sessionMessages[index];
            sessionContexts.Add(
                new SessionContext(
                    CreateSessionSnapshot(sessionMessage, index),
                    fieldMapper.MapFields(sessionMessage, FitNodeType.Session),
                    sessionMessage.GetFirstLapIndex(),
                    sessionMessage.GetNumLaps(),
                    index));
        }

        return sessionContexts;
    }

    private static List<LapContext> BuildLapContexts(
        IReadOnlyList<LapMesg> lapMessages,
        GarminFieldMapper fieldMapper,
        CancellationToken cancellationToken)
    {
        List<LapContext> lapContexts = new(lapMessages.Count);
        for (int index = 0; index < lapMessages.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            LapMesg lapMessage = lapMessages[index];
            FitLap lap = new(CreateLapSnapshot(lapMessage, index), fieldMapper.MapFields(lapMessage, FitNodeType.Lap));
            lapContexts.Add(new LapContext(lap, index));
        }

        return lapContexts;
    }

    private static void AssignLapsToSessions(
        List<SessionContext> sessionContexts,
        List<LapContext> lapContexts,
        List<FitDecodeIssue> issues,
        CancellationToken cancellationToken)
    {
        Dictionary<ushort, LapContext> lapsByMessageIndex = lapContexts
            .Where(context => context.MessageIndex is ushort)
            .GroupBy(context => context.MessageIndex!.Value)
            .ToDictionary(group => group.Key, group => group.First());

        foreach (SessionContext sessionContext in sessionContexts)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (sessionContext.FirstLapIndex is not ushort firstLapIndex || sessionContext.LapCount is not ushort lapCount)
            {
                continue;
            }

            for (int offset = 0; offset < lapCount; offset++)
            {
                ushort lapMessageIndex = (ushort)(firstLapIndex + offset);
                if (lapsByMessageIndex.TryGetValue(lapMessageIndex, out LapContext? lapContext) && !lapContext.IsAssigned)
                {
                    sessionContext.Laps.Add(lapContext.Lap);
                    lapContext.IsAssigned = true;
                }
            }
        }

        ImmutableArray<SessionProjection> sessionProjections = BuildSessionProjections(sessionContexts);
        int fallbackProjectionIndex = 0;

        foreach (LapContext lapContext in lapContexts.Where(context => !context.IsAssigned))
        {
            cancellationToken.ThrowIfCancellationRequested();

            SessionResolution resolution = ResolveSession(lapContext.StartTimeUtc, sessionProjections, fallbackProjectionIndex);
            fallbackProjectionIndex = resolution.ProjectionIndex;
            resolution.Context.Laps.Add(lapContext.Lap);
            lapContext.IsAssigned = true;
        }

        if (lapContexts.Any(context => !context.IsAssigned))
        {
            issues.Add(new FitDecodeIssue(FitDecodeIssueSeverity.Warning, "One or more Lap messages could not be assigned to a Session."));
        }
    }

    private static void AssignRecordsToSessions(
        List<SessionContext> sessionContexts,
        IReadOnlyList<RecordMesg> recordMessages,
        GarminFieldMapper fieldMapper,
        List<FitDecodeIssue> issues,
        CancellationToken cancellationToken)
    {
        ImmutableArray<SessionProjection> sessionProjections = BuildSessionProjections(sessionContexts);
        int fallbackProjectionIndex = 0;
        bool addedMissingTimestampWarning = false;
        bool addedBeforeFirstSessionWarning = false;

        for (int index = 0; index < recordMessages.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            RecordMesg recordMessage = recordMessages[index];
            FitRecord record = new(CreateRecordSnapshot(recordMessage, index), fieldMapper.MapFields(recordMessage, FitNodeType.Record));

            SessionResolution resolution = ResolveSession(record.Original.CanonicalTimestampUtc, sessionProjections, fallbackProjectionIndex);
            fallbackProjectionIndex = resolution.ProjectionIndex;
            resolution.Context.Records.Add(record);

            if (record.Original.CanonicalTimestampUtc is null && sessionContexts.Count > 1 && !addedMissingTimestampWarning)
            {
                issues.Add(new FitDecodeIssue(FitDecodeIssueSeverity.Warning, "At least one Record message was missing a timestamp and was assigned by sequence order."));
                addedMissingTimestampWarning = true;
            }

            if (resolution.WasBeforeFirstSession && sessionContexts.Count > 1 && !addedBeforeFirstSessionWarning)
            {
                issues.Add(new FitDecodeIssue(FitDecodeIssueSeverity.Warning, "At least one Record timestamp preceded the first Session start time and was assigned to the first Session."));
                addedBeforeFirstSessionWarning = true;
            }
        }
    }

    private static ImmutableArray<SessionProjection> BuildSessionProjections(List<SessionContext> sessionContexts)
        => sessionContexts
            .Select(
                context => new SessionProjection(
                    context,
                    context.Original.CanonicalTimestampUtc,
                    context.OriginalOrder))
            .OrderBy(projection => projection.StartTimeUtc ?? DateTimeOffset.MaxValue)
            .ThenBy(projection => projection.SortOrder)
            .ToImmutableArray();

    private static SessionResolution ResolveSession(
        DateTimeOffset? timestampUtc,
        ImmutableArray<SessionProjection> sessionProjections,
        int fallbackProjectionIndex)
    {
        if (sessionProjections.IsDefaultOrEmpty)
        {
            throw new InvalidOperationException("Record and lap assignment requires at least one Session.");
        }

        if (timestampUtc is null)
        {
            int projectionIndex = Math.Clamp(fallbackProjectionIndex, 0, sessionProjections.Length - 1);
            return new SessionResolution(sessionProjections[projectionIndex].Context, projectionIndex, WasBeforeFirstSession: false);
        }

        int firstKnownProjectionIndex = -1;
        for (int index = 0; index < sessionProjections.Length; index++)
        {
            if (sessionProjections[index].StartTimeUtc is not null)
            {
                firstKnownProjectionIndex = index;
                break;
            }
        }

        if (firstKnownProjectionIndex < 0)
        {
            int projectionIndex = Math.Clamp(fallbackProjectionIndex, 0, sessionProjections.Length - 1);
            return new SessionResolution(sessionProjections[projectionIndex].Context, projectionIndex, WasBeforeFirstSession: false);
        }

        if (timestampUtc < sessionProjections[firstKnownProjectionIndex].StartTimeUtc)
        {
            return new SessionResolution(sessionProjections[firstKnownProjectionIndex].Context, firstKnownProjectionIndex, WasBeforeFirstSession: true);
        }

        for (int index = firstKnownProjectionIndex; index < sessionProjections.Length; index++)
        {
            SessionProjection currentProjection = sessionProjections[index];
            if (currentProjection.StartTimeUtc is not DateTimeOffset currentStartTimeUtc)
            {
                continue;
            }

            DateTimeOffset? nextKnownStartTimeUtc = null;
            for (int nextIndex = index + 1; nextIndex < sessionProjections.Length; nextIndex++)
            {
                if (sessionProjections[nextIndex].StartTimeUtc is DateTimeOffset knownStartTimeUtc)
                {
                    nextKnownStartTimeUtc = knownStartTimeUtc;
                    break;
                }
            }

            if (timestampUtc >= currentStartTimeUtc
                && (nextKnownStartTimeUtc is null || timestampUtc < nextKnownStartTimeUtc.Value))
            {
                return new SessionResolution(currentProjection.Context, index, WasBeforeFirstSession: false);
            }
        }

        return new SessionResolution(sessionProjections[^1].Context, sessionProjections.Length - 1, WasBeforeFirstSession: false);
    }

    private static FitActivityAncillaryData BuildAncillaryData(
        IReadOnlyList<Mesg> orderedMessages,
        GarminFieldMapper fieldMapper,
        CancellationToken cancellationToken)
    {
        ImmutableArray<FitAncillaryMessage>.Builder builder = ImmutableArray.CreateBuilder<FitAncillaryMessage>();
        int sequenceNumber = 0;

        foreach (Mesg message in orderedMessages)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (message.Num is (ushort)MesgNum.Activity or (ushort)MesgNum.Session or (ushort)MesgNum.Lap or (ushort)MesgNum.Record)
            {
                continue;
            }

            builder.Add(
                new FitAncillaryMessage(
                    CreateAncillarySnapshot(message, sequenceNumber),
                    fieldMapper.MapFieldSnapshots(message, FitNodeType.Ancillary)));

            sequenceNumber++;
        }

        return builder.Count == 0
            ? FitActivityAncillaryData.Empty
            : new FitActivityAncillaryData(builder.ToImmutable());
    }

    private static FitNodeSnapshot CreateActivitySnapshot(ActivityMesg activityMessage, int sequenceNumber, FileIdMesg fileIdMessage)
        => new(
            new FitNodeIdentity(FitNodeType.Activity, sequenceNumber, null),
            activityMessage.Num,
            activityMessage.Name,
            activityMessage.LocalNum,
            TryGetUtc(activityMessage.GetTimestamp()) ?? TryGetUtc(fileIdMessage.GetTimeCreated()),
            startTimeUtc: null);

    private static FitNodeSnapshot CreateSessionSnapshot(SessionMesg sessionMessage, int sequenceNumber)
        => new(
            new FitNodeIdentity(FitNodeType.Session, sequenceNumber, sessionMessage.GetMessageIndex()),
            sessionMessage.Num,
            sessionMessage.Name,
            sessionMessage.LocalNum,
            TryGetUtc(sessionMessage.GetTimestamp()),
            TryGetUtc(sessionMessage.GetStartTime()));

    private static FitNodeSnapshot CreateLapSnapshot(LapMesg lapMessage, int sequenceNumber)
        => new(
            new FitNodeIdentity(FitNodeType.Lap, sequenceNumber, lapMessage.GetMessageIndex()),
            lapMessage.Num,
            lapMessage.Name,
            lapMessage.LocalNum,
            TryGetUtc(lapMessage.GetTimestamp()),
            TryGetUtc(lapMessage.GetStartTime()));

    private static FitNodeSnapshot CreateRecordSnapshot(RecordMesg recordMessage, int sequenceNumber)
        => new(
            new FitNodeIdentity(FitNodeType.Record, sequenceNumber, MessageIndex: null),
            recordMessage.Num,
            recordMessage.Name,
            recordMessage.LocalNum,
            TryGetUtc(recordMessage.GetTimestamp()),
            startTimeUtc: null);

    private static FitNodeSnapshot CreateAncillarySnapshot(Mesg message, int sequenceNumber)
        => new(
            new FitNodeIdentity(FitNodeType.Ancillary, sequenceNumber, TryGetMessageIndex(message)),
            message.Num,
            string.IsNullOrWhiteSpace(message.Name) ? $"mesg_{message.Num}" : message.Name,
            message.LocalNum,
            TryGetUtcFromField(message, 253),
            startTimeUtc: null);

    private static DateTimeOffset? TryGetUtc(Dynastream.Fit.DateTime? fitDateTime)
    {
        if (fitDateTime is null || fitDateTime.GetTimeStamp() < 0x10000000)
        {
            return null;
        }

        return new DateTimeOffset(fitDateTime.GetDateTime());
    }

    private static DateTimeOffset? TryGetUtcFromField(Mesg message, byte fieldNumber)
    {
        Field? field = message.GetField(fieldNumber);
        if (field?.GetRawValue(0) is not object rawValue)
        {
            return null;
        }

        if (!TryConvertToUInt32(rawValue, out uint timestamp) || timestamp < 0x10000000)
        {
            return null;
        }

        return new DateTimeOffset(new Dynastream.Fit.DateTime(timestamp).GetDateTime());
    }

    private static ushort? TryGetMessageIndex(Mesg message)
    {
        Field? field = message.GetField(254);
        if (field?.GetRawValue(0) is not object rawValue)
        {
            return null;
        }

        try
        {
            return Convert.ToUInt16(rawValue);
        }
        catch
        {
            return null;
        }
    }

    private static bool TryConvertToUInt32(object value, out uint result)
    {
        try
        {
            result = Convert.ToUInt32(value);
            return true;
        }
        catch
        {
            result = default;
            return false;
        }
    }

    private static FitActivityDecodeResult CreateFailureResult(FitFileSource source, string message)
        => new(
            activity: null,
            source,
            ImmutableArray.Create(new FitDecodeIssue(FitDecodeIssueSeverity.Error, message)));

    private static async Task<Stream> EnsureSeekableStreamAsync(Stream stream, CancellationToken cancellationToken)
    {
        if (stream.CanSeek)
        {
            return stream;
        }

        MemoryStream bufferedStream = new();
        await stream.CopyToAsync(bufferedStream, cancellationToken).ConfigureAwait(false);
        bufferedStream.Position = 0;
        return bufferedStream;
    }

    private sealed class GarminMessageCollector
    {
        public FitListener FitListener { get; } = new();

        public List<Mesg> OrderedMessages { get; } = [];

        public void OnMesg(object? sender, MesgEventArgs eventArgs)
        {
            FitListener.OnMesg(sender, eventArgs);
            OrderedMessages.Add(new Mesg(eventArgs.mesg));
        }
    }

    private sealed class SessionContext
    {
        public SessionContext(
            FitNodeSnapshot original,
            ImmutableArray<FitField> fields,
            ushort? firstLapIndex,
            ushort? lapCount,
            int originalOrder)
        {
            Original = original;
            Fields = fields;
            FirstLapIndex = firstLapIndex;
            LapCount = lapCount;
            OriginalOrder = originalOrder;
        }

        public FitNodeSnapshot Original { get; }

        public ImmutableArray<FitField> Fields { get; }

        public ushort? FirstLapIndex { get; }

        public ushort? LapCount { get; }

        public int OriginalOrder { get; }

        public ImmutableArray<FitLap>.Builder Laps { get; } = ImmutableArray.CreateBuilder<FitLap>();

        public ImmutableArray<FitRecord>.Builder Records { get; } = ImmutableArray.CreateBuilder<FitRecord>();

        public FitSession Build() => new(Original, Fields, Laps.ToImmutable(), Records.ToImmutable());
    }

    private sealed class LapContext
    {
        public LapContext(FitLap lap, int originalOrder)
        {
            Lap = lap;
            OriginalOrder = originalOrder;
        }

        public FitLap Lap { get; }

        public int OriginalOrder { get; }

        public ushort? MessageIndex => Lap.Original.Identity.MessageIndex;

        public DateTimeOffset? StartTimeUtc => Lap.Original.CanonicalTimestampUtc;

        public bool IsAssigned { get; set; }
    }

    private sealed record SessionProjection(SessionContext Context, DateTimeOffset? StartTimeUtc, int SortOrder);

    private sealed record SessionResolution(SessionContext Context, int ProjectionIndex, bool WasBeforeFirstSession);
}
