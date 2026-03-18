namespace FitToCsvConverter.Data;

using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using BionicCode.Utilities.Net;
using Dynastream.Fit;
using DateTime = DateTime;

public static class FitFileAnalyzer
{
    private static readonly Dictionary<string, ImmutableDictionary<FitSessionFieldInfo, Field>> s_sessionsCache = [];
    private static TaskCompletionSource<IEnumerable<FitSessionFieldInfo>> s_sessionFieldsTcs = new();
    private static string? s_currentFitFile;

    public static void ExportSessionFieldsToCsvDebug(string fitFilePath, string csvPath)
    {
        using var fitStream = new FileStream(fitFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);

        var decoder = new Decode();
        var broadcaster = new MesgBroadcaster();

        decoder.MesgEvent += broadcaster.OnMesg;
        decoder.MesgDefinitionEvent += broadcaster.OnMesgDefinition;

        using var writer = new StreamWriter(csvPath, false, Encoding.UTF8);
        writer.WriteLine("MesgName,FieldNum,FieldName,ValueIndex,DecodedValue,RawValue,ValueType,Units,Scale,Offset,ProfileType");

        broadcaster.SessionMesgEvent += (_, e) =>
        {
            var session = new SessionMesg(e.mesg);

            // Example of typed access:
            Dynastream.Fit.DateTime? startTime = session.GetStartTime();
            float? totalElapsed = session.GetTotalElapsedTime();
            float? avgSpeed = session.GetAvgSpeed();

            // Generic export of all fields for inspection:
            foreach (Field field in session.Fields)
            {
                if (field.Name == "unknown")
                {
                    // Optional: skip unsupported/unknown profile fields
                    // continue;
                }

                int valueCount = field.GetNumValues();
                if (valueCount == 0)
                {
                    WriteRow(
                        writer,
                        "session",
                        field.Num.ToString(CultureInfo.InvariantCulture),
                        field.Name,
                        "",
                        "",
                        "",
                        "",
                        field.Units,
                        field.Scale.ToString(CultureInfo.InvariantCulture),
                        field.Offset.ToString(CultureInfo.InvariantCulture),
                        field.ProfileType.ToString());
                    continue;
                }

                for (int i = 0; i < valueCount; i++)
                {
                    // Main-field decoding:
                    object? decoded = field.GetValue(i);

                    // Raw stored value:
                    object? raw = field.GetRawValue(i);

                    WriteRow(
                        writer,
                        "session",
                        field.Num.ToString(CultureInfo.InvariantCulture),
                        field.Name,
                        i.ToString(CultureInfo.InvariantCulture),
                        FormatValue(decoded),
                        FormatValue(raw),
                        decoded?.GetType().FullName ?? raw?.GetType().FullName ?? "",
                        field.Units,
                        field.Scale.ToString(CultureInfo.InvariantCulture),
                        field.Offset.ToString(CultureInfo.InvariantCulture),
                        field.ProfileType.ToString());
                }
            }

            // Optional: a tiny human summary at the end
            writer.WriteLine();
            writer.WriteLine("Summary");
            writer.WriteLine($"StartTime,{Escape(FormatValue(startTime))}");
            writer.WriteLine($"TotalElapsedTime_s,{Escape(FormatValue(totalElapsed))}");
            writer.WriteLine($"AvgSpeed_mps,{Escape(FormatValue(avgSpeed))}");
        };

        _ = decoder.Read(fitStream);
    }

    public static void ExportSessionFieldsToCsv(string fitFilePath, string csvPath, IEnumerable<FitSessionFieldInfo> sessionFields)
    {
        using var fitStream = new FileStream(fitFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);

        var decoder = new Decode();
        var broadcaster = new MesgBroadcaster();

        decoder.MesgEvent += broadcaster.OnMesg;
        decoder.MesgDefinitionEvent += broadcaster.OnMesgDefinition;

        using var writer = new StreamWriter(csvPath, false, Encoding.UTF8);
        writer.WriteLine("MesgName,FieldNum,FieldName,ValueIndex,DecodedValue,RawValue,ValueType,Units,Scale,Offset,ProfileType");

        broadcaster.SessionMesgEvent += (_, e) =>
        {
            var session = new SessionMesg(e.mesg);

            // Example of typed access:
            Dynastream.Fit.DateTime? startTime = session.GetStartTime();
            float? totalElapsed = session.GetTotalElapsedTime();
            float? avgSpeed = session.GetAvgSpeed();

            // Generic export of all fields for inspection:
        };
        foreach (FitSessionFieldInfo field in sessionFields)
        {
            int valueCount = field.GetNumValues();
            if (valueCount == 0)
            {
                WriteRow(
                    writer,
                    "session",
                    field.Num.ToString(CultureInfo.InvariantCulture),
                    field.Name,
                    "",
                    "",
                    "",
                    "",
                    field.Units,
                    field.Scale.ToString(CultureInfo.InvariantCulture),
                    field.Offset.ToString(CultureInfo.InvariantCulture),
                    field.ProfileType.ToString());
                continue;
            }

            for (int i = 0; i < valueCount; i++)
            {
                // Main-field decoding:
                object? decoded = field.GetValue(i);

                // Raw stored value:
                object? raw = field.GetRawValue(i);

                WriteRow(
                    writer,
                    "session",
                    field.Num.ToString(CultureInfo.InvariantCulture),
                    field.Name,
                    i.ToString(CultureInfo.InvariantCulture),
                    FormatValue(decoded),
                    FormatValue(raw),
                    decoded?.GetType().FullName ?? raw?.GetType().FullName ?? "",
                    field.Units,
                    field.Scale.ToString(CultureInfo.InvariantCulture),
                    field.Offset.ToString(CultureInfo.InvariantCulture),
                    field.ProfileType.ToString());
            }
        }

        // Optional: a tiny human summary at the end
        writer.WriteLine();
        writer.WriteLine("Summary");
        writer.WriteLine($"StartTime,{Escape(FormatValue(startTime))}");
        writer.WriteLine($"TotalElapsedTime_s,{Escape(FormatValue(totalElapsed))}");
        writer.WriteLine($"AvgSpeed_mps,{Escape(FormatValue(avgSpeed))}");
    }

    public static async Task<IEnumerable<FitSessionFieldInfo>> GetAvailableSessionFieldsAsync(string fitFilePath, CancellationToken cancellationToken = default)
    {
        ArgumentExceptionAdvanced.ThrowIfNullOrWhiteSpace(fitFilePath);

        s_currentFitFile = fitFilePath;

        if (s_sessionsCache.TryGetValue(s_currentFitFile, out ImmutableDictionary<FitSessionFieldInfo, Field>? sessionInfoMap))
        {
            IEnumerable<FitSessionFieldInfo> sessionFields = sessionInfoMap.Keys;

            return sessionFields;
        }

        await using var fitStream = new FileStream(fitFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);

        var decoder = new Decode();
        var broadcaster = new MesgBroadcaster();

        decoder.MesgEvent += broadcaster.OnMesg;
        decoder.MesgDefinitionEvent += broadcaster.OnMesgDefinition;

        broadcaster.SessionMesgEvent += OnSessionsReady;

        s_sessionFieldsTcs = new TaskCompletionSource<IEnumerable<FitSessionFieldInfo>>();
        _ = decoder.Read(fitStream);

        return await s_sessionFieldsTcs.Task.ConfigureAwait(false);
    }

    private static void OnSessionsReady(object sender, MesgEventArgs e)
    {
        var session = new SessionMesg(e.mesg);

        // Example of typed access:
        Dynastream.Fit.DateTime? startTime = session.GetStartTime();
        DateTime sessionTime = startTime?.GetDateTime() ?? DateTime.MinValue;
        float totalElapsedInSeconds = session.GetTotalElapsedTime() ?? 0;
        var sessionDuration = TimeSpan.FromSeconds(totalElapsedInSeconds);
        double avgSpeedInMps = session.GetAvgSpeed() ?? 0;

        HashSet<FitSessionFieldInfo> fieldInfoList = [];
        Dictionary<FitSessionFieldInfo, Field> fieldInfoMap = [];

        foreach (Field? field in session.Fields)
        {
            if (field is null || field.Name.Equals("unknown", StringComparison.OrdinalIgnoreCase))
            {
                continue; // Skip unknown fields
            }

            var fieldInfo = new FitSessionFieldInfo(
                fieldId: field.Num,
                fieldName: field.Name,
                sessionTime: sessionTime,
                sessionDuration: sessionDuration,
                averageSpeed: avgSpeedInMps);

            if (fieldInfoList.Add(fieldInfo))
            {
                fieldInfoMap.Add(fieldInfo, field);
            }
        }

        var immutableFieldInfoMap = fieldInfoMap.ToImmutableDictionary();
        s_sessionsCache.Add(s_currentFitFile!, immutableFieldInfoMap);

        s_sessionFieldsTcs.SetResult(fieldInfoList);
    }

    private static string FormatValue(object? value)
        => value switch
        {
            null => "",
            Dynastream.Fit.DateTime fitDateTime => fitDateTime.GetDateTime().ToString("O", CultureInfo.InvariantCulture),
            byte[] bytes => Convert.ToHexString(bytes),
            Enum enumValue => enumValue.ToString(),
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture) ?? "",
            _ => value.ToString() ?? ""
        };

    private static void WriteRow(
        StreamWriter writer,
        string mesgName,
        string fieldNum,
        string fieldName,
        string valueIndex,
        string decodedValue,
        string rawValue,
        string valueType,
        string units,
        string scale,
        string offset,
        string profileType) => writer.WriteLine(string.Join(",",
            Escape(mesgName),
            Escape(fieldNum),
            Escape(fieldName),
            Escape(valueIndex),
            Escape(decodedValue),
            Escape(rawValue),
            Escape(valueType),
            Escape(units),
            Escape(scale),
            Escape(offset),
            Escape(profileType)));

    private static string Escape(string? text)
    {
        text ??= "";
        return "\"" + text.Replace("\"", "\"\"") + "\"";
    }
}

public readonly struct FitSessionFieldInfo
{
    public FitSessionFieldInfo(int fieldId, string fieldName, DateTime sessionTime, TimeSpan sessionDuration, double averageSpeed)
    {
        FieldId = fieldId;
        FieldName = fieldName;
        SessionTime = sessionTime;
        SessionDuration = sessionDuration;
        AverageSpeed = averageSpeed;
        FieldDisplayName = fieldName; // Default display name is the same as the field name, can be overridden later
        IsVisible = true; // Default to visible, can be overridden later
    }

    public int FieldId { get; }
    public string FieldName { get; }
    public string FieldDisplayName { get; init; }
    public bool IsVisible { get; init; }
    public DateTime SessionTime { get; }
    public TimeSpan SessionDuration { get; }
    public double AverageSpeed { get; }
}