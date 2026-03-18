namespace FitToCsvConverter.Data;

using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using BionicCode.Utilities.Net;
using Dynastream.Fit;
using DateTime = DateTime;

public static class FitFileAnalyzer
{
    private static readonly Dictionary<string, Activity> s_activityCache = [];
    private static readonly TaskCompletionSource<IEnumerable<FitSessionFieldInfo>> s_sessionFieldsTcs = new();
    private static readonly string? s_currentFitFile;

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

    //public static void ExportSessionFieldsToCsv(string fitFilePath, string csvPath, IEnumerable<FitSessionFieldInfo> sessionFields)
    //{
    //    if (!s_activityCache.TryGetValue(fitFilePath, out ImmutableDictionary<FitSessionFieldInfo, Field>? fieldInfoMap))
    //    {
    //        throw new InvalidOperationException($"Session fields for file '{fitFilePath}' not found in cache. Ensure '{nameof(GetAvailableSessionFieldsAsync)}' was called first and the returned fields are passed to this method.");
    //    }

    //    using var fitStream = new FileStream(fitFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);

    //    var decoder = new Decode();
    //    var broadcaster = new MesgBroadcaster();

    //    decoder.MesgEvent += broadcaster.OnMesg;
    //    decoder.MesgDefinitionEvent += broadcaster.OnMesgDefinition;

    //    using var writer = new StreamWriter(csvPath, false, Encoding.UTF8);
    //    writer.WriteLine("MesgName,FieldNum,FieldName,ValueIndex,DecodedValue,RawValue,ValueType,Units,Scale,Offset,ProfileType");

    //    broadcaster.SessionMesgEvent += (_, e) =>
    //    {
    //        var session = new SessionMesg(e.mesg);

    //        // Example of typed access:
    //        Dynastream.Fit.DateTime? startTime = session.GetStartTime();
    //        float? totalElapsed = session.GetTotalElapsedTime();
    //        float? avgSpeed = session.GetAvgSpeed();

    //        // Generic export of all fields for inspection:
    //    };

    //    foreach (FitSessionFieldInfo fieldInfo in sessionFields)
    //    {
    //        if (!fieldInfoMap.TryGetValue(fieldInfo, out Field? field))
    //        {
    //            throw new InvalidOperationException($"Field info for FieldId={fieldInfo.Id}, FieldName='{fieldInfo.Name}' not found in cache for file '{fitFilePath}'. Ensure '{nameof(GetAvailableSessionFieldsAsync)}' was called first and the returned fields are passed to this method.");
    //        }

    //        int valueCount = field.GetNumValues();
    //        if (valueCount == 0)
    //        {
    //            WriteRow(
    //                writer,
    //                "session",
    //                field.Num.ToString(CultureInfo.InvariantCulture),
    //                field.Name,
    //                "",
    //                "",
    //                "",
    //                "",
    //                field.Units,
    //                field.Scale.ToString(CultureInfo.InvariantCulture),
    //                field.Offset.ToString(CultureInfo.InvariantCulture),
    //                field.ProfileType.ToString());
    //            continue;
    //        }

    //        for (int i = 0; i < valueCount; i++)
    //        {
    //            // Main-field decoding:
    //            object? decoded = field.GetValue(i);

    //            // Raw stored value:
    //            object? raw = field.GetRawValue(i);

    //            WriteRow(
    //                writer,
    //                "session",
    //                field.Num.ToString(CultureInfo.InvariantCulture),
    //                field.Name,
    //                i.ToString(CultureInfo.InvariantCulture),
    //                FormatValue(decoded),
    //                FormatValue(raw),
    //                decoded?.GetType().FullName ?? raw?.GetType().FullName ?? "",
    //                field.Units,
    //                field.Scale.ToString(CultureInfo.InvariantCulture),
    //                field.Offset.ToString(CultureInfo.InvariantCulture),
    //                field.ProfileType.ToString());
    //        }
    //    }

    //    // Optional: a tiny human summary at the end
    //    writer.WriteLine();
    //    writer.WriteLine("Summary");
    //    writer.WriteLine($"StartTime,{Escape(FormatValue(startTime))}");
    //    writer.WriteLine($"TotalElapsedTime_s,{Escape(FormatValue(totalElapsed))}");
    //    writer.WriteLine($"AvgSpeed_mps,{Escape(FormatValue(avgSpeed))}");
    //}

    //public static async Task<DecoderResult<Activity> GetActivityAsync(string fitFilePath, CancellationToken cancellationToken = default)
    //{
    //    ArgumentExceptionAdvanced.ThrowIfNullOrWhiteSpace(fitFilePath);

    //    s_currentFitFile = fitFilePath;

    //    if (s_activityCache.TryGetValue(s_currentFitFile, out ImmutableDictionary<FitSessionFieldInfo, Field>? sessionInfoMap))
    //    {
    //        IEnumerable<FitSessionFieldInfo> sessionFields = sessionInfoMap.Keys;

    //        return (Activity)sessionFields;
    //    }

    //    await using var fitStream = new FileStream(fitFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);

    //    var decoder = new Decode();
    //    var broadcaster = new MesgBroadcaster();

    //    decoder.MesgEvent += broadcaster.OnMesg;
    //    decoder.MesgDefinitionEvent += broadcaster.OnMesgDefinition;

    //    broadcaster.FileIdMesgEvent += OnFileIdReady;
    //    broadcaster.SessionMesgEvent += OnSessionsReady;
    //    broadcaster.ActivityMesgEvent += OnActivityReady;
    //    broadcaster.RecordMesgEvent += OnRecordReady;
    //    broadcaster. += OnRecordReady;

    //    s_sessionFieldsTcs = new TaskCompletionSource<IEnumerable<FitSessionFieldInfo>>();
    //    _ = decoder.Read(fitStream);

    //    return (Activity)await s_sessionFieldsTcs.Task.ConfigureAwait(false);
    //}

    private static void OnFileIdReady(object sender, MesgEventArgs e) => throw new NotImplementedException();
    private static void OnRecordReady(object sender, MesgEventArgs e) => throw new NotImplementedException();
    private static void OnActivityReady(object sender, MesgEventArgs e) => throw new NotImplementedException();

    //private static void OnSessionsReady(object sender, MesgEventArgs e)
    //{
    //    var session = new SessionMesg(e.mesg);

    //    // Example of typed access:
    //    Dynastream.Fit.DateTime? startTime = session.GetStartTime();
    //    DateTime sessionTime = startTime?.GetDateTime() ?? DateTime.MinValue;
    //    float totalElapsedInSeconds = session.GetTotalElapsedTime() ?? 0;
    //    var sessionDuration = TimeSpan.FromSeconds(totalElapsedInSeconds);
    //    double avgSpeedInMps = session.GetAvgSpeed() ?? 0;

    //    HashSet<FitSessionFieldInfo> fieldInfoList = [];
    //    Dictionary<FitSessionFieldInfo, Field> fieldInfoMap = [];

    //    foreach (Field field in session.Fields)
    //    {
    //        if (!IsKnownField(field))
    //        {
    //            continue; // Skip unknown fields
    //        }

    //        var fieldInfo = new FitSessionFieldInfo(
    //            fieldId: field.Num,
    //            fieldName: field.Name,
    //            sessionTime: sessionTime,
    //            sessionDuration: sessionDuration,
    //            averageSpeed: avgSpeedInMps);

    //        if (fieldInfoList.Add(fieldInfo))
    //        {
    //            fieldInfoMap.Add(fieldInfo, field);
    //        }
    //    }

    //    var immutableFieldInfoMap = fieldInfoMap.ToImmutableDictionary();
    //    s_activityCache.Add(s_currentFitFile!, immutableFieldInfoMap);

    //    s_sessionFieldsTcs.SetResult(fieldInfoList);
    //}

    private static bool IsKnownField(Field field) => field.ProfileType != Profile.Type.NumTypes;

    private static bool IsStringField(Field field) => (field.Type & Fit.BaseTypeNumMask) == Fit.String;

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

public class Activity
{
    public required string ActivityId { get; set; }
}

public class Session
{
    private ImmutableList<FitSessionFieldInfo> Fields { get; }
    public DateTime SessionTime { get; set; }
    public TimeSpan SessionDuration { get; set; }
    public double AverageSpeedMetersPerSecond { get; set; }
    public TimeUnit TimeUnit { get; set; }
    public SpeedUnit SpeedUnit { get; set; }
    public DistanceUnit DistanceUnit { get; set; }

    public Session(ImmutableList<FitSessionFieldInfo> fields, DateTime sessionTime, TimeSpan sessionDuration, double averageSpeed)
    {
        Fields = fields;
        SessionTime = sessionTime;
        SessionDuration = sessionDuration;
        AverageSpeedMetersPerSecond = averageSpeed;
        TimeUnit = TimeUnit.Auto;
        SpeedUnit = SpeedUnit.KilometersPerHour;
        DistanceUnit = DistanceUnit.Auto;
    }
}

public class FitSessionFieldInfo
{
    internal FitSessionFieldInfo(Field fitField)
    {
        ArgumentNullExceptionAdvanced.ThrowIfNull(fitField);

        FitField = fitField;
        Id = FitField.Num;
        Name = FitField.Name;
        DisplayName = Name; // Default display name is the same as the field name, can be overridden later
        IsVisible = true; // Default to visible, can be overridden later
    }

    public int Id { get; }
    public string Name { get; }
    public string DisplayName { get; set; }
    public bool IsVisible { get; set; }
    internal Field FitField { get; }
}

public enum SpeedUnit
{
    MetersPerSecond,
    KilometersPerHour,
    MilesPerHour
}

public enum TimeUnit
{
    /// <summary>
    /// Specifies that the value or behavior should be determined automatically based on the current context or default
    /// settings.
    /// </summary>
    /// <remarks>Use this value when you want the system to select the most appropriate option without
    /// explicitly specifying a particular value. In general the biggest time unit possible should be selected when this value is
    /// used.</remarks>
    Auto,
    Seconds,
    Minutes,
    Hours
}

public enum DistanceUnit
{
    /// <summary>
    /// Specifies that the value or behavior should be determined automatically based on the current context or default
    /// settings.
    /// </summary>
    /// <remarks>Use this value when you want the system to select the most appropriate option without
    /// explicitly specifying a particular value. In general the biggest distance unit possible should be selected when this value is
    /// used.</remarks>
    Auto,
    Meters,
    Kilometers,
    Miles
}

public class DecoderResult<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public string ErrorMessage { get; }
    private DecoderResult(bool isSuccess, T? value, string? errorMessage)
    {
        IsSuccess = isSuccess;
        Value = value;
        ErrorMessage = errorMessage ?? string.Empty;
    }

    public static DecoderResult<T> Success(T value) => new(true, value, string.Empty);
    public static DecoderResult<T> Failure(string errorMessage) => new(false, default, errorMessage);
}