namespace FitToCsvConverter.Test.Fixtures;

using System.Collections.Immutable;
using System.Linq;
using FitToCsvConverter.Data.Activities;
using FitToCsvConverter.Data.Decoding;
using FitToCsvConverter.Data.Fields;

internal static class FitActivityModelFactory
{
    public static FitActivity CreateActivityForExport()
    {
        DateTimeOffset activityStartTimeUtc = new(2024, 07, 14, 08, 30, 00, TimeSpan.Zero);

        FitField activitySportField = CreateField(
            FitNodeType.Activity,
            messageNumber: 34,
            fieldNumber: 0,
            originalName: "sport",
            messageName: "activity",
            decodedValues: ["running"]);

        FitField sessionAverageHeartRateField = CreateField(
            FitNodeType.Session,
            messageNumber: 18,
            fieldNumber: 0,
            originalName: "avg_heart_rate",
            messageName: "session",
            decodedValues: [145]);

        FitField lapDistanceField = CreateField(
            FitNodeType.Lap,
            messageNumber: 19,
            fieldNumber: 0,
            originalName: "total_distance",
            messageName: "lap",
            decodedValues: [1000.5]);

        FitRecord firstRecord = CreateRecord(
            sequenceNumber: 0,
            timestampUtc: activityStartTimeUtc,
            heartRate: 140,
            cadence: 85,
            powerZones: [1, 2, 3]);

        FitRecord secondRecord = CreateRecord(
            sequenceNumber: 1,
            timestampUtc: activityStartTimeUtc.AddSeconds(5),
            heartRate: 141,
            cadence: 86,
            powerZones: [4, 5, 6]);

        FitLap lap = new(
            CreateNodeSnapshot(
                FitNodeType.Lap,
                sequenceNumber: 0,
                messageIndex: 0,
                messageNumber: 19,
                messageName: "lap",
                timestampUtc: activityStartTimeUtc.AddMinutes(5),
                startTimeUtc: activityStartTimeUtc),
            ImmutableArray.Create(lapDistanceField));

        FitSession session = new(
            CreateNodeSnapshot(
                FitNodeType.Session,
                sequenceNumber: 0,
                messageIndex: 0,
                messageNumber: 18,
                messageName: "session",
                timestampUtc: activityStartTimeUtc.AddMinutes(10),
                startTimeUtc: activityStartTimeUtc),
            ImmutableArray.Create(sessionAverageHeartRateField),
            ImmutableArray.Create(lap),
            ImmutableArray.Create(firstRecord, secondRecord));

        return new FitActivity(
            CreateNodeSnapshot(
                FitNodeType.Activity,
                sequenceNumber: 0,
                messageIndex: null,
                messageNumber: 34,
                messageName: "activity",
                timestampUtc: activityStartTimeUtc.AddMinutes(15),
                startTimeUtc: null),
            ImmutableArray.Create(activitySportField),
            ImmutableArray.Create(session),
            new FitFileSource("sample.fit"),
            FitActivityAncillaryData.Empty);
    }

    private static FitRecord CreateRecord(
        int sequenceNumber,
        DateTimeOffset timestampUtc,
        int heartRate,
        int cadence,
        int[] powerZones)
    {
        FitField heartRateField = CreateField(
            FitNodeType.Record,
            messageNumber: 20,
            fieldNumber: 0,
            originalName: "heart_rate",
            messageName: "record",
            decodedValues: [heartRate]);

        FitField cadenceField = CreateField(
            FitNodeType.Record,
            messageNumber: 20,
            fieldNumber: 1,
            originalName: "cadence",
            messageName: "record",
            decodedValues: [cadence]);

        FitField powerZonesField = CreateField(
            FitNodeType.Record,
            messageNumber: 20,
            fieldNumber: 2,
            originalName: "power_zones",
            messageName: "record",
            decodedValues: powerZones.Cast<object?>().ToArray());

        return new FitRecord(
            CreateNodeSnapshot(
                FitNodeType.Record,
                sequenceNumber,
                messageIndex: null,
                messageNumber: 20,
                messageName: "record",
                timestampUtc,
                startTimeUtc: null),
            ImmutableArray.Create(heartRateField, cadenceField, powerZonesField));
    }

    private static FitNodeSnapshot CreateNodeSnapshot(
        FitNodeType nodeType,
        int sequenceNumber,
        ushort? messageIndex,
        ushort messageNumber,
        string messageName,
        DateTimeOffset? timestampUtc,
        DateTimeOffset? startTimeUtc)
        => new(
            new FitNodeIdentity(nodeType, sequenceNumber, messageIndex),
            messageNumber,
            messageName,
            localMessageNumber: 0,
            timestampUtc,
            startTimeUtc);

    private static FitField CreateField(
        FitNodeType nodeType,
        ushort messageNumber,
        byte fieldNumber,
        string originalName,
        string messageName,
        params object?[] decodedValues)
    {
        FitFieldKey key = new(nodeType, FitFieldKind.Standard, messageNumber, fieldNumber);
        ImmutableArray<FitFieldValue> originalValues = decodedValues
            .Select(decodedValue => new FitFieldValue(decodedValue, decodedValue))
            .ToImmutableArray();

        return new FitField(
            new FitFieldSnapshot(
                key,
                FitExportColumnKey.FromField(key),
                originalName,
                messageName,
                FitFieldKind.Standard,
                baseType: 0,
                baseTypeName: "test",
                profileTypeName: "Test",
                units: null,
                scale: 1,
                offset: 0,
                isAccumulated: false,
                isExpandedField: false,
                developerApplicationIdBytes: ImmutableArray<byte>.Empty,
                developerApplicationVersion: null,
                nativeOverrideFieldNumber: null,
                nativeOverrideMessageNumber: null,
                isArray: decodedValues.Length > 1,
                originalValues));
    }
}
