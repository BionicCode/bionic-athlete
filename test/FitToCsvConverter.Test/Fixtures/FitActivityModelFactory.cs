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

    public static FitActivity CreateActivityForStructuredCsvExport()
    {
        DateTimeOffset activityStartTimeUtc = new(2024, 07, 14, 08, 30, 00, TimeSpan.Zero);
        DateTimeOffset sessionStartTimeUtc = activityStartTimeUtc.AddMinutes(5);

        FitField activityTimestampField = CreateField(
            FitNodeType.Activity,
            messageNumber: 34,
            fieldNumber: 253,
            originalName: "timestamp",
            messageName: "activity",
            originalValues:
            [
                CreateFieldValue(rawValue: 1142696584u, decodedValue: activityStartTimeUtc)
            ],
            profileTypeName: "DateTime",
            baseTypeName: "uint32",
            baseType: 6,
            units: "s");

        FitField sessionStartTimeField = CreateField(
            FitNodeType.Session,
            messageNumber: 18,
            fieldNumber: 2,
            originalName: "start_time",
            messageName: "session",
            originalValues:
            [
                CreateFieldValue(rawValue: 1142696584u, decodedValue: sessionStartTimeUtc)
            ],
            profileTypeName: "DateTime",
            baseTypeName: "uint32",
            baseType: 6,
            units: "s");

        FitField sessionEnhancedAverageSpeedField = CreateField(
            FitNodeType.Session,
            messageNumber: 18,
            fieldNumber: 124,
            originalName: "enhanced_avg_speed",
            messageName: "session",
            originalValues:
            [
                CreateFieldValue(rawValue: 5108u, decodedValue: 5.108d)
            ],
            profileTypeName: "Uint32",
            baseTypeName: "uint32",
            baseType: 6,
            units: "m/s",
            scale: 1000d);

        FitField sessionTotalDistanceField = CreateField(
            FitNodeType.Session,
            messageNumber: 18,
            fieldNumber: 9,
            originalName: "total_distance",
            messageName: "session",
            originalValues:
            [
                CreateFieldValue(rawValue: 614749u, decodedValue: 6147.49d)
            ],
            profileTypeName: "Uint32",
            baseTypeName: "uint32",
            baseType: 6,
            units: "m",
            scale: 100d);

        FitField sessionTotalElapsedTimeField = CreateField(
            FitNodeType.Session,
            messageNumber: 18,
            fieldNumber: 7,
            originalName: "total_elapsed_time",
            messageName: "session",
            originalValues:
            [
                CreateFieldValue(rawValue: 1247782u, decodedValue: 1247.782d)
            ],
            profileTypeName: "Uint32",
            baseTypeName: "uint32",
            baseType: 6,
            units: "s",
            scale: 1000d);

        FitField sessionTotalTimerTimeField = CreateField(
            FitNodeType.Session,
            messageNumber: 18,
            fieldNumber: 8,
            originalName: "total_timer_time",
            messageName: "session",
            originalValues:
            [
                CreateFieldValue(rawValue: 1203591u, decodedValue: 1203.591d)
            ],
            profileTypeName: "Uint32",
            baseTypeName: "uint32",
            baseType: 6,
            units: "s",
            scale: 1000d);

        FitField sessionInvalidAveragePowerPositionField = CreateField(
            FitNodeType.Session,
            messageNumber: 18,
            fieldNumber: 120,
            originalName: "avg_power_position",
            messageName: "session",
            originalValues:
            [
                CreateFieldValue(rawValue: 65535, decodedValue: 65535)
            ],
            profileTypeName: "Uint16",
            baseTypeName: "uint16",
            baseType: 4,
            units: "watts");

        FitRecord firstRecord = CreateStructuredCsvRecord(
            sequenceNumber: 0,
            timestampUtc: sessionStartTimeUtc,
            speedMetersPerSecond: 5.0d,
            distanceMeters: 1000d);

        FitRecord secondRecord = CreateStructuredCsvRecord(
            sequenceNumber: 1,
            timestampUtc: sessionStartTimeUtc.AddSeconds(5),
            speedMetersPerSecond: 6.0d,
            distanceMeters: 1500d);

        FitLap lap = new(
            CreateNodeSnapshot(
                FitNodeType.Lap,
                sequenceNumber: 0,
                messageIndex: 0,
                messageNumber: 19,
                messageName: "lap",
                timestampUtc: sessionStartTimeUtc.AddMinutes(10),
                startTimeUtc: sessionStartTimeUtc),
            ImmutableArray<FitField>.Empty);

        FitSession session = new(
            CreateNodeSnapshot(
                FitNodeType.Session,
                sequenceNumber: 0,
                messageIndex: 0,
                messageNumber: 18,
                messageName: "session",
                timestampUtc: sessionStartTimeUtc.AddMinutes(20),
                startTimeUtc: sessionStartTimeUtc),
            ImmutableArray.Create(
                sessionStartTimeField,
                sessionEnhancedAverageSpeedField,
                sessionTotalDistanceField,
                sessionTotalElapsedTimeField,
                sessionTotalTimerTimeField,
                sessionInvalidAveragePowerPositionField),
            ImmutableArray.Create(lap),
            ImmutableArray.Create(firstRecord, secondRecord));

        return new FitActivity(
            CreateNodeSnapshot(
                FitNodeType.Activity,
                sequenceNumber: 0,
                messageIndex: null,
                messageNumber: 34,
                messageName: "activity",
                timestampUtc: activityStartTimeUtc.AddMinutes(30),
                startTimeUtc: null),
            ImmutableArray.Create(activityTimestampField),
            ImmutableArray.Create(session),
            new FitFileSource("structured-export.fit"),
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

    private static FitRecord CreateStructuredCsvRecord(
        int sequenceNumber,
        DateTimeOffset timestampUtc,
        double speedMetersPerSecond,
        double distanceMeters)
    {
        FitField timestampField = CreateField(
            FitNodeType.Record,
            messageNumber: 20,
            fieldNumber: 253,
            originalName: "timestamp",
            messageName: "record",
            originalValues:
            [
                CreateFieldValue(rawValue: 1142696584u + (uint)(sequenceNumber * 5), decodedValue: timestampUtc)
            ],
            profileTypeName: "DateTime",
            baseTypeName: "uint32",
            baseType: 6,
            units: "s");

        FitField speedField = CreateField(
            FitNodeType.Record,
            messageNumber: 20,
            fieldNumber: 124,
            originalName: "enhanced_speed",
            messageName: "record",
            originalValues:
            [
                CreateFieldValue(rawValue: (uint)Math.Round(speedMetersPerSecond * 1000d), decodedValue: speedMetersPerSecond)
            ],
            profileTypeName: "Uint32",
            baseTypeName: "uint32",
            baseType: 6,
            units: "m/s",
            scale: 1000d);

        FitField distanceField = CreateField(
            FitNodeType.Record,
            messageNumber: 20,
            fieldNumber: 5,
            originalName: "distance",
            messageName: "record",
            originalValues:
            [
                CreateFieldValue(rawValue: (uint)Math.Round(distanceMeters * 100d), decodedValue: distanceMeters)
            ],
            profileTypeName: "Uint32",
            baseTypeName: "uint32",
            baseType: 6,
            units: "m",
            scale: 100d);

        return new FitRecord(
            CreateNodeSnapshot(
                FitNodeType.Record,
                sequenceNumber,
                messageIndex: null,
                messageNumber: 20,
                messageName: "record",
                timestampUtc,
                startTimeUtc: null),
            ImmutableArray.Create(timestampField, speedField, distanceField));
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
        => CreateField(
            nodeType,
            messageNumber,
            fieldNumber,
            originalName,
            messageName,
            decodedValues.Select(decodedValue => CreateFieldValue(decodedValue, decodedValue)).ToImmutableArray(),
            profileTypeName: "Test",
            baseTypeName: "test",
            baseType: 0,
            units: null,
            scale: 1d,
            offset: 0d,
            isAccumulated: false,
            isExpandedField: false);

    private static FitField CreateField(
        FitNodeType nodeType,
        ushort messageNumber,
        byte fieldNumber,
        string originalName,
        string messageName,
        ImmutableArray<FitFieldValue> originalValues,
        string profileTypeName,
        string baseTypeName,
        byte baseType,
        string? units,
        double scale = 1d,
        double offset = 0d,
        bool isAccumulated = false,
        bool isExpandedField = false)
    {
        FitFieldKey key = new(nodeType, FitFieldKind.Standard, messageNumber, fieldNumber);

        return new FitField(
            new FitFieldSnapshot(
                key,
                FitExportColumnKey.FromField(key),
                originalName,
                messageName,
                FitFieldKind.Standard,
                baseType,
                baseTypeName,
                profileTypeName,
                units,
                scale,
                offset,
                isAccumulated,
                isExpandedField,
                developerApplicationIdBytes: ImmutableArray<byte>.Empty,
                developerApplicationVersion: null,
                nativeOverrideFieldNumber: null,
                nativeOverrideMessageNumber: null,
                isArray: originalValues.Length > 1,
                originalValues));
    }

    private static FitFieldValue CreateFieldValue(object? rawValue, object? decodedValue)
        => new(rawValue, decodedValue);
}