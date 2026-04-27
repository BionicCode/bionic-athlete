namespace FitBionicAthlete.Training.Reporting;

using System.Collections.Immutable;
using System.Globalization;
using BionicAthlete.Training.Domain.Activities;
using BionicAthlete.Training.Domain.Fields;

/// <summary>
/// Projects one decoded FIT activity into the first View C report model.
/// </summary>
public sealed class ActivityReportProjector : IActivityReportProjector
{
    /// <inheritdoc />
    public Task<ActivityReport> ProjectAsync(
        FitActivity activity,
        ActivityReportExportOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(activity);
        ArgumentNullException.ThrowIfNull(options);

        cancellationToken.ThrowIfCancellationRequested();

        FitSession? primarySession = activity.Sessions.FirstOrDefault();
        ImmutableArray<ActivityReportDiagnostic>.Builder diagnostics = ImmutableArray.CreateBuilder<ActivityReportDiagnostic>();
        ImmutableArray<ActivityReportSection>.Builder sections = ImmutableArray.CreateBuilder<ActivityReportSection>();

        if (primarySession is null)
        {
            diagnostics.Add(new ActivityReportDiagnostic("NoSession", "The decoded activity does not contain a session."));
        }

        sections.Add(CreateOverviewSection(activity, primarySession, options));
        sections.Add(CreateTimingSection(activity, primarySession, options));
        sections.Add(CreatePowerSection(primarySession, options));
        sections.Add(CreateHeartRateSection(primarySession, options));
        sections.Add(CreateCadenceAndSpeedSection(primarySession, options));
        sections.Add(CreateRespirationAndTemperatureSection(primarySession, options));
        sections.Add(CreateStaminaAndHydrationSection(primarySession, options));
        sections.Add(CreateLapsSection(primarySession, options));
        sections.Add(CreateDeviceAndSourceSection(activity, options));

        if (options.IncludeProvenanceNotes)
        {
            sections.Add(CreateDataQualitySection());
        }

        string sourceFilePath = activity.Source.FilePath ?? string.Empty;
        DateTimeOffset? startTimeUtc = activity.CanonicalStartTimeUtc;
        string title = BuildTitle(activity, primarySession, options);

        var report = new ActivityReport(
            CreateReportId(sourceFilePath, startTimeUtc),
            title,
            sourceFilePath,
            startTimeUtc,
            options.ExportTimestampUtc,
            sections.ToImmutable(),
            diagnostics.ToImmutable());

        return Task.FromResult(report);
    }

    private static ActivityReportSection CreateOverviewSection(
        FitActivity activity,
        FitSession? session,
        ActivityReportExportOptions options)
    {
        ImmutableArray<ActivityReportMetric>.Builder metrics = ImmutableArray.CreateBuilder<ActivityReportMetric>();
        AddMetric(metrics, CreateMetric(session, "total_distance", "Distance", options, value => FormatDistance(value, options), "km"));
        AddMetric(metrics, CreateDurationMetric(session, "total_timer_time", "Time", options));
        AddMetric(metrics, CreateSpeedMetric(session, "enhanced_avg_speed", "Avg Speed", options));
        AddMetric(metrics, CreateMetric(session, "avg_power", "Avg Power", options, value => FormatNumber(value, options.Culture, 0), "W"));
        AddMetric(metrics, CreateMetric(session, "total_calories", "Total Calories Burned", options, value => FormatNumber(value, options.Culture, 0), "kcal"));
        AddMetric(metrics, CreateMetric(session, "training_load_peak", "Exercise Load", options, value => FormatNumber(value, options.Culture, 0), null));
        AddMetric(metrics, CreateActivityDateMetric(activity, options));

        return new ActivityReportSection(
            "overview",
            "Overview",
            metrics.ToImmutable(),
            ImmutableArray<ActivityReportChart>.Empty,
            ImmutableArray<ActivityReportTable>.Empty);
    }

    private static ActivityReportSection CreateTimingSection(
        FitActivity activity,
        FitSession? session,
        ActivityReportExportOptions options)
    {
        ImmutableArray<ActivityReportMetric>.Builder metrics = ImmutableArray.CreateBuilder<ActivityReportMetric>();
        AddMetric(metrics, CreateDurationMetric(session, "total_timer_time", "Timer Time", options));
        AddMetric(metrics, CreateDurationMetric(session, "total_elapsed_time", "Elapsed Time", options));
        AddMetric(metrics, CreateDurationMetric(session, "total_moving_time", "Moving Time", options));

        DateTimeOffset? startTimeUtc = activity.CanonicalStartTimeUtc;
        if (startTimeUtc is DateTimeOffset startTime)
        {
            DateTimeOffset localStart = TimeZoneInfo.ConvertTime(startTime, options.LocalTimeZone);
            metrics.Add(new ActivityReportMetric(
                "activity.local_start_time",
                "Local Start",
                localStart.ToString("f", options.Culture),
                null,
                ActivityReportFieldClassification.DirectStandardFit,
                "activity.canonical_start_time"));
        }

        return new ActivityReportSection(
            "timing",
            "Timing",
            metrics.ToImmutable(),
            ImmutableArray<ActivityReportChart>.Empty,
            ImmutableArray<ActivityReportTable>.Empty);
    }

    private static ActivityReportSection CreatePowerSection(FitSession? session, ActivityReportExportOptions options)
    {
        ImmutableArray<ActivityReportMetric>.Builder metrics = ImmutableArray.CreateBuilder<ActivityReportMetric>();
        AddMetric(metrics, CreateMetric(session, "avg_power", "Avg Power", options, value => FormatNumber(value, options.Culture, 0), "W"));
        AddMetric(metrics, CreateMetric(session, "max_power", "Max Power", options, value => FormatNumber(value, options.Culture, 0), "W"));
        AddMetric(metrics, CreateMetric(session, "normalized_power", "Normalized Power (NP)", options, value => FormatNumber(value, options.Culture, 0), "W"));
        AddMetric(metrics, CreateMetric(session, "intensity_factor", "IF", options, value => FormatNumber(value, options.Culture, 3), null));
        AddMetric(metrics, CreateMetric(session, "training_stress_score", "TSS", options, value => FormatNumber(value, options.Culture, 1), null));
        AddMetric(metrics, CreateMetric(session, "threshold_power", "FTP Setting", options, value => FormatNumber(value, options.Culture, 0), "W"));
        AddMetric(metrics, CreateMetric(session, "total_work", "Work", options, value => FormatNumber(value / 1000d, options.Culture, 0), "kJ"));

        ImmutableArray<ActivityReportChart> charts = session is null
            ? ImmutableArray<ActivityReportChart>.Empty
            : CreateRecordCharts(session, "power", "Power", "Power", "W");

        return new ActivityReportSection(
            "power",
            "Power",
            metrics.ToImmutable(),
            charts,
            ImmutableArray<ActivityReportTable>.Empty);
    }

    private static ActivityReportSection CreateHeartRateSection(FitSession? session, ActivityReportExportOptions options)
    {
        ImmutableArray<ActivityReportMetric>.Builder metrics = ImmutableArray.CreateBuilder<ActivityReportMetric>();
        AddMetric(metrics, CreateMetric(session, "avg_heart_rate", "Avg HR", options, value => FormatNumber(value, options.Culture, 0), "bpm"));
        AddMetric(metrics, CreateMetric(session, "max_heart_rate", "Max HR", options, value => FormatNumber(value, options.Culture, 0), "bpm"));

        ImmutableArray<ActivityReportChart> charts = session is null
            ? ImmutableArray<ActivityReportChart>.Empty
            : CreateRecordCharts(session, "heart_rate", "Heart Rate", "Heart Rate", "bpm");

        return new ActivityReportSection(
            "heart-rate",
            "Heart Rate",
            metrics.ToImmutable(),
            charts,
            ImmutableArray<ActivityReportTable>.Empty);
    }

    private static ActivityReportSection CreateCadenceAndSpeedSection(FitSession? session, ActivityReportExportOptions options)
    {
        ImmutableArray<ActivityReportMetric>.Builder metrics = ImmutableArray.CreateBuilder<ActivityReportMetric>();
        AddMetric(metrics, CreateSpeedMetric(session, "enhanced_avg_speed", "Avg Speed", options));
        AddMetric(metrics, CreateSpeedMetric(session, "enhanced_max_speed", "Max Speed", options));
        AddMetric(metrics, CreateMetric(session, "avg_cadence", "Avg Bike Cadence", options, value => FormatNumber(value, options.Culture, 0), "rpm"));
        AddMetric(metrics, CreateMetric(session, "max_cadence", "Max Bike Cadence", options, value => FormatNumber(value, options.Culture, 0), "rpm"));
        AddMetric(metrics, CreateMetric(session, "total_cycles", "Total Strokes", options, value => FormatNumber(value, options.Culture, 0), "cycles"));

        ImmutableArray<ActivityReportChart>.Builder charts = ImmutableArray.CreateBuilder<ActivityReportChart>();
        if (session is not null)
        {
            charts.AddRange(CreateRecordCharts(session, "enhanced_speed", "Speed", "Speed", "m/s"));
            charts.AddRange(CreateRecordCharts(session, "cadence", "Cadence", "Cadence", "rpm"));
        }

        return new ActivityReportSection(
            "cadence-speed",
            "Cadence / Speed",
            metrics.ToImmutable(),
            charts.ToImmutable(),
            ImmutableArray<ActivityReportTable>.Empty);
    }

    private static ActivityReportSection CreateRespirationAndTemperatureSection(FitSession? session, ActivityReportExportOptions options)
    {
        ImmutableArray<ActivityReportMetric>.Builder metrics = ImmutableArray.CreateBuilder<ActivityReportMetric>();
        AddMetric(metrics, CreateMetric(session, "enhanced_avg_respiration_rate", "Avg Respiration Rate", options, value => FormatNumber(value, options.Culture, 0), "brpm"));
        AddMetric(metrics, CreateMetric(session, "enhanced_min_respiration_rate", "Min Respiration Rate", options, value => FormatNumber(value, options.Culture, 0), "brpm"));
        AddMetric(metrics, CreateMetric(session, "enhanced_max_respiration_rate", "Max Respiration Rate", options, value => FormatNumber(value, options.Culture, 0), "brpm"));
        AddMetric(metrics, CreateMetric(session, "avg_temperature", "Avg Temp", options, value => FormatNumber(value, options.Culture, 1), "°C"));
        AddMetric(metrics, CreateMetric(session, "min_temperature", "Min Temp", options, value => FormatNumber(value, options.Culture, 1), "°C"));
        AddMetric(metrics, CreateMetric(session, "max_temperature", "Max Temp", options, value => FormatNumber(value, options.Culture, 1), "°C"));

        ImmutableArray<ActivityReportChart>.Builder charts = ImmutableArray.CreateBuilder<ActivityReportChart>();
        if (session is not null)
        {
            charts.AddRange(CreateRecordCharts(session, "enhanced_respiration_rate", "Respiration", "Respiration", "brpm"));
            charts.AddRange(CreateRecordCharts(session, "temperature", "Temperature", "Temperature", "°C"));
        }

        return new ActivityReportSection(
            "respiration-temperature",
            "Respiration / Temperature",
            metrics.ToImmutable(),
            charts.ToImmutable(),
            ImmutableArray<ActivityReportTable>.Empty);
    }

    private static ActivityReportSection CreateStaminaAndHydrationSection(FitSession? session, ActivityReportExportOptions options)
    {
        ImmutableArray<ActivityReportMetric>.Builder metrics = ImmutableArray.CreateBuilder<ActivityReportMetric>();
        AddMetric(metrics, CreateMappedUnknownMetric(session, "unknown_178", "session.est_sweat_loss", "Est. Sweat Loss", "ml", options));
        AddMetric(metrics, CreateMappedUnknownMetric(session, "unknown_205", "session.beginning_potential", "Beginning Potential", "%", options));
        AddMetric(metrics, CreateMappedUnknownMetric(session, "unknown_206", "session.ending_potential", "Ending Potential", "%", options));
        AddMetric(metrics, CreateMappedUnknownMetric(session, "unknown_207", "session.min_stamina", "Min Stamina", "%", options));

        return new ActivityReportSection(
            "stamina-hydration",
            "Stamina / Hydration",
            metrics.ToImmutable(),
            ImmutableArray<ActivityReportChart>.Empty,
            ImmutableArray<ActivityReportTable>.Empty,
            "Stamina and sweat-loss values are shown as inferred aliases from preserved unknown FIT session fields when present.");
    }

    private static ActivityReportSection CreateLapsSection(FitSession? session, ActivityReportExportOptions options)
    {
        ImmutableArray<ActivityReportTable> tables = session is null
            ? ImmutableArray<ActivityReportTable>.Empty
            : ImmutableArray.Create(CreateLapTable(session, options));

        return new ActivityReportSection(
            "laps",
            "Laps / Intervals",
            ImmutableArray<ActivityReportMetric>.Empty,
            ImmutableArray<ActivityReportChart>.Empty,
            tables);
    }

    private static ActivityReportSection CreateDeviceAndSourceSection(FitActivity activity, ActivityReportExportOptions options)
    {
        ImmutableArray<ActivityReportMetric>.Builder metrics = ImmutableArray.CreateBuilder<ActivityReportMetric>();
        metrics.Add(new ActivityReportMetric(
            "source.file",
            "Source File",
            string.IsNullOrWhiteSpace(activity.Source.FilePath) ? "Unavailable" : activity.Source.FilePath,
            null,
            ActivityReportFieldClassification.DirectStandardFit,
            "fit.source"));
        metrics.Add(new ActivityReportMetric(
            "export.generated_at",
            "Generated",
            options.ExportTimestampUtc.ToString("O", CultureInfo.InvariantCulture),
            null,
            ActivityReportFieldClassification.DerivedFromFit,
            null,
            "Timestamp is supplied by the application export request, not read from the FIT file."));

        foreach (ActivityReportMetric metric in CreateDeviceMetrics(activity))
        {
            metrics.Add(metric);
        }

        return new ActivityReportSection(
            "device-source",
            "Device and Source Metadata",
            metrics.ToImmutable(),
            ImmutableArray<ActivityReportChart>.Empty,
            ImmutableArray<ActivityReportTable>.Empty);
    }

    private static ActivityReportSection CreateDataQualitySection()
        => new(
            "data-quality",
            "Data Quality and Provenance",
            ImmutableArray.Create(
                new ActivityReportMetric("provenance.direct_fit", "Direct FIT", "Value comes directly from a documented or decoded FIT field.", null, ActivityReportFieldClassification.DirectStandardFit),
                new ActivityReportMetric("provenance.derived", "Formula-derived", "Value is calculated from decoded FIT fields.", null, ActivityReportFieldClassification.DerivedFromFit),
                new ActivityReportMetric("provenance.mapped_unknown", "Mapped unknown FIT field", "Value is inferred from preserved unknown FIT data and marked with a caveat.", null, ActivityReportFieldClassification.MappedFromUnmappedFitField),
                new ActivityReportMetric("provenance.unavailable", "Unavailable", "Value was not present or not reliably derivable from this activity.", null, ActivityReportFieldClassification.Unavailable)),
            ImmutableArray<ActivityReportChart>.Empty,
            ImmutableArray<ActivityReportTable>.Empty,
            "The report is projected from decoded FIT data. It does not invent Garmin Connect-only values.");

    private static ActivityReportMetric CreateActivityDateMetric(FitActivity activity, ActivityReportExportOptions options)
    {
        if (activity.CanonicalStartTimeUtc is not DateTimeOffset startTimeUtc)
        {
            return CreateUnavailableMetric("activity.date", "Activity Date");
        }

        DateTimeOffset localStart = TimeZoneInfo.ConvertTime(startTimeUtc, options.LocalTimeZone);
        return new ActivityReportMetric(
            "activity.date",
            "Activity Date",
            localStart.ToString("D", options.Culture),
            null,
            ActivityReportFieldClassification.DirectStandardFit,
            "activity.canonical_start_time");
    }

    private static ActivityReportMetric? CreateMetric(
        FitSession? session,
        string fieldName,
        string label,
        ActivityReportExportOptions options,
        Func<double, string> formatter,
        string? unit)
    {
        FitField? field = session is null ? null : FindField(session.Fields, fieldName);
        if (field is null || !TryGetDouble(field, out double value))
        {
            return CreateUnavailableMetric($"session.{fieldName}", label);
        }

        return new ActivityReportMetric(
            $"session.{fieldName}",
            label,
            formatter.Invoke(value),
            unit,
            DetermineClassification(field),
            $"session.{field.Original.OriginalName}");
    }

    private static ActivityReportMetric? CreateSpeedMetric(
        FitSession? session,
        string fieldName,
        string label,
        ActivityReportExportOptions options)
        => CreateMetric(
            session,
            fieldName,
            label,
            options,
            value => FormatNumber(value * 3.6d, options.Culture, 1),
            "km/h");

    private static ActivityReportMetric? CreateDurationMetric(
        FitSession? session,
        string fieldName,
        string label,
        ActivityReportExportOptions options)
        => CreateMetric(
            session,
            fieldName,
            label,
            options,
            FormatDuration,
            null);

    private static ActivityReportMetric? CreateMappedUnknownMetric(
        FitSession? session,
        string sourceFieldName,
        string canonicalName,
        string label,
        string unit,
        ActivityReportExportOptions options)
    {
        FitField? field = session is null ? null : FindField(session.Fields, sourceFieldName);
        if (field is null || !TryGetDouble(field, out double value))
        {
            return CreateUnavailableMetric(canonicalName, label);
        }

        return new ActivityReportMetric(
            canonicalName,
            label,
            FormatNumber(value, options.Culture, 0),
            unit,
            ActivityReportFieldClassification.MappedFromUnmappedFitField,
            $"session.{sourceFieldName}",
            $"{label} is inferred from preserved unknown FIT field session.{sourceFieldName}; it is not publicly named in Profile.xlsx.");
    }

    private static ActivityReportTable CreateLapTable(FitSession session, ActivityReportExportOptions options)
    {
        ImmutableArray<ActivityReportTableColumn> columns =
        [
            new ActivityReportTableColumn("lap", "Lap"),
            new ActivityReportTableColumn("distance", "Distance"),
            new ActivityReportTableColumn("time", "Time"),
            new ActivityReportTableColumn("avg_speed", "Avg Speed")
        ];
        ImmutableArray<ActivityReportTableRow>.Builder rows = ImmutableArray.CreateBuilder<ActivityReportTableRow>(session.Laps.Length);

        for (int index = 0; index < session.Laps.Length; index++)
        {
            FitLap lap = session.Laps[index];
            string distance = FormatField(lap.Fields, "total_distance", options, value => FormatDistance(value, options), "km");
            string time = FormatField(lap.Fields, "total_timer_time", options, FormatDuration, null);
            string speed = FormatField(lap.Fields, "enhanced_avg_speed", options, value => $"{FormatNumber(value * 3.6d, options.Culture, 1)} km/h", null);
            rows.Add(new ActivityReportTableRow([FormatNumber(index + 1, options.Culture, 0), distance, time, speed]));
        }

        return new ActivityReportTable("laps", "Lap Summary", columns, rows.ToImmutable());
    }

    private static ImmutableArray<ActivityReportChart> CreateRecordCharts(
        FitSession session,
        string fieldName,
        string chartIdSuffix,
        string valueLabel,
        string? unit)
    {
        ImmutableArray<ActivityReportChartPoint> points = CreateChartPoints(session, fieldName);
        return points.IsDefaultOrEmpty
            ? ImmutableArray<ActivityReportChart>.Empty
            : ImmutableArray.Create(new ActivityReportChart(
                $"record-{fieldName}",
                chartIdSuffix,
                valueLabel,
                unit,
                points));
    }

    private static ImmutableArray<ActivityReportChartPoint> CreateChartPoints(FitSession session, string fieldName)
    {
        ImmutableArray<ActivityReportChartPoint>.Builder points = ImmutableArray.CreateBuilder<ActivityReportChartPoint>();
        foreach (FitRecord record in session.Records)
        {
            if (record.Original.TimestampUtc is not DateTimeOffset timestampUtc)
            {
                continue;
            }

            FitField? field = FindField(record.Fields, fieldName);
            if (field is not null && TryGetDouble(field, out double value))
            {
                points.Add(new ActivityReportChartPoint(timestampUtc, value));
            }
        }

        return DownSample(points.ToImmutable(), maxPointCount: 240);
    }

    private static ImmutableArray<ActivityReportChartPoint> DownSample(
        ImmutableArray<ActivityReportChartPoint> points,
        int maxPointCount)
    {
        if (points.Length <= maxPointCount)
        {
            return points;
        }

        ImmutableArray<ActivityReportChartPoint>.Builder sampledPoints = ImmutableArray.CreateBuilder<ActivityReportChartPoint>(maxPointCount);
        double step = (points.Length - 1d) / (maxPointCount - 1d);
        for (int index = 0; index < maxPointCount; index++)
        {
            int sourceIndex = (int)Math.Round(index * step, MidpointRounding.AwayFromZero);
            sampledPoints.Add(points[Math.Min(sourceIndex, points.Length - 1)]);
        }

        return sampledPoints.ToImmutable();
    }

    private static IEnumerable<ActivityReportMetric> CreateDeviceMetrics(FitActivity activity)
    {
        foreach (FitAncillaryMessage message in activity.AncillaryData.Messages)
        {
            if (!message.Original.MessageName.Equals("device_info", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            foreach (FitFieldSnapshot field in message.Fields)
            {
                if (field.OriginalName.Equals("product_name", StringComparison.OrdinalIgnoreCase)
                    || field.OriginalName.Equals("garmin_product", StringComparison.OrdinalIgnoreCase)
                    || field.OriginalName.Equals("software_version", StringComparison.OrdinalIgnoreCase))
                {
                    object? value = field.OriginalValues.FirstOrDefault()?.DecodedValue;
                    if (value is not null)
                    {
                        yield return new ActivityReportMetric(
                            $"device.{field.OriginalName}",
                            ToTitle(field.OriginalName),
                            Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty,
                            field.Units,
                            DetermineClassification(field),
                            $"device_info.{field.OriginalName}");
                    }
                }
            }
        }
    }

    private static ActivityReportMetric CreateUnavailableMetric(string canonicalName, string label)
        => new(
            canonicalName,
            label,
            "Unavailable",
            null,
            ActivityReportFieldClassification.Unavailable);

    private static void AddMetric(ImmutableArray<ActivityReportMetric>.Builder metrics, ActivityReportMetric? metric)
    {
        if (metric is not null)
        {
            metrics.Add(metric);
        }
    }

    private static FitField? FindField(IEnumerable<FitField> fields, string fieldName)
        => fields.FirstOrDefault(field => field.Original.OriginalName.Equals(fieldName, StringComparison.OrdinalIgnoreCase));

    private static bool TryGetDouble(FitField field, out double value)
    {
        object? rawValue = field.GetEffectiveDecodedValues().FirstOrDefault();
        return TryConvertToDouble(rawValue, out value);
    }

    private static bool TryConvertToDouble(object? rawValue, out double value)
    {
        switch (rawValue)
        {
            case null:
                value = default;
                return false;
            case double doubleValue:
                value = doubleValue;
                return true;
            case float floatValue:
                value = floatValue;
                return true;
            case decimal decimalValue:
                value = (double)decimalValue;
                return true;
            case IConvertible convertible:
                try
                {
                    value = convertible.ToDouble(CultureInfo.InvariantCulture);
                    return true;
                }
                catch (FormatException)
                {
                    value = default;
                    return false;
                }
                catch (InvalidCastException)
                {
                    value = default;
                    return false;
                }
                catch (OverflowException)
                {
                    value = default;
                    return false;
                }
            default:
                value = default;
                return false;
        }
    }

    private static string FormatField(
        IEnumerable<FitField> fields,
        string fieldName,
        ActivityReportExportOptions options,
        Func<double, string> formatter,
        string? unit)
    {
        FitField? field = FindField(fields, fieldName);
        if (field is null || !TryGetDouble(field, out double value))
        {
            return "Unavailable";
        }

        string formattedValue = formatter.Invoke(value);
        return string.IsNullOrWhiteSpace(unit) ? formattedValue : $"{formattedValue} {unit}";
    }

    private static string FormatDistance(double meters, ActivityReportExportOptions options)
        => FormatNumber(meters / 1000d, options.Culture, 2);

    private static string FormatNumber(double value, CultureInfo culture, int decimalDigits)
        => value.ToString($"N{decimalDigits}", culture);

    private static string FormatDuration(double seconds)
    {
        TimeSpan duration = TimeSpan.FromSeconds(Math.Max(seconds, 0d));
        return duration.TotalHours >= 1d
            ? duration.ToString(@"h\:mm\:ss", CultureInfo.InvariantCulture)
            : duration.ToString(@"m\:ss", CultureInfo.InvariantCulture);
    }

    private static ActivityReportFieldClassification DetermineClassification(FitField field)
        => field.Original.Kind switch
        {
            FitFieldKind.Developer => ActivityReportFieldClassification.DirectDeveloperField,
            FitFieldKind.Unknown => ActivityReportFieldClassification.MappedFromUnmappedFitField,
            _ => ActivityReportFieldClassification.DirectStandardFit
        };

    private static ActivityReportFieldClassification DetermineClassification(FitFieldSnapshot field)
        => field.Kind switch
        {
            FitFieldKind.Developer => ActivityReportFieldClassification.DirectDeveloperField,
            FitFieldKind.Unknown => ActivityReportFieldClassification.MappedFromUnmappedFitField,
            _ => ActivityReportFieldClassification.DirectStandardFit
        };

    private static string BuildTitle(FitActivity activity, FitSession? session, ActivityReportExportOptions options)
    {
        FitField? sportField = session is null ? null : FindField(session.Fields, "sport");
        string sport = sportField?.GetEffectiveDecodedValues().FirstOrDefault()?.ToString() ?? "Activity";
        string date = activity.CanonicalStartTimeUtc is DateTimeOffset startTimeUtc
            ? TimeZoneInfo.ConvertTime(startTimeUtc, options.LocalTimeZone).ToString("d", options.Culture)
            : "Undated";

        return $"{ToTitle(sport)} Report - {date}";
    }

    private static string CreateReportId(string sourceFilePath, DateTimeOffset? activityStartTimeUtc)
    {
        string sourceName = string.IsNullOrWhiteSpace(sourceFilePath)
            ? "activity"
            : Path.GetFileNameWithoutExtension(sourceFilePath);
        string timestamp = activityStartTimeUtc?.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture) ?? "unknown-date";
        return $"{SanitizeIdentifier(sourceName)}-{timestamp}";
    }

    private static string ToTitle(string value)
        => CultureInfo.InvariantCulture.TextInfo.ToTitleCase(value.Replace('_', ' ').Trim());

    private static string SanitizeIdentifier(string value)
    {
        char[] chars = value.Select(character => char.IsLetterOrDigit(character) ? character : '-').ToArray();
        return new string(chars).Trim('-').ToLowerInvariant();
    }
}
