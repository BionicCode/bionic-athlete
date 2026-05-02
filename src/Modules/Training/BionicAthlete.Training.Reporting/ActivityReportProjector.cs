namespace BionicAthlete.Training.Reporting;

using System.Collections.Immutable;
using System.Globalization;
using BionicAthlete.Application.Reporting;
using BionicAthlete.Training.Domain.Activities;
using BionicAthlete.Training.Domain.Fields;

/// <summary>
/// Projects one decoded FIT activity into the first View C report model.
/// </summary>
public sealed class ActivityReportProjector : IActivityReportProjector
{
    /// <inheritdoc />
    public Task<Report> ProjectAsync(
        FitActivity activity,
        ReportExportOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(activity);
        ArgumentNullException.ThrowIfNull(options);

        cancellationToken.ThrowIfCancellationRequested();

        FitSession? primarySession = activity.Sessions.FirstOrDefault();
        ImmutableArray<ReportDiagnostic>.Builder diagnostics = ImmutableArray.CreateBuilder<ReportDiagnostic>();
        ImmutableArray<ReportSection>.Builder sections = ImmutableArray.CreateBuilder<ReportSection>();

        if (primarySession is null)
        {
            diagnostics.Add(new ReportDiagnostic("NoSession", "The decoded activity does not contain a session."));
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

        var report = new Report(
            CreateReportId(sourceFilePath, startTimeUtc),
            title,
            sourceFilePath,
            startTimeUtc,
            options.ExportTimestampUtc,
            sections.ToImmutable(),
            diagnostics.ToImmutable());

        return Task.FromResult(report);
    }

    private static ReportSection CreateOverviewSection(
        FitActivity activity,
        FitSession? session,
        ReportExportOptions options)
    {
        ImmutableArray<ReportMetric>.Builder metrics = ImmutableArray.CreateBuilder<ReportMetric>();
        AddMetric(metrics, CreateMetric(session, "total_distance", "Distance", options, value => FormatDistance(value, options), "km"));
        AddMetric(metrics, CreateDurationMetric(session, "total_timer_time", "Time", options));
        AddMetric(metrics, CreateSpeedMetric(session, "enhanced_avg_speed", "Avg Speed", options));
        AddMetric(metrics, CreateMetric(session, "avg_power", "Avg Power", options, value => FormatNumber(value, options.Culture, 0), "W"));
        AddMetric(metrics, CreateMetric(session, "total_calories", "Total Calories Burned", options, value => FormatNumber(value, options.Culture, 0), "kcal"));
        AddMetric(metrics, CreateMetric(session, "training_load_peak", "Exercise Load", options, value => FormatNumber(value, options.Culture, 0), null));
        AddMetric(metrics, CreateActivityDateMetric(activity, options));

        return new ReportSection(
            "overview",
            "Overview",
            metrics.ToImmutable(),
            ImmutableArray<ReportChart>.Empty,
            ImmutableArray<ReportTable>.Empty);
    }

    private static ReportSection CreateTimingSection(
        FitActivity activity,
        FitSession? session,
        ReportExportOptions options)
    {
        ImmutableArray<ReportMetric>.Builder metrics = ImmutableArray.CreateBuilder<ReportMetric>();
        AddMetric(metrics, CreateDurationMetric(session, "total_timer_time", "Timer Time", options));
        AddMetric(metrics, CreateDurationMetric(session, "total_elapsed_time", "Elapsed Time", options));
        AddMetric(metrics, CreateDurationMetric(session, "total_moving_time", "Moving Time", options));

        DateTimeOffset? startTimeUtc = activity.CanonicalStartTimeUtc;
        if (startTimeUtc is DateTimeOffset startTime)
        {
            DateTimeOffset localStart = TimeZoneInfo.ConvertTime(startTime, options.LocalTimeZone);
            metrics.Add(new ReportMetric(
                "activity.local_start_time",
                "Local Start",
                localStart.ToString("f", options.Culture),
                null,
                ReportFieldClassification.DirectStandardFit,
                "activity.canonical_start_time"));
        }

        return new ReportSection(
            "timing",
            "Timing",
            metrics.ToImmutable(),
            ImmutableArray<ReportChart>.Empty,
            ImmutableArray<ReportTable>.Empty);
    }

    private static ReportSection CreatePowerSection(FitSession? session, ReportExportOptions options)
    {
        ImmutableArray<ReportMetric>.Builder metrics = ImmutableArray.CreateBuilder<ReportMetric>();
        AddMetric(metrics, CreateMetric(session, "avg_power", "Avg Power", options, value => FormatNumber(value, options.Culture, 0), "W"));
        AddMetric(metrics, CreateMetric(session, "max_power", "Max Power", options, value => FormatNumber(value, options.Culture, 0), "W"));
        AddMetric(metrics, CreateMetric(session, "normalized_power", "Normalized Power (NP)", options, value => FormatNumber(value, options.Culture, 0), "W"));
        AddMetric(metrics, CreateMetric(session, "intensity_factor", "IF", options, value => FormatNumber(value, options.Culture, 3), null));
        AddMetric(metrics, CreateMetric(session, "training_stress_score", "TSS", options, value => FormatNumber(value, options.Culture, 1), null));
        AddMetric(metrics, CreateMetric(session, "threshold_power", "FTP Setting", options, value => FormatNumber(value, options.Culture, 0), "W"));
        AddMetric(metrics, CreateMetric(session, "total_work", "Work", options, value => FormatNumber(value / 1000d, options.Culture, 0), "kJ"));

        ImmutableArray<ReportChart> charts = session is null
            ? ImmutableArray<ReportChart>.Empty
            : CreateRecordCharts(session, "power", "Power", "Power", "W");

        return new ReportSection(
            "power",
            "Power",
            metrics.ToImmutable(),
            charts,
            ImmutableArray<ReportTable>.Empty);
    }

    private static ReportSection CreateHeartRateSection(FitSession? session, ReportExportOptions options)
    {
        ImmutableArray<ReportMetric>.Builder metrics = ImmutableArray.CreateBuilder<ReportMetric>();
        AddMetric(metrics, CreateMetric(session, "avg_heart_rate", "Avg HR", options, value => FormatNumber(value, options.Culture, 0), "bpm"));
        AddMetric(metrics, CreateMetric(session, "max_heart_rate", "Max HR", options, value => FormatNumber(value, options.Culture, 0), "bpm"));

        ImmutableArray<ReportChart> charts = session is null
            ? ImmutableArray<ReportChart>.Empty
            : CreateRecordCharts(session, "heart_rate", "Heart Rate", "Heart Rate", "bpm");

        return new ReportSection(
            "heart-rate",
            "Heart Rate",
            metrics.ToImmutable(),
            charts,
            ImmutableArray<ReportTable>.Empty);
    }

    private static ReportSection CreateCadenceAndSpeedSection(FitSession? session, ReportExportOptions options)
    {
        ImmutableArray<ReportMetric>.Builder metrics = ImmutableArray.CreateBuilder<ReportMetric>();
        AddMetric(metrics, CreateSpeedMetric(session, "enhanced_avg_speed", "Avg Speed", options));
        AddMetric(metrics, CreateSpeedMetric(session, "enhanced_max_speed", "Max Speed", options));
        AddMetric(metrics, CreateMetric(session, "avg_cadence", "Avg Bike Cadence", options, value => FormatNumber(value, options.Culture, 0), "rpm"));
        AddMetric(metrics, CreateMetric(session, "max_cadence", "Max Bike Cadence", options, value => FormatNumber(value, options.Culture, 0), "rpm"));
        AddMetric(metrics, CreateMetric(session, "total_cycles", "Total Strokes", options, value => FormatNumber(value, options.Culture, 0), "cycles"));

        ImmutableArray<ReportChart>.Builder charts = ImmutableArray.CreateBuilder<ReportChart>();
        if (session is not null)
        {
            charts.AddRange(CreateRecordCharts(session, "enhanced_speed", "Speed", "Speed", "m/s"));
            charts.AddRange(CreateRecordCharts(session, "cadence", "Cadence", "Cadence", "rpm"));
        }

        return new ReportSection(
            "cadence-speed",
            "Cadence / Speed",
            metrics.ToImmutable(),
            charts.ToImmutable(),
            ImmutableArray<ReportTable>.Empty);
    }

    private static ReportSection CreateRespirationAndTemperatureSection(FitSession? session, ReportExportOptions options)
    {
        ImmutableArray<ReportMetric>.Builder metrics = ImmutableArray.CreateBuilder<ReportMetric>();
        AddMetric(metrics, CreateMetric(session, "enhanced_avg_respiration_rate", "Avg Respiration Rate", options, value => FormatNumber(value, options.Culture, 0), "brpm"));
        AddMetric(metrics, CreateMetric(session, "enhanced_min_respiration_rate", "Min Respiration Rate", options, value => FormatNumber(value, options.Culture, 0), "brpm"));
        AddMetric(metrics, CreateMetric(session, "enhanced_max_respiration_rate", "Max Respiration Rate", options, value => FormatNumber(value, options.Culture, 0), "brpm"));
        AddMetric(metrics, CreateMetric(session, "avg_temperature", "Avg Temp", options, value => FormatNumber(value, options.Culture, 1), "°C"));
        AddMetric(metrics, CreateMetric(session, "min_temperature", "Min Temp", options, value => FormatNumber(value, options.Culture, 1), "°C"));
        AddMetric(metrics, CreateMetric(session, "max_temperature", "Max Temp", options, value => FormatNumber(value, options.Culture, 1), "°C"));

        ImmutableArray<ReportChart>.Builder charts = ImmutableArray.CreateBuilder<ReportChart>();
        if (session is not null)
        {
            charts.AddRange(CreateRecordCharts(session, "enhanced_respiration_rate", "Respiration", "Respiration", "brpm"));
            charts.AddRange(CreateRecordCharts(session, "temperature", "Temperature", "Temperature", "°C"));
        }

        return new ReportSection(
            "respiration-temperature",
            "Respiration / Temperature",
            metrics.ToImmutable(),
            charts.ToImmutable(),
            ImmutableArray<ReportTable>.Empty);
    }

    private static ReportSection CreateStaminaAndHydrationSection(FitSession? session, ReportExportOptions options)
    {
        ImmutableArray<ReportMetric>.Builder metrics = ImmutableArray.CreateBuilder<ReportMetric>();
        AddMetric(metrics, CreateMappedUnknownMetric(session, "unknown_178", "session.est_sweat_loss", "Est. Sweat Loss", "ml", options));
        AddMetric(metrics, CreateMappedUnknownMetric(session, "unknown_205", "session.beginning_potential", "Beginning Potential", "%", options));
        AddMetric(metrics, CreateMappedUnknownMetric(session, "unknown_206", "session.ending_potential", "Ending Potential", "%", options));
        AddMetric(metrics, CreateMappedUnknownMetric(session, "unknown_207", "session.min_stamina", "Min Stamina", "%", options));

        return new ReportSection(
            "stamina-hydration",
            "Stamina / Hydration",
            metrics.ToImmutable(),
            ImmutableArray<ReportChart>.Empty,
            ImmutableArray<ReportTable>.Empty,
            "Stamina and sweat-loss values are shown as inferred aliases from preserved unknown FIT session fields when present.");
    }

    private static ReportSection CreateLapsSection(FitSession? session, ReportExportOptions options)
    {
        ImmutableArray<ReportTable> tables = session is null
            ? ImmutableArray<ReportTable>.Empty
            : ImmutableArray.Create(CreateLapTable(session, options));

        return new ReportSection(
            "laps",
            "Laps / Intervals",
            ImmutableArray<ReportMetric>.Empty,
            ImmutableArray<ReportChart>.Empty,
            tables);
    }

    private static ReportSection CreateDeviceAndSourceSection(FitActivity activity, ReportExportOptions options)
    {
        ImmutableArray<ReportMetric>.Builder metrics = ImmutableArray.CreateBuilder<ReportMetric>();
        metrics.Add(new ReportMetric(
            "source.file",
            "Source File",
            string.IsNullOrWhiteSpace(activity.Source.FilePath) ? "Unavailable" : activity.Source.FilePath,
            null,
            ReportFieldClassification.DirectStandardFit,
            "fit.source"));
        metrics.Add(new ReportMetric(
            "export.generated_at",
            "Generated",
            options.ExportTimestampUtc.ToString("O", CultureInfo.InvariantCulture),
            null,
            ReportFieldClassification.DerivedFromFit,
            null,
            "Timestamp is supplied by the application export request, not read from the FIT file."));

        foreach (ReportMetric metric in CreateDeviceMetrics(activity))
        {
            metrics.Add(metric);
        }

        return new ReportSection(
            "device-source",
            "Device and Source Metadata",
            metrics.ToImmutable(),
            ImmutableArray<ReportChart>.Empty,
            ImmutableArray<ReportTable>.Empty);
    }

    private static ReportSection CreateDataQualitySection()
        => new(
            "data-quality",
            "Data Quality and Provenance",
            ImmutableArray.Create(
                new ReportMetric("provenance.direct_fit", "Direct FIT", "Value comes directly from a documented or decoded FIT field.", null, ReportFieldClassification.DirectStandardFit),
                new ReportMetric("provenance.derived", "Formula-derived", "Value is calculated from decoded FIT fields.", null, ReportFieldClassification.DerivedFromFit),
                new ReportMetric("provenance.mapped_unknown", "Mapped unknown FIT field", "Value is inferred from preserved unknown FIT data and marked with a caveat.", null, ReportFieldClassification.MappedFromUnmappedFitField),
                new ReportMetric("provenance.unavailable", "Unavailable", "Value was not present or not reliably derivable from this activity.", null, ReportFieldClassification.Unavailable)),
            ImmutableArray<ReportChart>.Empty,
            ImmutableArray<ReportTable>.Empty,
            "The report is projected from decoded FIT data. It does not invent Garmin Connect-only values.");

    private static ReportMetric CreateActivityDateMetric(FitActivity activity, ReportExportOptions options)
    {
        if (activity.CanonicalStartTimeUtc is not DateTimeOffset startTimeUtc)
        {
            return CreateUnavailableMetric("activity.date", "Activity Date");
        }

        DateTimeOffset localStart = TimeZoneInfo.ConvertTime(startTimeUtc, options.LocalTimeZone);
        return new ReportMetric(
            "activity.date",
            "Activity Date",
            localStart.ToString("D", options.Culture),
            null,
            ReportFieldClassification.DirectStandardFit,
            "activity.canonical_start_time");
    }

    private static ReportMetric? CreateMetric(
        FitSession? session,
        string fieldName,
        string label,
        ReportExportOptions options,
        Func<double, string> formatter,
        string? unit)
    {
        FitField? field = session is null ? null : FindField(session.Fields, fieldName);
        if (field is null || !TryGetDouble(field, out double value))
        {
            return CreateUnavailableMetric($"session.{fieldName}", label);
        }

        return new ReportMetric(
            $"session.{fieldName}",
            label,
            formatter.Invoke(value),
            unit,
            DetermineClassification(field),
            $"session.{field.Original.OriginalName}");
    }

    private static ReportMetric? CreateSpeedMetric(
        FitSession? session,
        string fieldName,
        string label,
        ReportExportOptions options)
        => CreateMetric(
            session,
            fieldName,
            label,
            options,
            value => FormatNumber(value * 3.6d, options.Culture, 1),
            "km/h");

    private static ReportMetric? CreateDurationMetric(
        FitSession? session,
        string fieldName,
        string label,
        ReportExportOptions options)
        => CreateMetric(
            session,
            fieldName,
            label,
            options,
            FormatDuration,
            null);

    private static ReportMetric? CreateMappedUnknownMetric(
        FitSession? session,
        string sourceFieldName,
        string canonicalName,
        string label,
        string unit,
        ReportExportOptions options)
    {
        FitField? field = session is null ? null : FindField(session.Fields, sourceFieldName);
        if (field is null || !TryGetDouble(field, out double value))
        {
            return CreateUnavailableMetric(canonicalName, label);
        }

        return new ReportMetric(
            canonicalName,
            label,
            FormatNumber(value, options.Culture, 0),
            unit,
            ReportFieldClassification.MappedFromUnmappedFitField,
            $"session.{sourceFieldName}",
            $"{label} is inferred from preserved unknown FIT field session.{sourceFieldName}; it is not publicly named in Profile.xlsx.");
    }

    private static ReportTable CreateLapTable(FitSession session, ReportExportOptions options)
    {
        ImmutableArray<ReportTableColumn> columns =
        [
            new ReportTableColumn("lap", "Lap"),
            new ReportTableColumn("distance", "Distance"),
            new ReportTableColumn("time", "Time"),
            new ReportTableColumn("avg_speed", "Avg Speed")
        ];
        ImmutableArray<ReportTableRow>.Builder rows = ImmutableArray.CreateBuilder<ReportTableRow>(session.Laps.Length);

        for (int index = 0; index < session.Laps.Length; index++)
        {
            FitLap lap = session.Laps[index];
            string distance = FormatField(lap.Fields, "total_distance", options, value => FormatDistance(value, options), "km");
            string time = FormatField(lap.Fields, "total_timer_time", options, FormatDuration, null);
            string speed = FormatField(lap.Fields, "enhanced_avg_speed", options, value => $"{FormatNumber(value * 3.6d, options.Culture, 1)} km/h", null);
            rows.Add(new ReportTableRow([FormatNumber(index + 1, options.Culture, 0), distance, time, speed]));
        }

        return new ReportTable("laps", "Lap Summary", columns, rows.ToImmutable());
    }

    private static ImmutableArray<ReportChart> CreateRecordCharts(
        FitSession session,
        string fieldName,
        string chartIdSuffix,
        string valueLabel,
        string? unit)
    {
        ImmutableArray<ReportChartPoint> points = CreateChartPoints(session, fieldName);
        return points.IsDefaultOrEmpty
            ? ImmutableArray<ReportChart>.Empty
            : ImmutableArray.Create(new ReportChart(
                $"record-{fieldName}",
                chartIdSuffix,
                valueLabel,
                unit,
                points));
    }

    private static ImmutableArray<ReportChartPoint> CreateChartPoints(FitSession session, string fieldName)
    {
        ImmutableArray<ReportChartPoint>.Builder points = ImmutableArray.CreateBuilder<ReportChartPoint>();
        foreach (FitRecord record in session.Records)
        {
            if (record.Original.TimestampUtc is not DateTimeOffset timestampUtc)
            {
                continue;
            }

            FitField? field = FindField(record.Fields, fieldName);
            if (field is not null && TryGetDouble(field, out double value))
            {
                points.Add(new ReportChartPoint(timestampUtc, value));
            }
        }

        return DownSample(points.ToImmutable(), maxPointCount: 240);
    }

    private static ImmutableArray<ReportChartPoint> DownSample(
        ImmutableArray<ReportChartPoint> points,
        int maxPointCount)
    {
        if (points.Length <= maxPointCount)
        {
            return points;
        }

        ImmutableArray<ReportChartPoint>.Builder sampledPoints = ImmutableArray.CreateBuilder<ReportChartPoint>(maxPointCount);
        double step = (points.Length - 1d) / (maxPointCount - 1d);
        for (int index = 0; index < maxPointCount; index++)
        {
            int sourceIndex = (int)Math.Round(index * step, MidpointRounding.AwayFromZero);
            sampledPoints.Add(points[Math.Min(sourceIndex, points.Length - 1)]);
        }

        return sampledPoints.ToImmutable();
    }

    private static IEnumerable<ReportMetric> CreateDeviceMetrics(FitActivity activity)
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
                        yield return new ReportMetric(
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

    private static ReportMetric CreateUnavailableMetric(string canonicalName, string label)
        => new(
            canonicalName,
            label,
            "Unavailable",
            null,
            ReportFieldClassification.Unavailable);

    private static void AddMetric(ImmutableArray<ReportMetric>.Builder metrics, ReportMetric? metric)
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
        ReportExportOptions options,
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

    private static string FormatDistance(double meters, ReportExportOptions options)
        => FormatNumber(meters / 1000d, options.Culture, 2);

    private static string FormatNumber(double value, CultureInfo culture, int decimalDigits)
        => value.ToString($"N{decimalDigits}", culture);

    private static string FormatDuration(double seconds)
    {
        var duration = TimeSpan.FromSeconds(Math.Max(seconds, 0d));
        return duration.TotalHours >= 1d
            ? duration.ToString(@"h\:mm\:ss", CultureInfo.InvariantCulture)
            : duration.ToString(@"m\:ss", CultureInfo.InvariantCulture);
    }

    private static ReportFieldClassification DetermineClassification(FitField field)
        => field.Original.Kind switch
        {
            FitFieldKind.Developer => ReportFieldClassification.DirectDeveloperField,
            FitFieldKind.Unknown => ReportFieldClassification.MappedFromUnmappedFitField,
            _ => ReportFieldClassification.DirectStandardFit
        };

    private static ReportFieldClassification DetermineClassification(FitFieldSnapshot field)
        => field.Kind switch
        {
            FitFieldKind.Developer => ReportFieldClassification.DirectDeveloperField,
            FitFieldKind.Unknown => ReportFieldClassification.MappedFromUnmappedFitField,
            _ => ReportFieldClassification.DirectStandardFit
        };

    private static string BuildTitle(FitActivity activity, FitSession? session, ReportExportOptions options)
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
