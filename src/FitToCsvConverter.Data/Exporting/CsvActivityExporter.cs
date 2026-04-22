namespace FitToCsvConverter.Data.Exporting;

using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using FitToCsvConverter.Data.Activities;
using FitToCsvConverter.Data.Fields;

/// <summary>
/// Writes decoded activity data to structured export artifacts defined by <see cref="CsvExportRequest"/>.
/// </summary>
/// <remarks>
/// Array-valued FIT fields are written as a single CSV cell by joining element values with <c> | </c> in source order.
/// This keeps one selected FIT field mapped to one CSV column in the current export step.
/// </remarks>
public sealed class CsvActivityExporter : ICsvActivityExporter
{
    private const string ActiveCaloriesAlias = "Active Calories";
    private const string ActiveCaloriesExportName = "active_calories";
    private const string ActiveCaloriesFormula = "total_calories - metabolic_calories";
    private const string ArrayValueSeparator = " | ";
    private const string AvgMovingSpeedAlias = "Avg Moving Speed";
    private const string AvgMovingSpeedExportName = "avg_moving_speed";
    private const string AvgMovingSpeedFormula = "total_distance / moving_time";
    private const string CanonicalTimestampSemantics = "UTC ISO-8601";
    private const string DurationSemantics = "Numeric duration columns are normalized to seconds.";
    private const double FeetPerMeter = 3.28083989501312;
    private const string ManifestArtifactName = "manifest";
    private const string ManifestFileNameSuffix = "_manifest.json";
    private const string ManifestSchemaVersion = "1.0.0";
    private const double KilometersPerMeter = 0.001;
    private const double KilometersPerHourPerMeterPerSecond = 3.6;
    private const string MaxAveragePowerTwentyMinutesAlias = "Max Avg Power (20 min)";
    private const string MaxAveragePowerTwentyMinutesExportName = "max_avg_power_20min";
    private const string MaxAveragePowerTwentyMinutesFormula =
        "Max rolling average of record power over a 1200-second window using one-second sample-hold interpolation.";
    private const string MovingSpeedDerivationNotes =
        "Derived moving time uses direct total_moving_time when present; otherwise it sums record intervals where speed exceeds 0.1 m/s or distance increases.";
    private const string MovingTimeAlias = "Moving Time";
    private const string MovingTimeExportName = "moving_time";
    private const double MovingSpeedThresholdMetersPerSecond = 0.1;
    private const double MilesPerMeter = 0.000621371192237334;
    private const double MilesPerHourPerMeterPerSecond = 2.2369362920544;
    private const int PowerAverageWindowSeconds = 20 * 60;
    private const string TrainingLoadPeakAlias = "Exercise Load";
    private const string TotalCyclesAlias = "Total Strokes";

    // Garmin FIT SDK 21.195.0 exposes one invalid sentinel per base type through Dynastream.Fit.Fit.BaseType.
    // The exporter mirrors those sentinels here so invalid placeholders are blanked before structured CSV is written.
    private static readonly FrozenDictionary<string, decimal> s_numericInvalidValuesByBaseTypeName =
        new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
        {
            ["enum"] = 255m,
            ["sint8"] = 127m,
            ["uint8"] = 255m,
            ["sint16"] = 32767m,
            ["uint16"] = 65535m,
            ["sint32"] = 2147483647m,
            ["uint32"] = 4294967295m,
            ["uint8z"] = 0m,
            ["uint16z"] = 0m,
            ["uint32z"] = 0m,
            ["byte"] = 255m,
            ["sint64"] = 9223372036854775807m,
            ["uint64"] = 18446744073709551615m,
            ["uint64z"] = 0m,
        }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    private static readonly FrozenDictionary<string, string> s_fieldAliasesByOriginalName =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["training_load_peak"] = TrainingLoadPeakAlias,
            ["total_cycles"] = TotalCyclesAlias,
        }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    private static readonly FrozenDictionary<string, string> s_fieldNotesByOriginalName =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["training_load_peak"] = "Garmin Connect commonly presents this session summary as Exercise Load.",
            ["total_cycles"] = "Garmin cycling summaries commonly present this as Total Strokes.",
            ["workout_feel"] = "Garmin FIT SDK 21.195.0 exposes workout_feel as a nullable byte. The exporter preserves the raw byte value and does not impose undocumented device-specific scoring semantics.",
            ["workout_rpe"] = "Garmin FIT SDK 21.195.0 exposes workout_rpe as a nullable byte. The exporter preserves the raw byte value and does not impose undocumented device-specific scoring semantics.",
        }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    private static readonly JsonSerializerOptions s_manifestSerializerOptions = CreateManifestSerializerOptions();

    /// <inheritdoc/>
    public async Task<CsvExportResult> ExportAsync(CsvExportRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        EnsureStructuredCsvTarget(request.Options.Target);

        int anticipatedArtifactCount = request.NodeRequests.Length + request.SourceActivity.AncillaryData.Messages.Length + 1;
        ImmutableArray<ExportedArtifact>.Builder exportedArtifacts = ImmutableArray.CreateBuilder<ExportedArtifact>(anticipatedArtifactCount);

        // Respect the request order so callers can keep generated node artifacts stable across export and archive flows.
        foreach (CsvNodeExportRequest nodeRequest in request.NodeRequests)
        {
            cancellationToken.ThrowIfCancellationRequested();

            int rowCount = await ExportNodeAsync(
                request.SourceActivity,
                nodeRequest,
                request.Encoding,
                request.Delimiter,
                request.Options,
                cancellationToken).ConfigureAwait(false);

            exportedArtifacts.Add(
                new ExportedArtifact(
                    ExportedArtifactKind.NodeCsv,
                    nodeRequest.NodeType,
                    Path.GetFileName(nodeRequest.DestinationFilePath),
                    nodeRequest.DestinationFilePath,
                    rowCount));
        }

        foreach (ExportedArtifact ancillaryArtifact in await ExportAncillaryFamiliesAsync(request, cancellationToken).ConfigureAwait(false))
        {
            exportedArtifacts.Add(ancillaryArtifact);
        }

        ExportedArtifact manifestArtifact = await ExportManifestAsync(request, exportedArtifacts.ToImmutable(), cancellationToken).ConfigureAwait(false);
        exportedArtifacts.Add(manifestArtifact);

        return new CsvExportResult(exportedArtifacts.ToImmutable());
    }

    private static async Task<int> ExportNodeAsync(
        FitActivity sourceActivity,
        CsvNodeExportRequest nodeRequest,
        Encoding encoding,
        char delimiter,
        FitExportOptions exportOptions,
        CancellationToken cancellationToken)
    {
        string destinationDirectoryPath = Path.GetDirectoryName(nodeRequest.DestinationFilePath)
            ?? throw new InvalidOperationException($"Unable to determine a destination directory for '{nodeRequest.DestinationFilePath}'.");
        _ = Directory.CreateDirectory(destinationDirectoryPath);

        ImmutableArray<FitNode> nodes = EnumerateNodes(sourceActivity, nodeRequest.NodeType).ToImmutableArray();
        ImmutableArray<CsvColumnSelection> orderedColumns = nodeRequest.Columns
            .OrderBy(static column => column.Order)
            .ThenBy(static column => column.SourceName, StringComparer.OrdinalIgnoreCase)
            .ToImmutableArray();

        FrozenDictionary<FitExportColumnKey, FitField> referenceFieldLookup = nodes
            .SelectMany(static node => node.Fields)
            .GroupBy(static field => field.Original.ExportColumnKey)
            .ToFrozenDictionary(static group => group.Key, static group => group.First());
        ImmutableArray<ProjectedColumn> projectedColumns = BuildProjectedColumns(orderedColumns, referenceFieldLookup, exportOptions);
        ImmutableArray<DerivedSessionColumn> derivedSessionColumns = BuildDerivedSessionColumns(nodes, nodeRequest.NodeType, exportOptions);

        await using FileStream fileStream = new(
            nodeRequest.DestinationFilePath,
            new FileStreamOptions
            {
                Access = FileAccess.Write,
                Mode = FileMode.Create,
                Options = FileOptions.Asynchronous | FileOptions.SequentialScan,
                Share = FileShare.None
            });
        await using StreamWriter writer = new(fileStream, encoding);

        IEnumerable<string> headerCells = projectedColumns
            .Select(static projectedColumn => projectedColumn.Header)
            .Concat(derivedSessionColumns.Select(static column => column.Header));
        await WriteLineAsync(writer, headerCells, delimiter, cancellationToken).ConfigureAwait(false);

        int rowCount = 0;
        foreach (FitNode node in nodes)
        {
            cancellationToken.ThrowIfCancellationRequested();

            IReadOnlyDictionary<FitExportColumnKey, FitField> fieldLookup = node.Fields.ToDictionary(
                static field => field.Original.ExportColumnKey,
                static field => field);

            IEnumerable<string> directCellValues = projectedColumns.Select(projectedColumn =>
                fieldLookup.TryGetValue(projectedColumn.Selection.ColumnKey, out FitField? field)
                    ? FormatFieldValues(field, projectedColumn, exportOptions)
                    : RenderMissingValue(exportOptions));

            IEnumerable<string> derivedCellValues = node is FitSession session
                ? derivedSessionColumns.Select(column => FormatDerivedSessionValue(session, column.Kind, exportOptions))
                : [];

            await WriteLineAsync(writer, directCellValues.Concat(derivedCellValues), delimiter, cancellationToken).ConfigureAwait(false);
            rowCount++;
        }

        await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
        return rowCount;
    }

    private static async Task<ImmutableArray<ExportedArtifact>> ExportAncillaryFamiliesAsync(
        CsvExportRequest request,
        CancellationToken cancellationToken)
    {
        if (request.SourceActivity.AncillaryData.Messages.IsDefaultOrEmpty)
        {
            return ImmutableArray<ExportedArtifact>.Empty;
        }

        ImmutableArray<AncillaryMessageFamily> ancillaryFamilies = GroupAncillaryFamilies(request.SourceActivity.AncillaryData.Messages);
        ImmutableArray<ExportedArtifact>.Builder exportedArtifacts = ImmutableArray.CreateBuilder<ExportedArtifact>(ancillaryFamilies.Length);

        foreach (AncillaryMessageFamily ancillaryFamily in ancillaryFamilies)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string artifactFileName = BuildAncillaryArtifactFileName(ancillaryFamily, request.SourceFileNameWithoutExtension);
            string destinationFilePath = Path.Combine(request.OutputDirectoryPath, artifactFileName);
            int rowCount = await ExportAncillaryFamilyAsync(
                ancillaryFamily,
                destinationFilePath,
                request.Encoding,
                request.Delimiter,
                request.Options,
                cancellationToken).ConfigureAwait(false);

            exportedArtifacts.Add(
                new ExportedArtifact(
                    ExportedArtifactKind.AncillaryCsv,
                    FitNodeType.Ancillary,
                    artifactFileName,
                    destinationFilePath,
                    rowCount));
        }

        return exportedArtifacts.ToImmutable();
    }

    private static async Task<int> ExportAncillaryFamilyAsync(
        AncillaryMessageFamily ancillaryFamily,
        string destinationFilePath,
        Encoding encoding,
        char delimiter,
        FitExportOptions exportOptions,
        CancellationToken cancellationToken)
    {
        string destinationDirectoryPath = Path.GetDirectoryName(destinationFilePath)
            ?? throw new InvalidOperationException($"Unable to determine a destination directory for '{destinationFilePath}'.");
        _ = Directory.CreateDirectory(destinationDirectoryPath);

        await using FileStream fileStream = new(
            destinationFilePath,
            new FileStreamOptions
            {
                Access = FileAccess.Write,
                Mode = FileMode.Create,
                Options = FileOptions.Asynchronous | FileOptions.SequentialScan,
                Share = FileShare.None
            });
        await using StreamWriter writer = new(fileStream, encoding);

        ImmutableArray<AncillaryFieldColumn> fieldColumns = BuildAncillaryFieldColumns(ancillaryFamily.Messages, exportOptions);
        ImmutableArray<string> headerCells = BuildAncillaryHeaderCells(fieldColumns, exportOptions);
        await WriteLineAsync(writer, headerCells, delimiter, cancellationToken).ConfigureAwait(false);

        foreach (FitAncillaryMessage ancillaryMessage in ancillaryFamily.Messages)
        {
            cancellationToken.ThrowIfCancellationRequested();

            IReadOnlyDictionary<FitExportColumnKey, FitFieldSnapshot> fieldLookup = ancillaryMessage.Fields.ToDictionary(
                static field => field.ExportColumnKey,
                static field => field);

            IEnumerable<string> metadataCells =
            [
                ancillaryMessage.Original.MessageName,
                ancillaryMessage.Original.MessageNumber.ToString(CultureInfo.InvariantCulture),
                ancillaryMessage.Original.Identity.SequenceNumber.ToString(CultureInfo.InvariantCulture),
                ancillaryMessage.Original.Identity.MessageIndex?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
                ancillaryMessage.Original.LocalMessageNumber?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
                FormatTimestampForAncillaryMetadata(ancillaryMessage.Original.TimestampUtc, isLocalTimeDuplicate: false, exportOptions),
            ];

            IEnumerable<string> localTimeCells = exportOptions.IncludeLocalTimeColumns
                ? [FormatTimestampForAncillaryMetadata(ancillaryMessage.Original.TimestampUtc, isLocalTimeDuplicate: true, exportOptions)]
                : [];

            IEnumerable<string> fieldCells = fieldColumns.Select(column =>
                fieldLookup.TryGetValue(column.ColumnKey, out FitFieldSnapshot? field)
                    ? FormatFieldValues(field, exportOptions)
                    : RenderMissingValue(exportOptions));

            await WriteLineAsync(
                writer,
                metadataCells.Concat(localTimeCells).Concat(fieldCells),
                delimiter,
                cancellationToken).ConfigureAwait(false);
        }

        await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
        return ancillaryFamily.Messages.Length;
    }

    private static async Task<ExportedArtifact> ExportManifestAsync(
        CsvExportRequest request,
        ImmutableArray<ExportedArtifact> exportedArtifacts,
        CancellationToken cancellationToken)
    {
        string manifestFileName = request.SourceFileNameWithoutExtension + ManifestFileNameSuffix;
        string destinationFilePath = Path.Combine(request.OutputDirectoryPath, manifestFileName);
        CsvExportManifest manifest = BuildManifest(request, exportedArtifacts);

        string destinationDirectoryPath = Path.GetDirectoryName(destinationFilePath)
            ?? throw new InvalidOperationException($"Unable to determine a destination directory for '{destinationFilePath}'.");
        _ = Directory.CreateDirectory(destinationDirectoryPath);

        await using FileStream fileStream = new(
            destinationFilePath,
            new FileStreamOptions
            {
                Access = FileAccess.Write,
                Mode = FileMode.Create,
                Options = FileOptions.Asynchronous | FileOptions.SequentialScan,
                Share = FileShare.None
            });

        await JsonSerializer.SerializeAsync(fileStream, manifest, s_manifestSerializerOptions, cancellationToken).ConfigureAwait(false);
        await fileStream.FlushAsync(cancellationToken).ConfigureAwait(false);

        return new ExportedArtifact(
            ExportedArtifactKind.Manifest,
            FitNodeType.Activity,
            ManifestArtifactName,
            destinationFilePath,
            rowCount: 1);
    }

    private static ImmutableArray<DerivedSessionColumn> BuildDerivedSessionColumns(
        ImmutableArray<FitNode> nodes,
        FitNodeType nodeType,
        FitExportOptions exportOptions)
    {
        if (nodeType != FitNodeType.Session)
        {
            return ImmutableArray<DerivedSessionColumn>.Empty;
        }

        ImmutableArray<FitSession> sessions = nodes.OfType<FitSession>().ToImmutableArray();
        ImmutableArray<DerivedSessionColumn>.Builder builder = ImmutableArray.CreateBuilder<DerivedSessionColumn>();

        TryAddDerivedSessionColumn(builder, sessions, DerivedSessionFieldKind.ActiveCalories, ActiveCaloriesExportName, "kcal", exportOptions);
        TryAddDerivedSessionColumn(builder, sessions, DerivedSessionFieldKind.MovingTime, MovingTimeExportName, "s", exportOptions);
        TryAddDerivedSessionColumn(
            builder,
            sessions,
            DerivedSessionFieldKind.AverageMovingSpeed,
            AvgMovingSpeedExportName,
            exportOptions.UnitSystem == FitExportUnitSystem.Metric ? "km/h" : "mph",
            exportOptions);
        TryAddDerivedSessionColumn(builder, sessions, DerivedSessionFieldKind.MaxAveragePowerTwentyMinutes, MaxAveragePowerTwentyMinutesExportName, "watts", exportOptions);

        return builder.ToImmutable();
    }

    private static void TryAddDerivedSessionColumn(
        ImmutableArray<DerivedSessionColumn>.Builder builder,
        ImmutableArray<FitSession> sessions,
        DerivedSessionFieldKind kind,
        string exportName,
        string? unit,
        FitExportOptions exportOptions)
    {
        if (!sessions.Any(session => TryGetDerivedSessionValue(session, kind, exportOptions, out _)))
        {
            return;
        }

        builder.Add(new DerivedSessionColumn(kind, BuildDerivedHeader(exportName, unit, exportOptions)));
    }

    private static string BuildDerivedHeader(string exportName, string? unit, FitExportOptions exportOptions)
    {
        if (!exportOptions.IncludeUnitSuffixInHeaders || string.IsNullOrWhiteSpace(unit))
        {
            return exportName;
        }

        return $"{exportName} [{unit}]";
    }

    private static string FormatDerivedSessionValue(FitSession session, DerivedSessionFieldKind kind, FitExportOptions exportOptions)
        => TryGetDerivedSessionValue(session, kind, exportOptions, out object? derivedValue)
            ? FormatSingleValue(derivedValue)
            : RenderMissingValue(exportOptions);

    private static bool TryGetDerivedSessionValue(
        FitSession session,
        DerivedSessionFieldKind kind,
        FitExportOptions exportOptions,
        out object? derivedValue)
    {
        switch (kind)
        {
            case DerivedSessionFieldKind.ActiveCalories:
                if (TryGetActiveCalories(session, out double activeCalories))
                {
                    derivedValue = activeCalories;
                    return true;
                }

                break;

            case DerivedSessionFieldKind.MovingTime:
                if (TryGetMovingTimeSeconds(session, out double movingTimeSeconds))
                {
                    derivedValue = movingTimeSeconds;
                    return true;
                }

                break;

            case DerivedSessionFieldKind.AverageMovingSpeed:
                if (TryGetAverageMovingSpeed(session, exportOptions.UnitSystem, out double averageMovingSpeed))
                {
                    derivedValue = averageMovingSpeed;
                    return true;
                }

                break;

            case DerivedSessionFieldKind.MaxAveragePowerTwentyMinutes:
                if (TryGetMaximumAveragePowerTwentyMinutes(session, out double maximumAveragePowerTwentyMinutes))
                {
                    derivedValue = maximumAveragePowerTwentyMinutes;
                    return true;
                }

                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unsupported derived session field.");
        }

        derivedValue = null;
        return false;
    }

    private static bool TryGetActiveCalories(FitSession session, out double activeCalories)
    {
        IReadOnlyDictionary<string, FitField> fieldLookup = CreateFieldLookup(session.Fields);
        if (!TryGetCanonicalNumericFieldValue(fieldLookup, "total_calories", out double totalCalories)
            || !TryGetCanonicalNumericFieldValue(fieldLookup, "metabolic_calories", out double metabolicCalories))
        {
            activeCalories = default;
            return false;
        }

        activeCalories = totalCalories - metabolicCalories;
        return true;
    }

    private static bool TryGetMovingTimeSeconds(FitSession session, out double movingTimeSeconds)
    {
        IReadOnlyDictionary<string, FitField> fieldLookup = CreateFieldLookup(session.Fields);
        if (TryGetCanonicalNumericFieldValue(fieldLookup, "total_moving_time", out movingTimeSeconds)
            || TryGetCanonicalNumericFieldValue(fieldLookup, "moving_time", out movingTimeSeconds))
        {
            return true;
        }

        return TryDeriveMovingTimeSeconds(session, out movingTimeSeconds);
    }

    private static bool TryDeriveMovingTimeSeconds(FitSession session, out double movingTimeSeconds)
    {
        movingTimeSeconds = 0d;
        bool hasMovingInterval = false;

        for (int index = 0; index < session.Records.Length - 1; index++)
        {
            FitRecord currentRecord = session.Records[index];
            FitRecord nextRecord = session.Records[index + 1];

            if (currentRecord.Original.TimestampUtc is not DateTimeOffset currentTimestampUtc
                || nextRecord.Original.TimestampUtc is not DateTimeOffset nextTimestampUtc)
            {
                continue;
            }

            double intervalSeconds = (nextTimestampUtc - currentTimestampUtc).TotalSeconds;
            if (intervalSeconds <= 0d)
            {
                continue;
            }

            if (!IsMovingInterval(currentRecord, nextRecord))
            {
                continue;
            }

            movingTimeSeconds += intervalSeconds;
            hasMovingInterval = true;
        }

        return hasMovingInterval;
    }

    private static bool IsMovingInterval(FitRecord currentRecord, FitRecord nextRecord)
    {
        if (TryGetCanonicalNumericFieldValue(CreateFieldLookup(currentRecord.Fields), "enhanced_speed", out double enhancedSpeedMetersPerSecond)
            && enhancedSpeedMetersPerSecond > MovingSpeedThresholdMetersPerSecond)
        {
            return true;
        }

        if (TryGetCanonicalNumericFieldValue(CreateFieldLookup(currentRecord.Fields), "speed", out double speedMetersPerSecond)
            && speedMetersPerSecond > MovingSpeedThresholdMetersPerSecond)
        {
            return true;
        }

        IReadOnlyDictionary<string, FitField> currentFieldLookup = CreateFieldLookup(currentRecord.Fields);
        IReadOnlyDictionary<string, FitField> nextFieldLookup = CreateFieldLookup(nextRecord.Fields);
        return TryGetCanonicalNumericFieldValue(currentFieldLookup, "distance", out double currentDistanceMeters)
            && TryGetCanonicalNumericFieldValue(nextFieldLookup, "distance", out double nextDistanceMeters)
            && nextDistanceMeters > currentDistanceMeters;
    }

    private static bool TryGetAverageMovingSpeed(
        FitSession session,
        FitExportUnitSystem unitSystem,
        out double averageMovingSpeed)
    {
        IReadOnlyDictionary<string, FitField> fieldLookup = CreateFieldLookup(session.Fields);
        if (!TryGetCanonicalNumericFieldValue(fieldLookup, "total_distance", out double totalDistanceMeters)
            || !TryGetMovingTimeSeconds(session, out double movingTimeSeconds)
            || movingTimeSeconds <= 0d)
        {
            averageMovingSpeed = default;
            return false;
        }

        double speedMetersPerSecond = totalDistanceMeters / movingTimeSeconds;
        averageMovingSpeed = unitSystem == FitExportUnitSystem.Metric
            ? speedMetersPerSecond * KilometersPerHourPerMeterPerSecond
            : speedMetersPerSecond * MilesPerHourPerMeterPerSecond;
        return true;
    }

    private static bool TryGetMaximumAveragePowerTwentyMinutes(FitSession session, out double maximumAveragePowerTwentyMinutes)
    {
        ImmutableArray<double> oneSecondPowerSamples = BuildOneSecondPowerSamples(session);
        if (oneSecondPowerSamples.IsDefaultOrEmpty)
        {
            maximumAveragePowerTwentyMinutes = default;
            return false;
        }

        int windowSize = Math.Min(PowerAverageWindowSeconds, oneSecondPowerSamples.Length);
        double rollingSum = 0d;
        for (int index = 0; index < windowSize; index++)
        {
            rollingSum += oneSecondPowerSamples[index];
        }

        double maximumAverage = rollingSum / windowSize;
        for (int index = windowSize; index < oneSecondPowerSamples.Length; index++)
        {
            rollingSum += oneSecondPowerSamples[index];
            rollingSum -= oneSecondPowerSamples[index - windowSize];
            double currentAverage = rollingSum / windowSize;
            if (currentAverage > maximumAverage)
            {
                maximumAverage = currentAverage;
            }
        }

        maximumAveragePowerTwentyMinutes = maximumAverage;
        return true;
    }

    private static ImmutableArray<double> BuildOneSecondPowerSamples(FitSession session)
    {
        if (session.Records.Length < 2)
        {
            return ImmutableArray<double>.Empty;
        }

        // FIT record timestamps are second-resolution in Garmin activity files.
        // A one-second sample-hold stream keeps the derivation deterministic and easy to audit.
        ImmutableArray<double>.Builder builder = ImmutableArray.CreateBuilder<double>();
        for (int index = 0; index < session.Records.Length - 1; index++)
        {
            FitRecord currentRecord = session.Records[index];
            FitRecord nextRecord = session.Records[index + 1];

            if (currentRecord.Original.TimestampUtc is not DateTimeOffset currentTimestampUtc
                || nextRecord.Original.TimestampUtc is not DateTimeOffset nextTimestampUtc)
            {
                continue;
            }

            int intervalSeconds = (int)Math.Round(
                (nextTimestampUtc - currentTimestampUtc).TotalSeconds,
                MidpointRounding.AwayFromZero);
            if (intervalSeconds <= 0)
            {
                continue;
            }

            IReadOnlyDictionary<string, FitField> fieldLookup = CreateFieldLookup(currentRecord.Fields);
            if (!TryGetCanonicalNumericFieldValue(fieldLookup, "power", out double powerWatts)
                && !TryGetCanonicalNumericFieldValue(fieldLookup, "enhanced_power", out powerWatts))
            {
                continue;
            }

            for (int sampleIndex = 0; sampleIndex < intervalSeconds; sampleIndex++)
            {
                builder.Add(powerWatts);
            }
        }

        return builder.ToImmutable();
    }

    private static IReadOnlyDictionary<string, FitField> CreateFieldLookup(ImmutableArray<FitField> fields)
        => fields.ToDictionary(static field => field.Original.OriginalName, static field => field, StringComparer.OrdinalIgnoreCase);

    private static bool TryGetCanonicalNumericFieldValue(
        IReadOnlyDictionary<string, FitField> fieldLookup,
        string fieldName,
        out double numericValue)
    {
        if (!fieldLookup.TryGetValue(fieldName, out FitField? field))
        {
            numericValue = default;
            return false;
        }

        return TryGetCanonicalNumericFieldValue(field, out numericValue);
    }

    private static bool TryGetCanonicalNumericFieldValue(FitField field, out double numericValue)
    {
        ImmutableArray<object?> values = GetExportValues(field);
        if (values.Length != 1)
        {
            numericValue = default;
            return false;
        }

        return TryNormalizeNumericValueToCanonicalUnits(values[0], field.Original.Units, out numericValue)
            || TryConvertToDouble(values[0], out numericValue);
    }

    private static bool TryNormalizeNumericValueToCanonicalUnits(object? value, string? sourceUnit, out double normalizedValue)
    {
        if (TryNormalizeDurationToSeconds(value, sourceUnit, out normalizedValue))
        {
            return true;
        }

        if (TryNormalizeDistanceToMeters(value, sourceUnit, out normalizedValue))
        {
            return true;
        }

        if (TryNormalizeSpeedToMetersPerSecond(value, sourceUnit, out normalizedValue))
        {
            return true;
        }

        return false;
    }

    private static ImmutableArray<AncillaryMessageFamily> GroupAncillaryFamilies(ImmutableArray<FitAncillaryMessage> ancillaryMessages)
    {
        Dictionary<AncillaryFamilyKey, ImmutableArray<FitAncillaryMessage>.Builder> familiesByKey = [];
        foreach (FitAncillaryMessage ancillaryMessage in ancillaryMessages)
        {
            AncillaryFamilyKey familyKey = new(ancillaryMessage.Original.MessageName, ancillaryMessage.Original.MessageNumber);
            if (!familiesByKey.TryGetValue(familyKey, out ImmutableArray<FitAncillaryMessage>.Builder? familyBuilder))
            {
                familyBuilder = ImmutableArray.CreateBuilder<FitAncillaryMessage>();
                familiesByKey.Add(familyKey, familyBuilder);
            }

            familyBuilder.Add(ancillaryMessage);
        }

        return familiesByKey
            .OrderBy(static family => family.Key.MessageNumber)
            .ThenBy(static family => family.Key.MessageName, StringComparer.OrdinalIgnoreCase)
            .Select(static family => new AncillaryMessageFamily(family.Key, family.Value.ToImmutable()))
            .ToImmutableArray();
    }

    private static ImmutableArray<AncillaryFieldColumn> BuildAncillaryFieldColumns(
        ImmutableArray<FitAncillaryMessage> ancillaryMessages,
        FitExportOptions exportOptions)
    {
        Dictionary<FitExportColumnKey, AncillaryFieldColumn> columnsByKey = [];
        foreach (FitAncillaryMessage ancillaryMessage in ancillaryMessages)
        {
            foreach (FitFieldSnapshot field in ancillaryMessage.Fields)
            {
                if (columnsByKey.ContainsKey(field.ExportColumnKey))
                {
                    continue;
                }

                columnsByKey.Add(
                    field.ExportColumnKey,
                    new AncillaryFieldColumn(
                        field.ExportColumnKey,
                        BuildHeader(field.OriginalName, field, exportOptions, isLocalTimeDuplicate: false)));
            }
        }

        return columnsByKey.Values.ToImmutableArray();
    }

    private static ImmutableArray<string> BuildAncillaryHeaderCells(
        ImmutableArray<AncillaryFieldColumn> fieldColumns,
        FitExportOptions exportOptions)
    {
        ImmutableArray<string>.Builder builder = ImmutableArray.CreateBuilder<string>(6 + fieldColumns.Length + (exportOptions.IncludeLocalTimeColumns ? 1 : 0));
        builder.Add("message_name");
        builder.Add("message_number");
        builder.Add("sequence_number");
        builder.Add("message_index");
        builder.Add("local_message_number");
        builder.Add(exportOptions.IncludeUnitSuffixInHeaders ? "timestamp [UTC]" : "timestamp");
        if (exportOptions.IncludeLocalTimeColumns)
        {
            builder.Add(exportOptions.IncludeUnitSuffixInHeaders ? "timestamp [Local]" : "timestamp_local");
        }

        foreach (AncillaryFieldColumn fieldColumn in fieldColumns)
        {
            builder.Add(fieldColumn.Header);
        }

        return builder.ToImmutable();
    }

    private static string FormatTimestampForAncillaryMetadata(
        DateTimeOffset? timestampUtc,
        bool isLocalTimeDuplicate,
        FitExportOptions exportOptions)
    {
        if (timestampUtc is not DateTimeOffset nonNullTimestampUtc)
        {
            return RenderMissingValue(exportOptions);
        }

        object normalizedTimestamp = NormalizeTimestampValue(nonNullTimestampUtc, isLocalTimeDuplicate, exportOptions.LocalTimeZone)
            ?? nonNullTimestampUtc;
        return FormatSingleValue(normalizedTimestamp);
    }

    private static string FormatFieldValues(FitFieldSnapshot field, FitExportOptions exportOptions)
    {
        ImmutableArray<object?> values = field.OriginalValues
            .Select(originalValue => IsInvalidOriginalValue(field.BaseTypeName, originalValue.RawValue, originalValue.DecodedValue)
                ? null
                : originalValue.DecodedValue)
            .ToImmutableArray();

        if (values.IsDefaultOrEmpty)
        {
            return RenderMissingValue(exportOptions);
        }

        if (values.Length == 1)
        {
            object? normalizedValue = NormalizeValue(field, values[0], isLocalTimeDuplicate: false, exportOptions);
            return normalizedValue is null
                ? RenderMissingValue(exportOptions)
                : FormatSingleValue(normalizedValue);
        }

        ImmutableArray<string> formattedValues = values
            .Select(value =>
            {
                object? normalizedValue = NormalizeValue(field, value, isLocalTimeDuplicate: false, exportOptions);
                return normalizedValue is null ? RenderMissingValue(exportOptions) : FormatSingleValue(normalizedValue);
            })
            .ToImmutableArray();

        return formattedValues.All(string.IsNullOrEmpty)
            ? RenderMissingValue(exportOptions)
            : string.Join(ArrayValueSeparator, formattedValues);
    }

    private static CsvExportManifest BuildManifest(CsvExportRequest request, ImmutableArray<ExportedArtifact> exportedArtifacts)
    {
        FrozenDictionary<string, ExportedArtifact> exportedArtifactsByName = exportedArtifacts
            .GroupBy(static artifact => artifact.ArtifactName, StringComparer.OrdinalIgnoreCase)
            .ToFrozenDictionary(static group => group.Key, static group => group.Last(), StringComparer.OrdinalIgnoreCase);

        ImmutableArray<AncillaryMessageFamily> ancillaryFamilies = GroupAncillaryFamilies(request.SourceActivity.AncillaryData.Messages);
        ImmutableArray<CsvExportMessageFamilyManifestEntry> includedMessageFamilies = BuildIncludedMessageFamilies(
            request,
            ancillaryFamilies,
            exportedArtifactsByName);
        ImmutableArray<CsvExportMessageFamilyManifestEntry> omittedMessageFamilies = BuildOmittedMessageFamilies(
            request,
            ancillaryFamilies,
            exportedArtifactsByName);
        ImmutableArray<CsvExportFieldDictionaryEntry> fieldDictionary = BuildFieldDictionary(
            request,
            ancillaryFamilies,
            exportedArtifactsByName);

        return new CsvExportManifest
        {
            ExportSchemaVersion = ManifestSchemaVersion,
            ExporterVersion = typeof(CsvActivityExporter).Assembly.GetName().Version?.ToString() ?? "0.0.0.0",
            SourceDisplayName = request.SourceActivity.Source.DisplayName,
            SourceFilePath = request.SourceActivity.Source.FilePath,
            TimezoneSemantics = new CsvExportTimezoneSemantics
            {
                CanonicalTimestampColumns = CanonicalTimestampSemantics,
                IncludesLocalTimeColumns = request.Options.IncludeLocalTimeColumns,
                LocalTimeZoneId = request.Options.IncludeLocalTimeColumns ? request.Options.LocalTimeZone.Id : null,
                DurationColumns = DurationSemantics,
            },
            IncludedMessageFamilies = includedMessageFamilies,
            OmittedMessageFamilies = omittedMessageFamilies,
            HasDeveloperFields = ContainsDeveloperFields(request.SourceActivity),
            HasUnknownOrVendorFields = ContainsUnknownOrVendorFields(request.SourceActivity),
            FieldDictionary = fieldDictionary,
        };
    }

    private static ImmutableArray<CsvExportMessageFamilyManifestEntry> BuildIncludedMessageFamilies(
        CsvExportRequest request,
        ImmutableArray<AncillaryMessageFamily> ancillaryFamilies,
        FrozenDictionary<string, ExportedArtifact> exportedArtifactsByName)
    {
        ImmutableArray<CsvExportMessageFamilyManifestEntry>.Builder builder = ImmutableArray.CreateBuilder<CsvExportMessageFamilyManifestEntry>();

        foreach (CsvNodeExportRequest nodeRequest in request.NodeRequests)
        {
            if (!exportedArtifactsByName.TryGetValue(Path.GetFileName(nodeRequest.DestinationFilePath), out ExportedArtifact? exportedArtifact))
            {
                continue;
            }

            ImmutableArray<FitNode> nodes = EnumerateNodes(request.SourceActivity, nodeRequest.NodeType).ToImmutableArray();
            builder.Add(
                new CsvExportMessageFamilyManifestEntry
                {
                    MessageFamily = nodes.FirstOrDefault()?.Original.MessageName ?? nodeRequest.NodeType.ToString().ToLowerInvariant(),
                    MessageNumber = nodes.FirstOrDefault()?.Original.MessageNumber ?? 0,
                    ArtifactName = exportedArtifact.ArtifactName,
                    ArtifactFileName = Path.GetFileName(exportedArtifact.FilePath),
                    ArtifactKind = exportedArtifact.Kind,
                    NodeType = nodeRequest.NodeType.ToString(),
                    RowCount = exportedArtifact.RowCount,
                    ContainsDeveloperFields = nodes.SelectMany(static node => node.Fields).Any(static field => field.Original.Kind == FitFieldKind.Developer),
                    ContainsUnknownOrVendorFields = nodes.SelectMany(static node => node.Fields).Any(static field => field.Original.Kind == FitFieldKind.Unknown),
                });
        }

        foreach (AncillaryMessageFamily ancillaryFamily in ancillaryFamilies)
        {
            string artifactFileName = BuildAncillaryArtifactFileName(ancillaryFamily, request.SourceFileNameWithoutExtension);
            if (!exportedArtifactsByName.TryGetValue(artifactFileName, out ExportedArtifact? exportedArtifact))
            {
                continue;
            }

            builder.Add(
                new CsvExportMessageFamilyManifestEntry
                {
                    MessageFamily = ancillaryFamily.Key.MessageName,
                    MessageNumber = ancillaryFamily.Key.MessageNumber,
                    ArtifactName = exportedArtifact.ArtifactName,
                    ArtifactFileName = Path.GetFileName(exportedArtifact.FilePath),
                    ArtifactKind = exportedArtifact.Kind,
                    NodeType = FitNodeType.Ancillary.ToString(),
                    RowCount = exportedArtifact.RowCount,
                    ContainsDeveloperFields = ancillaryFamily.Messages.SelectMany(static message => message.Fields).Any(static field => field.Kind == FitFieldKind.Developer),
                    ContainsUnknownOrVendorFields = ancillaryFamily.Messages.SelectMany(static message => message.Fields).Any(static field => field.Kind == FitFieldKind.Unknown)
                        || string.Equals(ancillaryFamily.Key.MessageName, "unknown", StringComparison.OrdinalIgnoreCase),
                });
        }

        return builder
            .OrderBy(static entry => entry.MessageNumber)
            .ThenBy(static entry => entry.MessageFamily, StringComparer.OrdinalIgnoreCase)
            .ToImmutableArray();
    }

    private static ImmutableArray<CsvExportMessageFamilyManifestEntry> BuildOmittedMessageFamilies(
        CsvExportRequest request,
        ImmutableArray<AncillaryMessageFamily> ancillaryFamilies,
        FrozenDictionary<string, ExportedArtifact> exportedArtifactsByName)
    {
        HashSet<FitNodeType> selectedNodeTypes = request.NodeRequests.Select(static nodeRequest => nodeRequest.NodeType).ToHashSet();
        ImmutableArray<CsvExportMessageFamilyManifestEntry>.Builder builder = ImmutableArray.CreateBuilder<CsvExportMessageFamilyManifestEntry>();

        AddOmittedNodeFamilyIfNeeded(builder, request.SourceActivity.Fields, FitNodeType.Activity, selectedNodeTypes.Contains(FitNodeType.Activity));
        AddOmittedNodeFamilyIfNeeded(
            builder,
            request.SourceActivity.Sessions.SelectMany(static session => session.Fields).ToImmutableArray(),
            FitNodeType.Session,
            selectedNodeTypes.Contains(FitNodeType.Session));
        AddOmittedNodeFamilyIfNeeded(
            builder,
            request.SourceActivity.Sessions.SelectMany(static session => session.Laps).SelectMany(static lap => lap.Fields).ToImmutableArray(),
            FitNodeType.Lap,
            selectedNodeTypes.Contains(FitNodeType.Lap));
        AddOmittedNodeFamilyIfNeeded(
            builder,
            request.SourceActivity.Sessions.SelectMany(static session => session.Records).SelectMany(static record => record.Fields).ToImmutableArray(),
            FitNodeType.Record,
            selectedNodeTypes.Contains(FitNodeType.Record));

        foreach (AncillaryMessageFamily ancillaryFamily in ancillaryFamilies)
        {
            string artifactFileName = BuildAncillaryArtifactFileName(ancillaryFamily, request.SourceFileNameWithoutExtension);
            if (exportedArtifactsByName.ContainsKey(artifactFileName))
            {
                continue;
            }

            builder.Add(
                new CsvExportMessageFamilyManifestEntry
                {
                    MessageFamily = ancillaryFamily.Key.MessageName,
                    MessageNumber = ancillaryFamily.Key.MessageNumber,
                    ArtifactName = artifactFileName,
                    ArtifactFileName = artifactFileName,
                    ArtifactKind = ExportedArtifactKind.AncillaryCsv,
                    NodeType = FitNodeType.Ancillary.ToString(),
                    RowCount = 0,
                    ContainsDeveloperFields = ancillaryFamily.Messages.SelectMany(static message => message.Fields).Any(static field => field.Kind == FitFieldKind.Developer),
                    ContainsUnknownOrVendorFields = ancillaryFamily.Messages.SelectMany(static message => message.Fields).Any(static field => field.Kind == FitFieldKind.Unknown),
                    OmissionReason = "Ancillary message family was present in the decoded activity but not written to the structured export bundle.",
                });
        }

        return builder
            .OrderBy(static entry => entry.MessageNumber)
            .ThenBy(static entry => entry.MessageFamily, StringComparer.OrdinalIgnoreCase)
            .ToImmutableArray();
    }

    private static void AddOmittedNodeFamilyIfNeeded(
        ImmutableArray<CsvExportMessageFamilyManifestEntry>.Builder builder,
        ImmutableArray<FitField> fields,
        FitNodeType nodeType,
        bool isExported)
    {
        if (isExported || fields.IsDefaultOrEmpty)
        {
            return;
        }

        FitField firstField = fields[0];
        builder.Add(
            new CsvExportMessageFamilyManifestEntry
            {
                MessageFamily = firstField.Original.MessageName,
                MessageNumber = firstField.Original.Key.MessageNumber,
                ArtifactName = nodeType.ToString().ToLowerInvariant(),
                ArtifactFileName = nodeType.ToString().ToLowerInvariant() + ".csv",
                ArtifactKind = ExportedArtifactKind.NodeCsv,
                NodeType = nodeType.ToString(),
                RowCount = 0,
                ContainsDeveloperFields = fields.Any(static field => field.Original.Kind == FitFieldKind.Developer),
                ContainsUnknownOrVendorFields = fields.Any(static field => field.Original.Kind == FitFieldKind.Unknown),
                OmissionReason = "Message family was present in the decoded activity but not selected for node-level CSV export.",
            });
    }

    private static ImmutableArray<CsvExportFieldDictionaryEntry> BuildFieldDictionary(
        CsvExportRequest request,
        ImmutableArray<AncillaryMessageFamily> ancillaryFamilies,
        FrozenDictionary<string, ExportedArtifact> exportedArtifactsByName)
    {
        Dictionary<FitExportColumnKey, CsvExportColumnSelectionMetadata> selectedColumnsByKey = request.NodeRequests
            .SelectMany(static nodeRequest => nodeRequest.Columns.Select(column => new
            {
                nodeRequest.DestinationFilePath,
                Column = column
            }))
            .ToDictionary(
                static entry => entry.Column.ColumnKey,
                static entry => new CsvExportColumnSelectionMetadata(
                    entry.Column.ColumnName,
                    Path.GetFileName(entry.DestinationFilePath)),
                comparer: EqualityComparer<FitExportColumnKey>.Default);

        // The field dictionary is meant to describe the export schema, not every row instance.
        // Deduplicate by stable export column key so repeated record/session fields produce one dictionary entry.
        Dictionary<FitExportColumnKey, CsvExportFieldDictionaryEntry> uniqueFieldEntriesByColumnKey = [];

        AddNodeFieldEntries(uniqueFieldEntriesByColumnKey, request.SourceActivity.Fields, selectedColumnsByKey, exportedArtifactsByName, request.Options.UnitSystem);
        foreach (FitSession session in request.SourceActivity.Sessions)
        {
            AddNodeFieldEntries(uniqueFieldEntriesByColumnKey, session.Fields, selectedColumnsByKey, exportedArtifactsByName, request.Options.UnitSystem);
            foreach (FitLap lap in session.Laps)
            {
                AddNodeFieldEntries(uniqueFieldEntriesByColumnKey, lap.Fields, selectedColumnsByKey, exportedArtifactsByName, request.Options.UnitSystem);
            }

            foreach (FitRecord record in session.Records)
            {
                AddNodeFieldEntries(uniqueFieldEntriesByColumnKey, record.Fields, selectedColumnsByKey, exportedArtifactsByName, request.Options.UnitSystem);
            }
        }

        foreach (AncillaryMessageFamily ancillaryFamily in ancillaryFamilies)
        {
            string artifactFileName = BuildAncillaryArtifactFileName(ancillaryFamily, request.SourceFileNameWithoutExtension);
            bool isExported = exportedArtifactsByName.ContainsKey(artifactFileName);

            foreach (FitFieldSnapshot field in ancillaryFamily.Messages.SelectMany(static message => message.Fields))
            {
                if (uniqueFieldEntriesByColumnKey.ContainsKey(field.ExportColumnKey))
                {
                    continue;
                }

                uniqueFieldEntriesByColumnKey.Add(
                    field.ExportColumnKey,
                    new CsvExportFieldDictionaryEntry
                    {
                        ExportName = field.OriginalName,
                        NodeType = FitNodeType.Ancillary.ToString(),
                        SourceMessageFamily = field.MessageName,
                        SourceMessageNumber = field.Key.MessageNumber,
                        SourceFieldName = field.OriginalName,
                        Classification = GetClassification(field.Kind),
                        Unit = GetNormalizedUnit(field, request.Options.UnitSystem) ?? field.Units,
                        Alias = GetAlias(field.OriginalName),
                        DerivationFormula = null,
                        IsExported = isExported,
                        ArtifactName = isExported ? artifactFileName : null,
                        Notes = BuildFieldNotes(field.OriginalName, field.Kind, isAncillary: true),
                    });
            }
        }

        ImmutableArray<CsvExportFieldDictionaryEntry>.Builder builder = ImmutableArray.CreateBuilder<CsvExportFieldDictionaryEntry>(
            uniqueFieldEntriesByColumnKey.Count + 12);
        builder.AddRange(uniqueFieldEntriesByColumnKey.Values);
        AddDerivedSessionFieldEntries(builder, request, exportedArtifactsByName);
        AddAuditOnlyReferenceEntries(builder);

        return builder
            .OrderBy(static entry => entry.NodeType, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static entry => entry.SourceMessageNumber)
            .ThenBy(static entry => entry.ExportName, StringComparer.OrdinalIgnoreCase)
            .ToImmutableArray();
    }

    private static void AddNodeFieldEntries(
        IDictionary<FitExportColumnKey, CsvExportFieldDictionaryEntry> uniqueFieldEntriesByColumnKey,
        ImmutableArray<FitField> fields,
        IReadOnlyDictionary<FitExportColumnKey, CsvExportColumnSelectionMetadata> selectedColumnsByKey,
        FrozenDictionary<string, ExportedArtifact> exportedArtifactsByName,
        FitExportUnitSystem unitSystem)
    {
        foreach (FitField field in fields)
        {
            if (uniqueFieldEntriesByColumnKey.ContainsKey(field.Original.ExportColumnKey))
            {
                continue;
            }

            bool isSelected = selectedColumnsByKey.TryGetValue(field.Original.ExportColumnKey, out CsvExportColumnSelectionMetadata? selectionMetadata);
            string? artifactName = isSelected && exportedArtifactsByName.ContainsKey(selectionMetadata!.ArtifactName)
                ? selectionMetadata.ArtifactName
                : null;

            uniqueFieldEntriesByColumnKey.Add(
                field.Original.ExportColumnKey,
                new CsvExportFieldDictionaryEntry
                {
                    ExportName = isSelected ? selectionMetadata!.ColumnName : field.State.ColumnName,
                    NodeType = field.Original.Key.NodeType.ToString(),
                    SourceMessageFamily = field.Original.MessageName,
                    SourceMessageNumber = field.Original.Key.MessageNumber,
                    SourceFieldName = field.Original.OriginalName,
                    Classification = GetClassification(field.Original.Kind),
                    Unit = GetNormalizedUnit(field, unitSystem) is string normalizedUnit
                        ? normalizedUnit
                        : field.Original.Units,
                    Alias = GetAlias(field.Original.OriginalName),
                    DerivationFormula = null,
                    IsExported = isSelected && artifactName is not null,
                    ArtifactName = artifactName,
                    Notes = BuildFieldNotes(field.Original.OriginalName, field.Original.Kind, isAncillary: false),
                });
        }
    }

    private static void AddDerivedSessionFieldEntries(
        ImmutableArray<CsvExportFieldDictionaryEntry>.Builder builder,
        CsvExportRequest request,
        FrozenDictionary<string, ExportedArtifact> exportedArtifactsByName)
    {
        string? sessionArtifactName = request.NodeRequests
            .Where(static nodeRequest => nodeRequest.NodeType == FitNodeType.Session)
            .Select(static nodeRequest => Path.GetFileName(nodeRequest.DestinationFilePath))
            .FirstOrDefault(exportedArtifactsByName.ContainsKey);

        if (sessionArtifactName is null)
        {
            return;
        }

        AddDerivedSessionFieldEntry(
            builder,
            request,
            sessionArtifactName,
            DerivedSessionFieldKind.ActiveCalories,
            ActiveCaloriesExportName,
            "session",
            ActiveCaloriesAlias,
            ActiveCaloriesFormula,
            unit: "kcal");
        AddDerivedSessionFieldEntry(
            builder,
            request,
            sessionArtifactName,
            DerivedSessionFieldKind.MovingTime,
            MovingTimeExportName,
            "session",
            MovingTimeAlias,
            "Direct total_moving_time when present; otherwise sum record intervals where speed exceeds 0.1 m/s or distance increases.",
            unit: "s");
        AddDerivedSessionFieldEntry(
            builder,
            request,
            sessionArtifactName,
            DerivedSessionFieldKind.AverageMovingSpeed,
            AvgMovingSpeedExportName,
            "session",
            AvgMovingSpeedAlias,
            AvgMovingSpeedFormula,
            unit: request.Options.UnitSystem == FitExportUnitSystem.Metric ? "km/h" : "mph");
        AddDerivedSessionFieldEntry(
            builder,
            request,
            sessionArtifactName,
            DerivedSessionFieldKind.MaxAveragePowerTwentyMinutes,
            MaxAveragePowerTwentyMinutesExportName,
            "record",
            MaxAveragePowerTwentyMinutesAlias,
            MaxAveragePowerTwentyMinutesFormula,
            unit: "watts");
    }

    private static void AddDerivedSessionFieldEntry(
        ImmutableArray<CsvExportFieldDictionaryEntry>.Builder builder,
        CsvExportRequest request,
        string sessionArtifactName,
        DerivedSessionFieldKind kind,
        string exportName,
        string sourceMessageFamily,
        string alias,
        string derivationFormula,
        string unit)
    {
        bool isProjected = request.SourceActivity.Sessions.Any(session => TryGetDerivedSessionValue(session, kind, request.Options, out _));
        if (!isProjected)
        {
            return;
        }

        FitExportFieldClassification classification = GetDerivedClassification(request.SourceActivity, kind);
        builder.Add(
            new CsvExportFieldDictionaryEntry
            {
                ExportName = exportName,
                NodeType = FitNodeType.Session.ToString(),
                SourceMessageFamily = sourceMessageFamily,
                SourceMessageNumber = sourceMessageFamily.Equals("record", StringComparison.OrdinalIgnoreCase) ? (ushort)20 : (ushort)18,
                SourceFieldName = GetDerivedSourceFieldName(kind),
                Classification = classification,
                Unit = unit,
                Alias = alias,
                DerivationFormula = classification == FitExportFieldClassification.DirectStandardFit ? null : derivationFormula,
                IsExported = true,
                ArtifactName = sessionArtifactName,
                Notes = kind == DerivedSessionFieldKind.MovingTime ? MovingSpeedDerivationNotes : "Derived summary column added for structured export completeness.",
            });
    }

    private static string? GetDerivedSourceFieldName(DerivedSessionFieldKind kind) => kind switch
    {
        DerivedSessionFieldKind.ActiveCalories => "total_calories, metabolic_calories",
        DerivedSessionFieldKind.MovingTime => "total_moving_time or record speed/distance stream",
        DerivedSessionFieldKind.AverageMovingSpeed => "total_distance, moving_time",
        DerivedSessionFieldKind.MaxAveragePowerTwentyMinutes => "record power",
        _ => null
    };

    private static FitExportFieldClassification GetDerivedClassification(FitActivity activity, DerivedSessionFieldKind kind) => kind switch
    {
        DerivedSessionFieldKind.MovingTime when activity.Sessions.Any(session => HasNamedField(session.Fields, "total_moving_time") || HasNamedField(session.Fields, "moving_time"))
            => FitExportFieldClassification.DirectStandardFit,
        DerivedSessionFieldKind.MovingTime => FitExportFieldClassification.DerivedFromFit,
        DerivedSessionFieldKind.ActiveCalories => FitExportFieldClassification.DerivedFromFit,
        DerivedSessionFieldKind.AverageMovingSpeed => FitExportFieldClassification.DerivedFromFit,
        DerivedSessionFieldKind.MaxAveragePowerTwentyMinutes => FitExportFieldClassification.DerivedFromFit,
        _ => FitExportFieldClassification.Unavailable
    };

    private static bool HasNamedField(ImmutableArray<FitField> fields, string fieldName)
        => fields.Any(field => field.Original.OriginalName.Equals(fieldName, StringComparison.OrdinalIgnoreCase));

    private static void AddAuditOnlyReferenceEntries(ImmutableArray<CsvExportFieldDictionaryEntry>.Builder builder)
    {
        builder.Add(
            new CsvExportFieldDictionaryEntry
            {
                ExportName = "stamina",
                NodeType = FitNodeType.Session.ToString(),
                SourceMessageFamily = "session",
                SourceMessageNumber = 18,
                SourceFieldName = null,
                Classification = FitExportFieldClassification.GarminConnectOnlyOrUnconfirmed,
                Unit = "%",
                Alias = "Stamina",
                DerivationFormula = null,
                IsExported = false,
                ArtifactName = null,
                Notes = "Garmin FIT SDK 21.195.0 does not expose a named stamina field in Activity, Session, or Record messages. Raw unknown session fields may still carry related device-specific data.",
            });
        builder.Add(
            new CsvExportFieldDictionaryEntry
            {
                ExportName = "beginning_potential",
                NodeType = FitNodeType.Session.ToString(),
                SourceMessageFamily = "session",
                SourceMessageNumber = 18,
                SourceFieldName = null,
                Classification = FitExportFieldClassification.GarminConnectOnlyOrUnconfirmed,
                Unit = "%",
                Alias = "Beginning Potential",
                DerivationFormula = null,
                IsExported = false,
                ArtifactName = null,
                Notes = "No named FIT field is available in Garmin FIT SDK 21.195.0. Preserve raw unknown session fields when present and compare cautiously against Garmin Connect.",
            });
        builder.Add(
            new CsvExportFieldDictionaryEntry
            {
                ExportName = "ending_potential",
                NodeType = FitNodeType.Session.ToString(),
                SourceMessageFamily = "session",
                SourceMessageNumber = 18,
                SourceFieldName = null,
                Classification = FitExportFieldClassification.GarminConnectOnlyOrUnconfirmed,
                Unit = "%",
                Alias = "Ending Potential",
                DerivationFormula = null,
                IsExported = false,
                ArtifactName = null,
                Notes = "No named FIT field is available in Garmin FIT SDK 21.195.0. Preserve raw unknown session fields when present and compare cautiously against Garmin Connect.",
            });
        builder.Add(
            new CsvExportFieldDictionaryEntry
            {
                ExportName = "min_stamina",
                NodeType = FitNodeType.Session.ToString(),
                SourceMessageFamily = "session",
                SourceMessageNumber = 18,
                SourceFieldName = null,
                Classification = FitExportFieldClassification.GarminConnectOnlyOrUnconfirmed,
                Unit = "%",
                Alias = "Min Stamina",
                DerivationFormula = null,
                IsExported = false,
                ArtifactName = null,
                Notes = "No named FIT field is available in Garmin FIT SDK 21.195.0. Preserve raw unknown session fields when present and compare cautiously against Garmin Connect.",
            });
        builder.Add(
            new CsvExportFieldDictionaryEntry
            {
                ExportName = "est_sweat_loss",
                NodeType = FitNodeType.Session.ToString(),
                SourceMessageFamily = "session",
                SourceMessageNumber = 18,
                SourceFieldName = null,
                Classification = FitExportFieldClassification.GarminConnectOnlyOrUnconfirmed,
                Unit = "ml",
                Alias = "Est. Sweat Loss",
                DerivationFormula = null,
                IsExported = false,
                ArtifactName = null,
                Notes = "No named FIT field is available in Garmin FIT SDK 21.195.0 for this structured export path.",
            });
        builder.Add(
            new CsvExportFieldDictionaryEntry
            {
                ExportName = "fluid_consumed",
                NodeType = FitNodeType.Session.ToString(),
                SourceMessageFamily = "session",
                SourceMessageNumber = 18,
                SourceFieldName = null,
                Classification = FitExportFieldClassification.GarminConnectOnlyOrUnconfirmed,
                Unit = "ml",
                Alias = "Fluid Consumed",
                DerivationFormula = null,
                IsExported = false,
                ArtifactName = null,
                Notes = "No named FIT field is available in Garmin FIT SDK 21.195.0 for this structured export path.",
            });
        builder.Add(
            new CsvExportFieldDictionaryEntry
            {
                ExportName = "fluid_net",
                NodeType = FitNodeType.Session.ToString(),
                SourceMessageFamily = "session",
                SourceMessageNumber = 18,
                SourceFieldName = null,
                Classification = FitExportFieldClassification.GarminConnectOnlyOrUnconfirmed,
                Unit = "ml",
                Alias = "Fluid Net",
                DerivationFormula = null,
                IsExported = false,
                ArtifactName = null,
                Notes = "No named FIT field is available in Garmin FIT SDK 21.195.0 for this structured export path.",
            });
        builder.Add(
            new CsvExportFieldDictionaryEntry
            {
                ExportName = "intensity_minutes",
                NodeType = FitNodeType.Session.ToString(),
                SourceMessageFamily = "session",
                SourceMessageNumber = 18,
                SourceFieldName = null,
                Classification = FitExportFieldClassification.GarminConnectOnlyOrUnconfirmed,
                Unit = "min",
                Alias = "Intensity Minutes",
                DerivationFormula = null,
                IsExported = false,
                ArtifactName = null,
                Notes = "No named FIT field is available in Garmin FIT SDK 21.195.0 for this structured export path.",
            });
    }

    private static FitExportFieldClassification GetClassification(FitFieldKind kind) => kind switch
    {
        FitFieldKind.Standard => FitExportFieldClassification.DirectStandardFit,
        FitFieldKind.Developer => FitExportFieldClassification.DirectDeveloperField,
        FitFieldKind.Unknown => FitExportFieldClassification.DirectStandardFit,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unsupported FIT field kind."),
    };

    private static bool ContainsDeveloperFields(FitActivity activity)
        => activity.Fields.Any(static field => field.Original.Kind == FitFieldKind.Developer)
            || activity.Sessions.Any(session =>
                session.Fields.Any(static field => field.Original.Kind == FitFieldKind.Developer)
                || session.Laps.Any(lap => lap.Fields.Any(static field => field.Original.Kind == FitFieldKind.Developer))
                || session.Records.Any(record => record.Fields.Any(static field => field.Original.Kind == FitFieldKind.Developer)))
            || activity.AncillaryData.Messages.SelectMany(static message => message.Fields).Any(static field => field.Kind == FitFieldKind.Developer);

    private static bool ContainsUnknownOrVendorFields(FitActivity activity)
        => activity.Fields.Any(static field => field.Original.Kind == FitFieldKind.Unknown)
            || activity.Sessions.Any(session =>
                session.Fields.Any(static field => field.Original.Kind == FitFieldKind.Unknown)
                || session.Laps.Any(lap => lap.Fields.Any(static field => field.Original.Kind == FitFieldKind.Unknown))
                || session.Records.Any(record => record.Fields.Any(static field => field.Original.Kind == FitFieldKind.Unknown)))
            || activity.AncillaryData.Messages.Any(message =>
                string.Equals(message.Original.MessageName, "unknown", StringComparison.OrdinalIgnoreCase)
                || message.Fields.Any(static field => field.Kind == FitFieldKind.Unknown));

    private static string? GetAlias(string originalName)
        => s_fieldAliasesByOriginalName.TryGetValue(originalName, out string? alias) ? alias : null;

    private static string? BuildFieldNotes(string originalName, FitFieldKind kind, bool isAncillary)
    {
        List<string> notes = [];
        if (kind == FitFieldKind.Unknown)
        {
            notes.Add("Garmin FIT SDK 21.195.0 does not expose a semantic profile name for this field, so the raw source value is preserved under an unknown_* export name.");
        }

        if (isAncillary)
        {
            notes.Add("Exported from a restored ancillary FIT message family outside the Activity/Session/Lap/Record tree.");
        }

        if (s_fieldNotesByOriginalName.TryGetValue(originalName, out string? note))
        {
            notes.Add(note);
        }

        return notes.Count == 0 ? null : string.Join(" ", notes);
    }

    private static IEnumerable<FitNode> EnumerateNodes(FitActivity sourceActivity, FitNodeType nodeType) => nodeType switch
    {
        FitNodeType.Activity => [sourceActivity],
        FitNodeType.Session => sourceActivity.Sessions,
        FitNodeType.Lap => sourceActivity.Sessions.SelectMany(static session => session.Laps),
        FitNodeType.Record => sourceActivity.Sessions.SelectMany(static session => session.Records),
        FitNodeType.Ancillary => throw new NotSupportedException("Ancillary messages are exported through the dedicated ancillary CSV writer."),
        _ => throw new ArgumentOutOfRangeException(nameof(nodeType), nodeType, "Unsupported FIT node type.")
    };

    private static ImmutableArray<ProjectedColumn> BuildProjectedColumns(
        ImmutableArray<CsvColumnSelection> orderedColumns,
        FrozenDictionary<FitExportColumnKey, FitField> referenceFieldLookup,
        FitExportOptions exportOptions)
    {
        ImmutableArray<ProjectedColumn>.Builder projectedColumns = ImmutableArray.CreateBuilder<ProjectedColumn>(orderedColumns.Length * 2);
        foreach (CsvColumnSelection orderedColumn in orderedColumns)
        {
            if (!referenceFieldLookup.TryGetValue(orderedColumn.ColumnKey, out FitField? referenceField))
            {
                throw new InvalidOperationException($"Unable to resolve export metadata for selected column '{orderedColumn.ColumnName}'.");
            }

            projectedColumns.Add(new ProjectedColumn(
                orderedColumn,
                BuildHeader(orderedColumn.ColumnName, referenceField, exportOptions, isLocalTimeDuplicate: false),
                IsLocalTimeDuplicate: false));

            // Keep the local-time duplicate immediately next to the canonical timestamp column so downstream
            // readers can pair them without scanning the whole header set.
            if (exportOptions.IncludeLocalTimeColumns && IsTimestampField(referenceField))
            {
                projectedColumns.Add(new ProjectedColumn(
                    orderedColumn,
                    BuildHeader(orderedColumn.ColumnName, referenceField, exportOptions, isLocalTimeDuplicate: true),
                    IsLocalTimeDuplicate: true));
            }
        }

        return projectedColumns.ToImmutable();
    }

    private static string BuildHeader(
        string baseHeader,
        FitField referenceField,
        FitExportOptions exportOptions,
        bool isLocalTimeDuplicate)
        => BuildHeader(baseHeader, referenceField.Original, exportOptions, isLocalTimeDuplicate);

    private static string BuildHeader(
        string baseHeader,
        FitFieldSnapshot referenceField,
        FitExportOptions exportOptions,
        bool isLocalTimeDuplicate)
    {
        if (IsTimestampField(referenceField))
        {
            string timestampQualifier = isLocalTimeDuplicate ? "Local" : "UTC";
            return exportOptions.IncludeUnitSuffixInHeaders
                ? $"{baseHeader} [{timestampQualifier}]"
                : $"{baseHeader} {timestampQualifier}";
        }

        string? normalizedUnit = GetNormalizedUnit(referenceField, exportOptions.UnitSystem);
        if (!exportOptions.IncludeUnitSuffixInHeaders || string.IsNullOrWhiteSpace(normalizedUnit))
        {
            return baseHeader;
        }

        return $"{baseHeader} [{normalizedUnit}]";
    }

    private static string FormatFieldValues(FitField field, ProjectedColumn projectedColumn, FitExportOptions exportOptions)
    {
        ImmutableArray<object?> values = GetExportValues(field);
        if (values.IsDefaultOrEmpty)
        {
            return RenderMissingValue(exportOptions);
        }

        if (values.Length == 1)
        {
            return FormatNormalizedValue(field, values[0], projectedColumn.IsLocalTimeDuplicate, exportOptions);
        }

        ImmutableArray<string> formattedValues = values
            .Select(value => FormatNormalizedValue(field, value, projectedColumn.IsLocalTimeDuplicate, exportOptions))
            .ToImmutableArray();

        return formattedValues.All(string.IsNullOrEmpty)
            ? RenderMissingValue(exportOptions)
            : string.Join(ArrayValueSeparator, formattedValues);
    }

    private static ImmutableArray<object?> GetExportValues(FitField field)
    {
        if (field.State.HasEditedDecodedValues)
        {
            return field.State.EditedDecodedValues;
        }

        return field.Original.OriginalValues
            .Select(originalValue => IsInvalidOriginalValue(field.Original.BaseTypeName, originalValue.RawValue, originalValue.DecodedValue)
                ? null
                : originalValue.DecodedValue)
            .ToImmutableArray();
    }

    private static bool IsInvalidOriginalValue(string baseTypeName, object? rawValue, object? decodedValue)
    {
        if (decodedValue is float singleValue && float.IsNaN(singleValue))
        {
            return true;
        }

        if (decodedValue is double doubleValue && double.IsNaN(doubleValue))
        {
            return true;
        }

        if (string.Equals(baseTypeName, "string", StringComparison.OrdinalIgnoreCase))
        {
            return IsInvalidStringValue(rawValue);
        }

        return s_numericInvalidValuesByBaseTypeName.TryGetValue(baseTypeName, out decimal invalidValue)
            && TryConvertToDecimal(rawValue, out decimal numericRawValue)
            && numericRawValue == invalidValue;
    }

    private static bool IsInvalidStringValue(object? rawValue) => rawValue switch
    {
        null => true,
        byte byteValue => byteValue == 0,
        sbyte signedByteValue => signedByteValue == 0,
        short shortValue => shortValue == 0,
        ushort unsignedShortValue => unsignedShortValue == 0,
        int integerValue => integerValue == 0,
        uint unsignedIntegerValue => unsignedIntegerValue == 0,
        long longValue => longValue == 0,
        ulong unsignedLongValue => unsignedLongValue == 0,
        ImmutableArray<byte> immutableByteArrayValue => immutableByteArrayValue.All(static byteValue => byteValue == 0),
        byte[] byteArrayValue => byteArrayValue.All(static byteValue => byteValue == 0),
        _ => false
    };

    private static bool TryConvertToDecimal(object? value, out decimal numericValue)
    {
        try
        {
            switch (value)
            {
                case decimal decimalValue:
                    numericValue = decimalValue;
                    return true;
                case IConvertible convertibleValue:
                    numericValue = convertibleValue.ToDecimal(CultureInfo.InvariantCulture);
                    return true;
                default:
                    numericValue = default;
                    return false;
            }
        }
        catch (FormatException)
        {
            numericValue = default;
            return false;
        }
        catch (InvalidCastException)
        {
            numericValue = default;
            return false;
        }
        catch (OverflowException)
        {
            numericValue = default;
            return false;
        }
    }

    private static string FormatNormalizedValue(
        FitField field,
        object? value,
        bool isLocalTimeDuplicate,
        FitExportOptions exportOptions)
    {
        object? normalizedValue = NormalizeValue(field, value, isLocalTimeDuplicate, exportOptions);
        return normalizedValue is null
            ? RenderMissingValue(exportOptions)
            : FormatSingleValue(normalizedValue);
    }

    private static object? NormalizeValue(
        FitField field,
        object? value,
        bool isLocalTimeDuplicate,
        FitExportOptions exportOptions)
        => NormalizeValue(field.Original, value, isLocalTimeDuplicate, exportOptions);

    private static object? NormalizeValue(
        FitFieldSnapshot field,
        object? value,
        bool isLocalTimeDuplicate,
        FitExportOptions exportOptions)
    {
        if (value is null)
        {
            return null;
        }

        if (value is float singleValue && float.IsNaN(singleValue))
        {
            return null;
        }

        if (value is double doubleValue && double.IsNaN(doubleValue))
        {
            return null;
        }

        if (IsTimestampField(field))
        {
            return NormalizeTimestampValue(value, isLocalTimeDuplicate, exportOptions.LocalTimeZone);
        }

        if (TryNormalizeDurationToSeconds(value, field.Units, out double normalizedDurationValue))
        {
            return normalizedDurationValue;
        }

        if (TryNormalizeDistanceValue(value, field.Units, exportOptions.UnitSystem, out double normalizedDistanceValue))
        {
            return normalizedDistanceValue;
        }

        if (TryNormalizeSpeedValue(value, field.Units, exportOptions.UnitSystem, out double normalizedSpeedValue))
        {
            return normalizedSpeedValue;
        }

        return value;
    }

    private static object? NormalizeTimestampValue(object value, bool isLocalTimeDuplicate, TimeZoneInfo localTimeZone)
    {
        if (!TryConvertToDateTimeOffset(value, out DateTimeOffset timestampValue))
        {
            return value;
        }

        DateTimeOffset utcTimestamp = timestampValue.ToUniversalTime();
        return isLocalTimeDuplicate
            ? TimeZoneInfo.ConvertTime(utcTimestamp, localTimeZone)
            : utcTimestamp;
    }

    private static bool TryConvertToDateTimeOffset(object value, out DateTimeOffset timestampValue)
    {
        switch (value)
        {
            case DateTimeOffset dateTimeOffsetValue:
                timestampValue = dateTimeOffsetValue;
                return true;

            case DateTime dateTimeValue:
                DateTime normalizedDateTime = dateTimeValue.Kind == DateTimeKind.Unspecified
                    ? DateTime.SpecifyKind(dateTimeValue, DateTimeKind.Utc)
                    : dateTimeValue;
                timestampValue = new DateTimeOffset(normalizedDateTime);
                return true;

            default:
                timestampValue = default;
                return false;
        }
    }

    private static bool TryNormalizeDurationToSeconds(object? value, string? sourceUnit, out double normalizedValue)
    {
        string normalizedUnit = NormalizeUnit(sourceUnit);
        switch (normalizedUnit)
        {
            case "s":
                if (!TryConvertToDouble(value, out double numericValueInSeconds))
                {
                    normalizedValue = default;
                    return false;
                }

                normalizedValue = numericValueInSeconds;
                return true;

            case "ms":
                if (!TryConvertToDouble(value, out double numericValueInMilliseconds))
                {
                    normalizedValue = default;
                    return false;
                }

                normalizedValue = numericValueInMilliseconds / 1000d;
                return true;

            case "min":
                if (!TryConvertToDouble(value, out double numericValueInMinutes))
                {
                    normalizedValue = default;
                    return false;
                }

                normalizedValue = numericValueInMinutes * 60d;
                return true;

            case "h":
                if (!TryConvertToDouble(value, out double numericValueInHours))
                {
                    normalizedValue = default;
                    return false;
                }

                normalizedValue = numericValueInHours * 3600d;
                return true;

            default:
                normalizedValue = default;
                return false;
        }
    }

    private static bool TryNormalizeDistanceToMeters(object? value, string? sourceUnit, out double normalizedValue)
    {
        string normalizedUnit = NormalizeUnit(sourceUnit);
        if (!IsDistanceUnit(normalizedUnit) || !TryConvertToDouble(value, out double numericValue))
        {
            normalizedValue = default;
            return false;
        }

        normalizedValue = normalizedUnit switch
        {
            "m" => numericValue,
            "km" => numericValue / KilometersPerMeter,
            "ft" => numericValue / FeetPerMeter,
            "mi" => numericValue / MilesPerMeter,
            _ => double.NaN
        };

        return !double.IsNaN(normalizedValue);
    }

    private static bool TryNormalizeDistanceValue(
        object? value,
        string? sourceUnit,
        FitExportUnitSystem unitSystem,
        out double normalizedValue)
    {
        if (!TryNormalizeDistanceToMeters(value, sourceUnit, out double distanceInMeters))
        {
            normalizedValue = default;
            return false;
        }

        normalizedValue = unitSystem == FitExportUnitSystem.Metric
            ? distanceInMeters * KilometersPerMeter
            : distanceInMeters * MilesPerMeter;
        return true;
    }

    private static bool TryNormalizeSpeedToMetersPerSecond(object? value, string? sourceUnit, out double normalizedValue)
    {
        string normalizedUnit = NormalizeUnit(sourceUnit);
        if (!IsSpeedUnit(normalizedUnit) || !TryConvertToDouble(value, out double numericValue))
        {
            normalizedValue = default;
            return false;
        }

        normalizedValue = normalizedUnit switch
        {
            "m/s" => numericValue,
            "km/h" => numericValue / KilometersPerHourPerMeterPerSecond,
            "mph" => numericValue / MilesPerHourPerMeterPerSecond,
            _ => double.NaN
        };

        return !double.IsNaN(normalizedValue);
    }

    private static bool TryNormalizeSpeedValue(
        object? value,
        string? sourceUnit,
        FitExportUnitSystem unitSystem,
        out double normalizedValue)
    {
        if (!TryNormalizeSpeedToMetersPerSecond(value, sourceUnit, out double speedInMetersPerSecond))
        {
            normalizedValue = default;
            return false;
        }

        normalizedValue = unitSystem == FitExportUnitSystem.Metric
            ? speedInMetersPerSecond * KilometersPerHourPerMeterPerSecond
            : speedInMetersPerSecond * MilesPerHourPerMeterPerSecond;
        return true;
    }

    private static bool TryConvertToDouble(object? value, out double numericValue)
    {
        // Structured CSV normalization only applies to true numeric payloads.
        // Edited/export values can intentionally be text labels, and those should round-trip unchanged.
        switch (value)
        {
            case byte byteValue:
                numericValue = byteValue;
                return true;
            case sbyte signedByteValue:
                numericValue = signedByteValue;
                return true;
            case short shortValue:
                numericValue = shortValue;
                return true;
            case ushort unsignedShortValue:
                numericValue = unsignedShortValue;
                return true;
            case int integerValue:
                numericValue = integerValue;
                return true;
            case uint unsignedIntegerValue:
                numericValue = unsignedIntegerValue;
                return true;
            case long longValue:
                numericValue = longValue;
                return true;
            case ulong unsignedLongValue:
                numericValue = unsignedLongValue;
                return true;
            case float singleValue:
                numericValue = singleValue;
                return true;
            case double doubleValue:
                numericValue = doubleValue;
                return true;
            case decimal decimalValue:
                numericValue = Convert.ToDouble(decimalValue, CultureInfo.InvariantCulture);
                return true;
            default:
                numericValue = default;
                return false;
        }
    }

    private static bool IsTimestampField(FitField field) => IsTimestampField(field.Original);

    private static bool IsTimestampField(FitFieldSnapshot field)
    {
        if (field.ProfileTypeName.Equals("DateTime", StringComparison.OrdinalIgnoreCase)
            || field.ProfileTypeName.Equals("LocalDateTime", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return field.OriginalValues.Any(static fieldValue => fieldValue.DecodedValue is DateTimeOffset or DateTime);
    }

    private static string? GetNormalizedUnit(FitField field, FitExportUnitSystem unitSystem)
        => GetNormalizedUnit(field.Original, unitSystem);

    private static string? GetNormalizedUnit(FitFieldSnapshot field, FitExportUnitSystem unitSystem)
    {
        if (IsTimestampField(field))
        {
            return null;
        }

        if (TryNormalizeDurationUnit(field.Units, out string durationUnit))
        {
            return durationUnit;
        }

        if (TryNormalizeDistanceUnit(field.Units, unitSystem, out string distanceUnit))
        {
            return distanceUnit;
        }

        if (TryNormalizeSpeedUnit(field.Units, unitSystem, out string speedUnit))
        {
            return speedUnit;
        }

        return string.IsNullOrWhiteSpace(field.Units) ? null : field.Units;
    }

    private static bool TryNormalizeDurationUnit(string? sourceUnit, out string normalizedUnit)
    {
        switch (NormalizeUnit(sourceUnit))
        {
            case "s":
            case "ms":
            case "min":
            case "h":
                normalizedUnit = "s";
                return true;
            default:
                normalizedUnit = string.Empty;
                return false;
        }
    }

    private static bool TryNormalizeDistanceUnit(string? sourceUnit, FitExportUnitSystem unitSystem, out string normalizedUnit)
    {
        switch (NormalizeUnit(sourceUnit))
        {
            case "m":
            case "km":
            case "ft":
            case "mi":
                normalizedUnit = unitSystem == FitExportUnitSystem.Metric ? "km" : "mi";
                return true;
            default:
                normalizedUnit = string.Empty;
                return false;
        }
    }

    private static bool TryNormalizeSpeedUnit(string? sourceUnit, FitExportUnitSystem unitSystem, out string normalizedUnit)
    {
        switch (NormalizeUnit(sourceUnit))
        {
            case "m/s":
            case "km/h":
            case "mph":
                normalizedUnit = unitSystem == FitExportUnitSystem.Metric ? "km/h" : "mph";
                return true;
            default:
                normalizedUnit = string.Empty;
                return false;
        }
    }

    private static string NormalizeUnit(string? unit)
        => string.IsNullOrWhiteSpace(unit)
            ? string.Empty
            : unit.Trim().ToLowerInvariant();

    private static bool IsDistanceUnit(string normalizedUnit)
        => normalizedUnit is "m" or "km" or "ft" or "mi";

    private static bool IsSpeedUnit(string normalizedUnit)
        => normalizedUnit is "m/s" or "km/h" or "mph";

    private static string RenderMissingValue(FitExportOptions exportOptions)
        => exportOptions.MissingValueStyle == FitExportMissingValueStyle.Literal
            ? exportOptions.MissingValueLiteral
            : string.Empty;

    private static void EnsureStructuredCsvTarget(FitExportTarget target)
    {
        if (target != FitExportTarget.StructuredCsv)
        {
            throw new NotSupportedException(
                $"The current CSV exporter only supports '{FitExportTarget.StructuredCsv}'. Requested target: '{target}'.");
        }
    }

    private static string FormatSingleValue(object? value) => value switch
    {
        null => string.Empty,
        DateTimeOffset dateTimeOffsetValue => dateTimeOffsetValue.ToString("O", CultureInfo.InvariantCulture),
        DateTime dateTimeValue => dateTimeValue.ToString("O", CultureInfo.InvariantCulture),
        IFormattable formattableValue => formattableValue.ToString(format: null, CultureInfo.InvariantCulture) ?? string.Empty,
        _ => value.ToString() ?? string.Empty
    };

    private static async Task WriteLineAsync(
        StreamWriter writer,
        IEnumerable<string> values,
        char delimiter,
        CancellationToken cancellationToken)
    {
        string line = string.Join(delimiter, values.Select(value => EscapeValue(value, delimiter)));
        await writer.WriteLineAsync(line.AsMemory(), cancellationToken).ConfigureAwait(false);
    }

    private static string EscapeValue(string value, char delimiter)
    {
        bool requiresEscaping = value.Contains(delimiter)
            || value.Contains('"')
            || value.Contains('\r')
            || value.Contains('\n');

        if (!requiresEscaping)
        {
            return value;
        }

        return $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
    }

    private static string BuildAncillaryArtifactFileName(AncillaryMessageFamily ancillaryFamily, string sourceFileNameWithoutExtension)
        => $"{sourceFileNameWithoutExtension}_{SanitizeFileNameSegment(ancillaryFamily.Key.MessageName)}_{ancillaryFamily.Key.MessageNumber}.csv";

    private static string SanitizeFileNameSegment(string value)
    {
        StringBuilder builder = new(value.Length);
        foreach (char character in value)
        {
            _ = builder.Append(Path.GetInvalidFileNameChars().Contains(character) ? '_' : character);
        }

        return builder.ToString();
    }

    private static JsonSerializerOptions CreateManifestSerializerOptions()
    {
        JsonSerializerOptions serializerOptions = new()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
        };
        serializerOptions.Converters.Add(new JsonStringEnumConverter());
        return serializerOptions;
    }

    private sealed record ProjectedColumn(
        CsvColumnSelection Selection,
        string Header,
        bool IsLocalTimeDuplicate);

    private sealed record DerivedSessionColumn(
        DerivedSessionFieldKind Kind,
        string Header);

    private sealed record AncillaryFieldColumn(
        FitExportColumnKey ColumnKey,
        string Header);

    private sealed record AncillaryMessageFamily(
        AncillaryFamilyKey Key,
        ImmutableArray<FitAncillaryMessage> Messages);

    private sealed record CsvExportColumnSelectionMetadata(
        string ColumnName,
        string ArtifactName);

    private readonly record struct AncillaryFamilyKey(string MessageName, ushort MessageNumber);

    private enum DerivedSessionFieldKind
    {
        ActiveCalories = 0,
        MovingTime = 1,
        AverageMovingSpeed = 2,
        MaxAveragePowerTwentyMinutes = 3
    }
}
