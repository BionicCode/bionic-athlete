namespace FitToCsvConverter.Test.Fixtures;

using Dynastream.Fit;
using FitDateTime = Dynastream.Fit.DateTime;

internal static class FitTestFileFactory
{
    public static byte[] CreateSingleSessionActivityWithDeveloperFields()
    {
        FitDateTime startTime = new(new System.DateTime(2024, 05, 01, 06, 30, 00, DateTimeKind.Utc));
        FitDateTime endTime = new(startTime);
        endTime.Add(20U);

        DeveloperDataIdMesg developerDataId = CreateDeveloperDataIdMessage();
        FieldDescriptionMesg sessionDeveloperFieldDescription = CreateFieldDescriptionMessage(
            developerDataIndex: 0,
            fieldDefinitionNumber: 0,
            fitBaseType: FitBaseType.Float32,
            fieldName: "session_score",
            units: "score",
            nativeMesgNum: MesgNum.Session);
        FieldDescriptionMesg sessionArrayFieldDescription = CreateFieldDescriptionMessage(
            developerDataIndex: 0,
            fieldDefinitionNumber: 1,
            fitBaseType: FitBaseType.Uint16,
            fieldName: "session_steps",
            units: "steps",
            nativeMesgNum: MesgNum.Session,
            isArray: true);
        FieldDescriptionMesg lapDeveloperFieldDescription = CreateFieldDescriptionMessage(
            developerDataIndex: 0,
            fieldDefinitionNumber: 2,
            fitBaseType: FitBaseType.Uint16,
            fieldName: "lap_score",
            units: "score",
            nativeMesgNum: MesgNum.Lap);
        FieldDescriptionMesg recordDeveloperFieldDescription = CreateFieldDescriptionMessage(
            developerDataIndex: 0,
            fieldDefinitionNumber: 3,
            fitBaseType: FitBaseType.Uint16,
            fieldName: "record_tag",
            units: "tag",
            nativeMesgNum: MesgNum.Record,
            nativeFieldNum: RecordMesg.FieldDefNum.HeartRate);

        List<Mesg> messages =
        [
            CreateFileIdMessage(startTime),
            CreateTimerEvent(startTime, EventType.Start),
            developerDataId,
            sessionDeveloperFieldDescription,
            sessionArrayFieldDescription,
            lapDeveloperFieldDescription,
            recordDeveloperFieldDescription
        ];

        for (int index = 0; index < 3; index++)
        {
            FitDateTime recordTimestamp = new(startTime);
            recordTimestamp.Add((uint)(index * 5));
            RecordMesg record = new();
            record.SetTimestamp(recordTimestamp);
            record.SetHeartRate((byte)(140 + index));
            record.SetDistance(100 + (index * 50));
            record.SetPositionLat(1000 + index);
            record.SetPositionLong(2000 + index);

            DeveloperField recordDeveloperField = new(recordDeveloperFieldDescription, developerDataId);
            record.SetDeveloperField(recordDeveloperField);
            recordDeveloperField.SetValue((ushort)(900 + index));

            messages.Add(record);
        }

        LapMesg firstLap = new();
        firstLap.SetMessageIndex(0);
        firstLap.SetStartTime(startTime);
        firstLap.SetTimestamp(new FitDateTime(startTime.GetTimeStamp() + 10));
        firstLap.SetTotalElapsedTime(10U);
        firstLap.SetTotalTimerTime(10U);
        DeveloperField firstLapDeveloperField = new(lapDeveloperFieldDescription, developerDataId);
        firstLap.SetDeveloperField(firstLapDeveloperField);
        firstLapDeveloperField.SetValue((ushort)111);
        messages.Add(firstLap);

        LapMesg secondLap = new();
        secondLap.SetMessageIndex(1);
        secondLap.SetStartTime(new FitDateTime(startTime.GetTimeStamp() + 10));
        secondLap.SetTimestamp(endTime);
        secondLap.SetTotalElapsedTime(10U);
        secondLap.SetTotalTimerTime(10U);
        DeveloperField secondLapDeveloperField = new(lapDeveloperFieldDescription, developerDataId);
        secondLap.SetDeveloperField(secondLapDeveloperField);
        secondLapDeveloperField.SetValue((ushort)222);
        messages.Add(secondLap);

        SessionMesg session = new();
        session.SetMessageIndex(0);
        session.SetStartTime(startTime);
        session.SetTimestamp(endTime);
        session.SetTotalElapsedTime(20U);
        session.SetTotalTimerTime(20U);
        session.SetFirstLapIndex(0);
        session.SetNumLaps(2);
        session.SetSport(Sport.Running);
        session.SetSubSport(SubSport.Generic);

        DeveloperField sessionDeveloperField = new(sessionDeveloperFieldDescription, developerDataId);
        session.SetDeveloperField(sessionDeveloperField);
        sessionDeveloperField.SetValue(12.5f);

        DeveloperField sessionArrayField = new(sessionArrayFieldDescription, developerDataId);
        session.SetDeveloperField(sessionArrayField);
        sessionArrayField.SetValue(index: 0, value: (ushort)10);
        sessionArrayField.SetValue(index: 1, value: (ushort)20);
        sessionArrayField.SetValue(index: 2, value: (ushort)30);
        messages.Add(session);

        messages.Add(CreateTimerEvent(endTime, EventType.StopAll));

        ActivityMesg activity = new();
        activity.SetTimestamp(new FitDateTime(endTime.GetTimeStamp() + 5));
        activity.SetNumSessions(1);
        activity.SetTotalTimerTime(20U);
        messages.Add(activity);

        return EncodeMessages(messages);
    }

    public static byte[] CreateMultiSessionActivity()
    {
        FitDateTime firstSessionStart = new(new System.DateTime(2024, 06, 15, 08, 00, 00, DateTimeKind.Utc));
        FitDateTime secondSessionStart = new(new System.DateTime(2024, 06, 15, 10, 00, 00, DateTimeKind.Utc));

        List<Mesg> messages =
        [
            CreateFileIdMessage(firstSessionStart),
            CreateTimerEvent(firstSessionStart, EventType.Start)
        ];

        AddRecord(messages, firstSessionStart, 130, 500);
        AddRecord(messages, new FitDateTime(firstSessionStart.GetTimeStamp() + 10), 131, 550);
        AddRecord(messages, secondSessionStart, 150, 800);
        AddRecord(messages, new FitDateTime(secondSessionStart.GetTimeStamp() + 10), 151, 850);

        LapMesg firstLap = new();
        firstLap.SetMessageIndex(0);
        firstLap.SetStartTime(firstSessionStart);
        firstLap.SetTimestamp(new FitDateTime(firstSessionStart.GetTimeStamp() + 20));
        firstLap.SetTotalElapsedTime(20U);
        firstLap.SetTotalTimerTime(20U);
        messages.Add(firstLap);

        LapMesg secondLap = new();
        secondLap.SetMessageIndex(1);
        secondLap.SetStartTime(secondSessionStart);
        secondLap.SetTimestamp(new FitDateTime(secondSessionStart.GetTimeStamp() + 20));
        secondLap.SetTotalElapsedTime(20U);
        secondLap.SetTotalTimerTime(20U);
        messages.Add(secondLap);

        SessionMesg firstSession = new();
        firstSession.SetMessageIndex(0);
        firstSession.SetStartTime(firstSessionStart);
        firstSession.SetTimestamp(new FitDateTime(firstSessionStart.GetTimeStamp() + 20));
        firstSession.SetTotalElapsedTime(20U);
        firstSession.SetTotalTimerTime(20U);
        firstSession.SetFirstLapIndex(0);
        firstSession.SetNumLaps(1);
        firstSession.SetSport(Sport.Running);
        firstSession.SetSubSport(SubSport.Generic);
        messages.Add(firstSession);

        SessionMesg secondSession = new();
        secondSession.SetMessageIndex(1);
        secondSession.SetStartTime(secondSessionStart);
        secondSession.SetTimestamp(new FitDateTime(secondSessionStart.GetTimeStamp() + 20));
        secondSession.SetTotalElapsedTime(20U);
        secondSession.SetTotalTimerTime(20U);
        secondSession.SetFirstLapIndex(1);
        secondSession.SetNumLaps(1);
        secondSession.SetSport(Sport.Cycling);
        secondSession.SetSubSport(SubSport.Road);
        messages.Add(secondSession);

        messages.Add(CreateTimerEvent(new FitDateTime(secondSessionStart.GetTimeStamp() + 20), EventType.StopAll));

        ActivityMesg activity = new();
        activity.SetTimestamp(new FitDateTime(secondSessionStart.GetTimeStamp() + 25));
        activity.SetNumSessions(2);
        activity.SetTotalTimerTime(40U);
        messages.Add(activity);

        return EncodeMessages(messages);
    }

    public static string GetExampleFitFilePath()
        => Path.GetFullPath(
            Path.Combine(
                AppContext.BaseDirectory,
                "..",
                "..",
                "..",
                "..",
                "..",
                "src",
                "FitToCsvConverter.Data",
                "ExampleFiles",
                "2026-03-17_22204467914_ACTIVITY.fit"));

    private static void AddRecord(List<Mesg> messages, FitDateTime timestamp, byte heartRate, float distance)
    {
        RecordMesg record = new();
        record.SetTimestamp(timestamp);
        record.SetHeartRate(heartRate);
        record.SetDistance(distance);
        record.SetPositionLat(10000 + heartRate);
        record.SetPositionLong(20000 + heartRate);
        messages.Add(record);
    }

    private static FileIdMesg CreateFileIdMessage(FitDateTime startTime)
    {
        FileIdMesg fileId = new();
        fileId.SetType(Dynastream.Fit.File.Activity);
        fileId.SetManufacturer(Manufacturer.Development);
        fileId.SetProduct(1);
        fileId.SetSerialNumber(12345);
        fileId.SetTimeCreated(startTime);
        return fileId;
    }

    private static EventMesg CreateTimerEvent(FitDateTime timestamp, EventType eventType)
    {
        EventMesg eventMessage = new();
        eventMessage.SetTimestamp(timestamp);
        eventMessage.SetEvent(Event.Timer);
        eventMessage.SetEventType(eventType);
        return eventMessage;
    }

    private static DeveloperDataIdMesg CreateDeveloperDataIdMessage()
    {
        DeveloperDataIdMesg developerDataId = new();
        developerDataId.SetDeveloperDataIndex(0);
        developerDataId.SetApplicationVersion(42);

        byte[] applicationId = [1, 2, 3, 4, 5, 6, 7, 8];
        for (int index = 0; index < applicationId.Length; index++)
        {
            developerDataId.SetApplicationId(index, applicationId[index]);
        }

        return developerDataId;
    }

    private static FieldDescriptionMesg CreateFieldDescriptionMessage(
        byte developerDataIndex,
        byte fieldDefinitionNumber,
        byte fitBaseType,
        string fieldName,
        string units,
        ushort nativeMesgNum,
        byte? nativeFieldNum = null,
        bool isArray = false)
    {
        FieldDescriptionMesg fieldDescription = new();
        fieldDescription.SetDeveloperDataIndex(developerDataIndex);
        fieldDescription.SetFieldDefinitionNumber(fieldDefinitionNumber);
        fieldDescription.SetFitBaseTypeId(fitBaseType);
        fieldDescription.SetFieldName(0, fieldName);
        fieldDescription.SetUnits(0, units);
        fieldDescription.SetNativeMesgNum(nativeMesgNum);
        fieldDescription.SetArray(isArray ? (byte)1 : (byte)0);

        if (nativeFieldNum is byte fieldNumber)
        {
            fieldDescription.SetNativeFieldNum(fieldNumber);
        }

        return fieldDescription;
    }

    private static byte[] EncodeMessages(IEnumerable<Mesg> messages)
    {
        using MemoryStream fitStream = new();
        Encode encoder = new(ProtocolVersion.V20);
        encoder.Open(fitStream);
        encoder.Write(messages);
        encoder.Close();
        return fitStream.ToArray();
    }
}
