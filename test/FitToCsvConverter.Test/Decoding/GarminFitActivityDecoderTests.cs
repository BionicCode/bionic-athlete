namespace FitToCsvConverter.Test.Decoding;

using System.IO;
using Dynastream.Fit;
using FitToCsvConverter.Data.Activities;
using FitToCsvConverter.Data.Decoding;
using FitToCsvConverter.Data.Decoding.Garmin;
using FitToCsvConverter.Data.Fields;
using FitToCsvConverter.Test.Fixtures;

public sealed class GarminFitActivityDecoderTests
{
    [Fact]
    public async Task DecodeAsyncBuildsHierarchyAndPreservesDeveloperFields()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        GarminFitActivityDecoder decoder = new();
        await using MemoryStream fitStream = new(FitTestFileFactory.CreateSingleSessionActivityWithDeveloperFields());

        FitActivityDecodeResult result = await decoder.DecodeAsync(fitStream, "single-session.fit", cancellationToken);

        Assert.True(result.IsSuccess);
        FitActivity activity = Assert.IsType<FitActivity>(result.Activity);
        FitSession session = Assert.Single(activity.Sessions);

        Assert.Equal(2, session.Laps.Length);
        Assert.Equal(3, session.Records.Length);
        Assert.Contains(activity.AncillaryData.Messages, message => message.Original.MessageNumber == (ushort)MesgNum.Event);
        Assert.Contains(activity.AncillaryData.Messages, message => message.Original.MessageNumber == (ushort)MesgNum.FieldDescription);
        Assert.Contains(activity.AncillaryData.Messages, message => message.Original.MessageNumber == (ushort)MesgNum.DeveloperDataId);

        FitField sessionDeveloperField = Assert.Single(session.Fields.Where(field => field.Original.OriginalName == "session_score"));
        Assert.Equal(FitFieldKind.Developer, sessionDeveloperField.Original.Kind);
        Assert.Equal(42U, sessionDeveloperField.Original.DeveloperApplicationVersion);
        Assert.True(sessionDeveloperField.Original.DeveloperApplicationIdBytes.SequenceEqual(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }));

        FitField sessionArrayField = Assert.Single(session.Fields.Where(field => field.Original.OriginalName == "session_steps"));
        Assert.True(sessionArrayField.Original.IsArray);
        Assert.Equal(3, sessionArrayField.Original.OriginalValues.Length);
        Assert.Equal((ushort)10, sessionArrayField.Original.OriginalValues[0].DecodedValue);
        Assert.Equal((ushort)20, sessionArrayField.Original.OriginalValues[1].DecodedValue);
        Assert.Equal((ushort)30, sessionArrayField.Original.OriginalValues[2].DecodedValue);

        FitField lapDeveloperField = Assert.Single(session.Laps[0].Fields.Where(field => field.Original.OriginalName == "lap_score"));
        Assert.Equal(FitFieldKind.Developer, lapDeveloperField.Original.Kind);

        FitField recordDeveloperField = Assert.Single(session.Records[0].Fields.Where(field => field.Original.OriginalName == "record_tag"));
        Assert.Equal(FitFieldKind.Developer, recordDeveloperField.Original.Kind);
        Assert.Equal((ushort)MesgNum.Record, recordDeveloperField.Original.NativeOverrideMessageNumber);
        Assert.Equal(RecordMesg.FieldDefNum.HeartRate, recordDeveloperField.Original.NativeOverrideFieldNumber);
    }

    [Fact]
    public async Task DecodeAsyncUsesCanonicalSessionAndActivityDates()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        GarminFitActivityDecoder decoder = new();
        await using MemoryStream fitStream = new(FitTestFileFactory.CreateSingleSessionActivityWithDeveloperFields());

        FitActivityDecodeResult result = await decoder.DecodeAsync(fitStream, "single-session.fit", cancellationToken);

        FitActivity activity = Assert.IsType<FitActivity>(result.Activity);
        FitSession session = Assert.Single(activity.Sessions);
        DateTimeOffset expectedStartTimeUtc = new(2024, 05, 01, 06, 30, 00, TimeSpan.Zero);

        Assert.Equal(expectedStartTimeUtc, session.CanonicalStartTimeUtc);
        Assert.Equal(expectedStartTimeUtc, activity.CanonicalStartTimeUtc);
        Assert.NotEqual(activity.Original.TimestampUtc, activity.CanonicalStartTimeUtc);
    }

    [Fact]
    public async Task DecodeAsyncAssignsRecordsToTheCorrectSessionAcrossMultipleSessions()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        GarminFitActivityDecoder decoder = new();
        await using MemoryStream fitStream = new(FitTestFileFactory.CreateMultiSessionActivity());

        FitActivityDecodeResult result = await decoder.DecodeAsync(fitStream, "multi-session.fit", cancellationToken);

        FitActivity activity = Assert.IsType<FitActivity>(result.Activity);

        Assert.Equal(2, activity.Sessions.Length);
        Assert.Equal(2, activity.Sessions[0].Records.Length);
        Assert.Equal(2, activity.Sessions[1].Records.Length);
        _ = Assert.Single(activity.Sessions[0].Laps);
        _ = Assert.Single(activity.Sessions[1].Laps);

        Assert.Equal(new DateTimeOffset(2024, 06, 15, 08, 00, 00, TimeSpan.Zero), activity.Sessions[0].Records[0].Original.TimestampUtc);
        Assert.Equal(new DateTimeOffset(2024, 06, 15, 08, 00, 10, TimeSpan.Zero), activity.Sessions[0].Records[1].Original.TimestampUtc);
        Assert.Equal(new DateTimeOffset(2024, 06, 15, 10, 00, 00, TimeSpan.Zero), activity.Sessions[1].Records[0].Original.TimestampUtc);
        Assert.Equal(new DateTimeOffset(2024, 06, 15, 10, 00, 10, TimeSpan.Zero), activity.Sessions[1].Records[1].Original.TimestampUtc);
    }

    [Fact]
    public async Task DecodeFileAsyncPreservesUnknownFieldsFromTheExampleFile()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        GarminFitActivityDecoder decoder = new();

        FitActivityDecodeResult result = await decoder.DecodeFileAsync(FitTestFileFactory.GetExampleFitFilePath(), cancellationToken);

        FitActivity activity = Assert.IsType<FitActivity>(result.Activity);
        bool hasUnknownNodeField = activity.Sessions
            .SelectMany(session => session.Records)
            .SelectMany(record => record.Fields)
            .Any(field => field.Original.Kind == FitFieldKind.Unknown);

        Assert.True(result.IsSuccess);
        Assert.True(hasUnknownNodeField);
    }
}
