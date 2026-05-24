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
    private const string ArrayValueOrdering = "Source order";
    private const string ArrayValueShape = "Array";
    private const string BeginningPotentialAlias = "Beginning Potential";
    private const string BeginningPotentialExportName = "beginning_potential";
    private const string AvgMovingSpeedAlias = "Avg Moving Speed";
    private const string AvgMovingSpeedExportName = "avg_moving_speed";
    private const string AvgMovingSpeedFormula = "total_distance / moving_time";
    private const string CanonicalTimestampSemantics = "UTC ISO-8601";
    private const string DurationSemantics = "Numeric duration columns are normalized to seconds.";
    private const string EndingPotentialAlias = "Ending Potential";
    private const string EndingPotentialExportName = "ending_potential";
    private const string EstimatedSweatLossAlias = "Est. Sweat Loss";
    private const string EstimatedSweatLossExportName = "est_sweat_loss";
    private const string FormulaDerivedRoundingNotes =
        "Formula-derived structured values are emitted with invariant-culture numeric formatting; the exporter does not apply presentation rounding.";
    private const double FeetPerMeter = 3.28083989501312;
    private const string GarminConnectAliasLocale = "en";
    private const string GarminConnectPdfAliasSource = "GarminConnectPdf";
    private const double JoulesPerKilojoule = 1000d;
    private const string CoreDirectoryName = "core";
    private const string RawLosslessDirectoryName = "raw_lossless";
    private const string MetadataDirectoryName = "metadata";
    private const string AnalyticsDirectoryName = "analytics";
    private const string RawUnmappedDirectoryName = "raw_unmapped";
    private const string LocalSourceTimestampQualifier = "Local Source";
    private const string ManifestArtifactName = "manifest";
    private const string ManifestFileNameSuffix = "_manifest.json";
    private const string ManifestSchemaVersion = "2.0.0";
    private const double KilometersPerMeter = 0.001;
    private const double KilometersPerHourPerMeterPerSecond = 3.6;
    private const string MaxAveragePowerTwentyMinutesAlias = "Max Avg Power (20 min)";
    private const string MaxAveragePowerTwentyMinutesExportName = "max_avg_power_20min";
    private const string MaxAveragePowerTwentyMinutesFormula =
        "Max rolling average of record power over a 1200-second elapsed-time window using one sample per record timestamp and zero-filled missing elapsed seconds.";
    private const string MessageTimestampMetadataHeader = "message_timestamp";
    private const string MinimumStaminaAlias = "Min Stamina";
    private const string MinimumStaminaExportName = "min_stamina";
    private const string MappedUnknownFieldRoundingNotes =
        "Mapped unknown-field values are emitted from the preserved decoded FIT value without presentation rounding.";
    private const string MappedUnknownFieldSourceEvidence =
        "Observed in the Edge 840 repository reference activity by comparing preserved session unknown fields against the matching Garmin Connect PDF and FitCSVTool export.";
    private const string MappedUnknownFieldMappingReason =
        "The preserved source field value matches a Garmin Connect reference value, but Profile.xlsx and Garmin FIT SDK 21.195.0 do not publish a semantic field name for this session field.";
    private const string MovingSpeedDerivationNotes =
        "Derived moving time uses direct total_moving_time when present; otherwise it counts up to one elapsed second per qualifying record interval where speed exceeds 0.1 m/s or distance increases.";
    private const string MovingTimeAlias = "Moving Time";
    private const string MovingTimeExportName = "moving_time";
    private const double MovingSpeedThresholdMetersPerSecond = 0.1;
    private const double MilesPerMeter = 0.000621371192237334;
    private const double MilesPerHourPerMeterPerSecond = 2.2369362920544;
    private const int PowerAverageWindowSeconds = 20 * 60;
    private const string RawUnmappedArtifactName = "raw_unmapped";
    private const string RawUnmappedFileNameSuffix = "_raw_unmapped.csv";
    private const string ScalarValueShape = "Scalar";
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

    private static readonly FrozenSet<string> s_metadataMessageFamilies =
        new[]
        {
            "file_id",
            "file_creator",
            "device_info",
            "device_settings",
            "user_profile",
            "sport",
            "training_settings",
            "timestamp_correlation",
            "zones_target",
        }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    private static readonly FrozenSet<string> s_analyticsMessageFamilies =
        new[]
        {
            "event",
            "time_in_zone",
            "split_summary",
            "hrv",
        }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    private static readonly FrozenDictionary<string, string> s_unitOverridesByFieldName =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // Profile.xlsx leaves this sibling summary field unitless while avg/max respiration use Breaths/min.
            // Keep the structured export coherent for machine consumers without changing the decoded source model.
            ["enhanced_min_respiration_rate"] = "Breaths/min",
        }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    private static readonly FrozenDictionary<string, CsvFieldAliasDefinition> s_fieldAliasDefinitionsByKey =
        new Dictionary<string, CsvFieldAliasDefinition>(StringComparer.OrdinalIgnoreCase)
        {
            [CreateAliasLookupKey("Session", "training_load_peak")] = new(
                TrainingLoadPeakAlias,
                CsvExportAliasKind.DirectFieldAlias,
                Confidence: 0.95,
                IsDirectAlias: true,
                IsDerivedAlias: false,
                "Garmin Connect rounds this session metric to the whole-number Exercise Load label shown in the PDF summary."),
            [CreateAliasLookupKey("Session", "total_cycles")] = new(
                TotalCyclesAlias,
                CsvExportAliasKind.DirectFieldAlias,
                Confidence: 0.95,
                IsDirectAlias: true,
                IsDerivedAlias: false,
                "Garmin cycling summaries commonly present total_cycles as Total Strokes."),
            [CreateAliasLookupKey("Session", "total_distance")] = new("Distance", CsvExportAliasKind.DirectFieldAlias, 0.95, true, false, null),
            [CreateAliasLookupKey("Session", "total_timer_time")] = new("Time", CsvExportAliasKind.DirectFieldAlias, 0.9, true, false, null),
            [CreateAliasLookupKey("Session", "total_elapsed_time")] = new("Elapsed Time", CsvExportAliasKind.DirectFieldAlias, 0.95, true, false, null),
            [CreateAliasLookupKey("Session", "enhanced_avg_speed")] = new("Avg Speed", CsvExportAliasKind.DirectFieldAlias, 0.95, true, false, null),
            [CreateAliasLookupKey("Session", "enhanced_max_speed")] = new("Max Speed", CsvExportAliasKind.DirectFieldAlias, 0.95, true, false, null),
            [CreateAliasLookupKey("Session", "avg_power")] = new("Avg Power", CsvExportAliasKind.DirectFieldAlias, 0.95, true, false, null),
            [CreateAliasLookupKey("Session", "max_power")] = new("Max Power", CsvExportAliasKind.DirectFieldAlias, 0.95, true, false, null),
            [CreateAliasLookupKey("Session", "normalized_power")] = new("Normalized Power (NP)", CsvExportAliasKind.DirectFieldAlias, 0.95, true, false, null),
            [CreateAliasLookupKey("Session", "intensity_factor")] = new("IF", CsvExportAliasKind.DirectFieldAlias, 0.95, true, false, null),
            [CreateAliasLookupKey("Session", "training_stress_score")] = new("TSS", CsvExportAliasKind.DirectFieldAlias, 0.95, true, false, null),
            [CreateAliasLookupKey("Session", "threshold_power")] = new("FTP Setting", CsvExportAliasKind.DirectFieldAlias, 0.95, true, false, null),
            [CreateAliasLookupKey("Session", "total_work")] = new("Work", CsvExportAliasKind.DirectFieldAlias, 0.95, true, false, "Garmin Connect presents this value in kJ."),
            [CreateAliasLookupKey("Session", "metabolic_calories")] = new("Resting Calories", CsvExportAliasKind.DirectFieldAlias, 0.95, true, false, null),
            [CreateAliasLookupKey("Session", "total_calories")] = new("Total Calories Burned", CsvExportAliasKind.DirectFieldAlias, 0.95, true, false, null),
            [CreateAliasLookupKey("Session", "avg_heart_rate")] = new("Avg HR", CsvExportAliasKind.DirectFieldAlias, 0.95, true, false, null),
            [CreateAliasLookupKey("Session", "max_heart_rate")] = new("Max HR", CsvExportAliasKind.DirectFieldAlias, 0.95, true, false, null),
            [CreateAliasLookupKey("Session", "avg_cadence")] = new("Avg Bike Cadence", CsvExportAliasKind.DirectFieldAlias, 0.95, true, false, null),
            [CreateAliasLookupKey("Session", "max_cadence")] = new("Max Bike Cadence", CsvExportAliasKind.DirectFieldAlias, 0.95, true, false, null),
            [CreateAliasLookupKey("Session", "avg_temperature")] = new("Avg Temp", CsvExportAliasKind.DirectFieldAlias, 0.95, true, false, null),
            [CreateAliasLookupKey("Session", "min_temperature")] = new("Min Temp", CsvExportAliasKind.DirectFieldAlias, 0.95, true, false, null),
            [CreateAliasLookupKey("Session", "max_temperature")] = new("Max Temp", CsvExportAliasKind.DirectFieldAlias, 0.95, true, false, null),
            [CreateAliasLookupKey("Session", "enhanced_avg_respiration_rate")] = new("Avg Respiration Rate", CsvExportAliasKind.DirectFieldAlias, 0.95, true, false, null),
            [CreateAliasLookupKey("Session", "enhanced_min_respiration_rate")] = new("Min Respiration Rate", CsvExportAliasKind.DirectFieldAlias, 0.95, true, false, null),
            [CreateAliasLookupKey("Session", "enhanced_max_respiration_rate")] = new("Max Respiration Rate", CsvExportAliasKind.DirectFieldAlias, 0.95, true, false, null),
            [CreateAliasLookupKey("Session", "total_training_effect")] = new("Aerobic", CsvExportAliasKind.DirectFieldAlias, 0.9, true, false, "Garmin Connect adds the benefit text separately."),
            [CreateAliasLookupKey("Session", "total_anaerobic_training_effect")] = new("Anaerobic", CsvExportAliasKind.DirectFieldAlias, 0.9, true, false, "Garmin Connect adds the benefit text separately."),
            [CreateAliasLookupKey("Session", "workout_feel")] = new("How did you feel?", CsvExportAliasKind.HumanFriendlyAlias, 0.7, true, false, "Garmin Connect translates the raw workout_feel byte into a user-facing label such as Strong."),
            [CreateAliasLookupKey("Session", "workout_rpe")] = new("Perceived Effort", CsvExportAliasKind.HumanFriendlyAlias, 0.75, true, false, "Garmin Connect displays the raw workout_rpe byte as a /10 score plus a descriptive label."),
            [CreateAliasLookupKey("Session", "unknown_178")] = new(EstimatedSweatLossAlias, CsvExportAliasKind.HumanFriendlyAlias, 0.55, false, false, "Observed on the Edge 840 reference activity as a session unknown field matching Garmin Connect sweat-loss output."),
            [CreateAliasLookupKey("Session", "unknown_205")] = new(BeginningPotentialAlias, CsvExportAliasKind.HumanFriendlyAlias, 0.55, false, false, "Observed on the Edge 840 reference activity as a session unknown field matching Garmin Connect beginning potential."),
            [CreateAliasLookupKey("Session", "unknown_206")] = new(EndingPotentialAlias, CsvExportAliasKind.HumanFriendlyAlias, 0.55, false, false, "Observed on the Edge 840 reference activity as a session unknown field matching Garmin Connect ending potential."),
            [CreateAliasLookupKey("Session", "unknown_207")] = new(MinimumStaminaAlias, CsvExportAliasKind.HumanFriendlyAlias, 0.55, false, false, "Observed on the Edge 840 reference activity as a session unknown field matching Garmin Connect minimum stamina."),
        }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    private static readonly FrozenDictionary<string, string> s_fieldNotesByOriginalName =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["training_load_peak"] = "Garmin Connect commonly presents this session summary as Exercise Load.",
            ["total_cycles"] = "Garmin cycling summaries commonly present this as Total Strokes.",
            ["total_work"] = "The machine export normalizes joule-based work values to kJ so the column matches common cycling summary conventions.",
            ["workout_feel"] = "Garmin FIT SDK 21.195.0 exposes workout_feel as a nullable byte. The exporter preserves the raw byte value and does not impose undocumented device-specific scoring semantics.",
            ["workout_rpe"] = "Garmin FIT SDK 21.195.0 exposes workout_rpe as a nullable byte. The exporter preserves the raw byte value and does not impose undocumented device-specific scoring semantics.",
            ["unknown_178"] = "This unknown session field matched Garmin Connect sweat-loss output in the repository reference activity, but Garmin does not document the semantic name in FIT SDK 21.195.0.",
            ["unknown_205"] = "This unknown session field matched Garmin Connect beginning potential in the repository reference activity, but Garmin does not document the semantic name in FIT SDK 21.195.0.",
            ["unknown_206"] = "This unknown session field matched Garmin Connect ending potential in the repository reference activity, but Garmin does not document the semantic name in FIT SDK 21.195.0.",
            ["unknown_207"] = "This unknown session field matched Garmin Connect minimum stamina in the repository reference activity, but Garmin does not document the semantic name in FIT SDK 21.195.0.",
        }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    private static readonly JsonSerializerOptions s_manifestSerializerOptions = CreateManifestSerializerOptions();

    /// <inheritdoc/>
    public async Task<CsvExportResult> ExportAsync(CsvExportRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        EnsureStructuredCsvTarget(request.Options.Target);

        int anticipatedArtifactCount = request.NodeRequests.Length + request.SourceActivity.AncillaryData.Messages.Length + 4;
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
                    rowCount,
                    BuildCoreArtifactRelativePath(Path.GetFileName(nodeRequest.DestinationFilePath))));
        }

        if (request.Options.DataView == FitExportDataView.RawCanonical)
        {
            foreach (ExportedArtifact ancillaryArtifact in await ExportAncillaryFamiliesAsync(request, cancellationToken).ConfigureAwait(false))
            {
                exportedArtifacts.Add(ancillaryArtifact);
            }
        }
        else
        {
            foreach (ExportedArtifact consolidatedArtifact in await ExportConsolidatedAncillaryArtifactsAsync(request, cancellationToken).ConfigureAwait(false))
            {
                exportedArtifacts.Add(consolidatedArtifact);
            }

            ExportedArtifact? rawUnmappedArtifact = await ExportRawUnmappedArtifactAsync(request, cancellationToken).ConfigureAwait(false);
            if (rawUnmappedArtifact is not null)
            {
                exportedArtifacts.Add(rawUnmappedArtifact);
            }
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
            string destinationFilePath = Path.Combine(request.OutputDirectoryPath, RawLosslessDirectoryName, artifactFileName);
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
                    rowCount,
                    BuildRawLosslessArtifactRelativePath(artifactFileName)));
        }

        return exportedArtifacts.ToImmutable();
    }

    private static async Task<ImmutableArray<ExportedArtifact>> ExportConsolidatedAncillaryArtifactsAsync(
        CsvExportRequest request,
        CancellationToken cancellationToken)
    {
        if (request.SourceActivity.AncillaryData.Messages.IsDefaultOrEmpty)
        {
            return ImmutableArray<ExportedArtifact>.Empty;
        }

        ImmutableArray<AncillaryMessageFamily> ancillaryFamilies = GroupAncillaryFamilies(request.SourceActivity.AncillaryData.Messages);
        ImmutableArray<ExportedArtifact>.Builder exportedArtifacts = ImmutableArray.CreateBuilder<ExportedArtifact>(2);
        foreach (IGrouping<CsvExportArtifactGroup, AncillaryMessageFamily> artifactGroup in ancillaryFamilies
            .Where(static ancillaryFamily => TryGetConsolidatedAncillaryArtifactGroup(ancillaryFamily.Key.MessageName, out _))
            .GroupBy(static ancillaryFamily =>
            {
                _ = TryGetConsolidatedAncillaryArtifactGroup(ancillaryFamily.Key.MessageName, out CsvExportArtifactGroup group);
                return group;
            }))
        {
            cancellationToken.ThrowIfCancellationRequested();

            CsvExportArtifactGroup group = artifactGroup.Key;
            ImmutableArray<ConsolidatedAncillaryRow> rows = BuildConsolidatedAncillaryRows(artifactGroup.ToImmutableArray());
            string artifactFileName = BuildConsolidatedAncillaryArtifactFileName(request.SourceFileNameWithoutExtension, group);
            string destinationFilePath = Path.Combine(
                request.OutputDirectoryPath,
                GetArtifactGroupDirectoryName(group),
                artifactFileName);
            int rowCount = await ExportConsolidatedAncillaryRowsAsync(
                rows,
                destinationFilePath,
                request.Encoding,
                request.Delimiter,
                cancellationToken).ConfigureAwait(false);

            exportedArtifacts.Add(
                new ExportedArtifact(
                    ExportedArtifactKind.ConsolidatedCsv,
                    FitNodeType.Ancillary,
                    artifactFileName,
                    destinationFilePath,
                    rowCount,
                    BuildArtifactRelativePath(GetArtifactGroupDirectoryName(group), artifactFileName)));
        }

        return exportedArtifacts.ToImmutable();
    }

    private static async Task<ExportedArtifact?> ExportRawUnmappedArtifactAsync(
        CsvExportRequest request,
        CancellationToken cancellationToken)
    {
        ImmutableArray<RawUnmappedRow> rawUnmappedRows = BuildRawUnmappedRows(request.SourceActivity, request.SourceFileNameWithoutExtension);
        if (rawUnmappedRows.IsDefaultOrEmpty)
        {
            return null;
        }

        string artifactFileName = request.SourceFileNameWithoutExtension + RawUnmappedFileNameSuffix;
        string destinationFilePath = Path.Combine(request.OutputDirectoryPath, RawUnmappedDirectoryName, artifactFileName);
        int rowCount = await ExportRawUnmappedRowsAsync(
            rawUnmappedRows,
            artifactFileName,
            destinationFilePath,
            request.Encoding,
            request.Delimiter,
            cancellationToken).ConfigureAwait(false);

        return new ExportedArtifact(
            ExportedArtifactKind.ConsolidatedCsv,
            FitNodeType.Ancillary,
            artifactFileName,
            destinationFilePath,
            rowCount,
            BuildArtifactRelativePath(RawUnmappedDirectoryName, artifactFileName));
    }

    private static ImmutableArray<ConsolidatedAncillaryRow> BuildConsolidatedAncillaryRows(
        ImmutableArray<AncillaryMessageFamily> ancillaryFamilies)
    {
        ImmutableArray<ConsolidatedAncillaryRow>.Builder builder = ImmutableArray.CreateBuilder<ConsolidatedAncillaryRow>();
        foreach (AncillaryMessageFamily ancillaryFamily in ancillaryFamilies)
        {
            foreach (FitAncillaryMessage ancillaryMessage in ancillaryFamily.Messages)
            {
                foreach (FitFieldSnapshot field in ancillaryMessage.Fields)
                {
                    FitExportFieldClassification classification = GetClassification(field.MessageName, field.Kind);
                    if (IsRawUnmappedClassification(classification))
                    {
                        continue;
                    }

                    for (int valueIndex = 0; valueIndex < field.OriginalValues.Length; valueIndex++)
                    {
                        FitFieldValue fieldValue = field.OriginalValues[valueIndex];
                        builder.Add(
                            new ConsolidatedAncillaryRow(
                                ancillaryMessage.Original.MessageName,
                                ancillaryMessage.Original.MessageNumber,
                                ancillaryMessage.Original.Identity.SequenceNumber,
                                ancillaryMessage.Original.Identity.MessageIndex,
                                ancillaryMessage.Original.LocalMessageNumber,
                                ancillaryMessage.Original.TimestampUtc,
                                field.Key.FieldNumber,
                                field.OriginalName,
                                valueIndex,
                                field.OriginalValues.Length,
                                FormatSingleValue(fieldValue.RawValue),
                                FormatSingleValue(fieldValue.DecodedValue),
                                field.Units,
                                classification,
                                BuildFieldNotes(field.MessageName, field.OriginalName, field.Kind, isAncillary: true)));
                    }
                }
            }
        }

        return builder
            .OrderBy(static row => row.MessageNumber)
            .ThenBy(static row => row.RowSequence)
            .ThenBy(static row => row.FieldNumber)
            .ThenBy(static row => row.ValueIndex)
            .ToImmutableArray();
    }

    private static async Task<int> ExportConsolidatedAncillaryRowsAsync(
        ImmutableArray<ConsolidatedAncillaryRow> rows,
        string destinationFilePath,
        Encoding encoding,
        char delimiter,
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

        await WriteLineAsync(
            writer,
            [
                "message_name",
                "message_number",
                "row_sequence",
                "message_index",
                "local_message_number",
                "timestamp [UTC]",
                "field_number",
                "field_name",
                "value_index",
                "value_count",
                "raw_value",
                "decoded_value",
                "unit",
                "classification",
                "notes"
            ],
            delimiter,
            cancellationToken).ConfigureAwait(false);

        foreach (ConsolidatedAncillaryRow row in rows)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await WriteLineAsync(
                writer,
                [
                    row.MessageName,
                    row.MessageNumber.ToString(CultureInfo.InvariantCulture),
                    row.RowSequence.ToString(CultureInfo.InvariantCulture),
                    row.MessageIndex?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
                    row.LocalMessageNumber?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
                    row.TimestampUtc is DateTimeOffset timestampUtc ? FormatSingleValue(timestampUtc) : string.Empty,
                    row.FieldNumber.ToString(CultureInfo.InvariantCulture),
                    row.FieldName,
                    row.ValueIndex.ToString(CultureInfo.InvariantCulture),
                    row.ValueCount.ToString(CultureInfo.InvariantCulture),
                    row.RawValue,
                    row.DecodedValue,
                    row.Unit ?? string.Empty,
                    row.Classification.ToString(),
                    row.Notes ?? string.Empty,
                ],
                delimiter,
                cancellationToken).ConfigureAwait(false);
        }

        await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
        return rows.Length;
    }

    private static async Task<int> ExportRawUnmappedRowsAsync(
        ImmutableArray<RawUnmappedRow> rawUnmappedRows,
        string sourceArtifactName,
        string destinationFilePath,
        Encoding encoding,
        char delimiter,
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

        await WriteLineAsync(
            writer,
            [
                "node_type",
                "message_number",
                "message_name",
                "row_sequence",
                "message_index",
                "local_message_number",
                "timestamp [UTC]",
                "field_number",
                "field_name",
                "value_index",
                "value_count",
                "raw_value",
                "decoded_value",
                "unit",
                "source_artifact",
                "classification",
                "notes"
            ],
            delimiter,
            cancellationToken).ConfigureAwait(false);

        foreach (RawUnmappedRow rawUnmappedRow in rawUnmappedRows)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await WriteLineAsync(
                writer,
                [
                    rawUnmappedRow.NodeType,
                    rawUnmappedRow.MessageNumber.ToString(CultureInfo.InvariantCulture),
                    rawUnmappedRow.MessageName,
                    rawUnmappedRow.RowSequence.ToString(CultureInfo.InvariantCulture),
                    rawUnmappedRow.MessageIndex?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
                    rawUnmappedRow.LocalMessageNumber?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
                    rawUnmappedRow.TimestampUtc is DateTimeOffset timestampUtc ? FormatSingleValue(timestampUtc) : string.Empty,
                    rawUnmappedRow.FieldNumber.ToString(CultureInfo.InvariantCulture),
                    rawUnmappedRow.FieldName,
                    rawUnmappedRow.ValueIndex.ToString(CultureInfo.InvariantCulture),
                    rawUnmappedRow.ValueCount.ToString(CultureInfo.InvariantCulture),
                    rawUnmappedRow.RawValue,
                    rawUnmappedRow.DecodedValue,
                    rawUnmappedRow.Unit ?? string.Empty,
                    rawUnmappedRow.SourceArtifactName ?? sourceArtifactName,
                    rawUnmappedRow.Classification.ToString(),
                    rawUnmappedRow.Notes ?? string.Empty,
                ],
                delimiter,
                cancellationToken).ConfigureAwait(false);
        }

        await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
        return rawUnmappedRows.Length;
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
        ExportedArtifact manifestArtifact = new(
            ExportedArtifactKind.Manifest,
            FitNodeType.Activity,
            ManifestArtifactName,
            destinationFilePath,
            rowCount: 1,
            manifestFileName);
        CsvExportManifest manifest = BuildManifest(request, exportedArtifacts.Add(manifestArtifact));

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

        return manifestArtifact;
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
        TryAddDerivedSessionColumn(builder, sessions, DerivedSessionFieldKind.EstimatedSweatLoss, EstimatedSweatLossExportName, "ml", exportOptions);
        TryAddDerivedSessionColumn(builder, sessions, DerivedSessionFieldKind.BeginningPotential, BeginningPotentialExportName, "%", exportOptions);
        TryAddDerivedSessionColumn(builder, sessions, DerivedSessionFieldKind.EndingPotential, EndingPotentialExportName, "%", exportOptions);
        TryAddDerivedSessionColumn(builder, sessions, DerivedSessionFieldKind.MinimumStamina, MinimumStaminaExportName, "%", exportOptions);
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

            case DerivedSessionFieldKind.EstimatedSweatLoss:
                if (TryGetRestoredUnknownSessionValue(session, "unknown_178", out double estimatedSweatLoss))
                {
                    derivedValue = estimatedSweatLoss;
                    return true;
                }

                break;

            case DerivedSessionFieldKind.BeginningPotential:
                if (TryGetRestoredUnknownSessionValue(session, "unknown_205", out double beginningPotential))
                {
                    derivedValue = beginningPotential;
                    return true;
                }

                break;

            case DerivedSessionFieldKind.EndingPotential:
                if (TryGetRestoredUnknownSessionValue(session, "unknown_206", out double endingPotential))
                {
                    derivedValue = endingPotential;
                    return true;
                }

                break;

            case DerivedSessionFieldKind.MinimumStamina:
                if (TryGetRestoredUnknownSessionValue(session, "unknown_207", out double minimumStamina))
                {
                    derivedValue = minimumStamina;
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

    private static bool TryGetRestoredUnknownSessionValue(FitSession session, string fieldName, out double numericValue)
    {
        IReadOnlyDictionary<string, FitField> fieldLookup = CreateFieldLookup(session.Fields);
        if (!fieldLookup.TryGetValue(fieldName, out FitField? field))
        {
            numericValue = default;
            return false;
        }

        return TryGetCanonicalNumericFieldValue(field, out numericValue);
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

            movingTimeSeconds += Math.Min(intervalSeconds, 1d);
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
        DateTimeOffset? firstRecordTimestampUtc = session.Records
            .Select(static record => record.Original.TimestampUtc)
            .FirstOrDefault(static timestampUtc => timestampUtc.HasValue);
        if (firstRecordTimestampUtc is null)
        {
            return ImmutableArray<double>.Empty;
        }

        ImmutableArray<(int OffsetSeconds, double PowerWatts)> powerSamples = session.Records
            .Select(record => TryCreatePowerSample(record, firstRecordTimestampUtc, out (int OffsetSeconds, double PowerWatts) powerSample)
                ? powerSample
                : ((int OffsetSeconds, double PowerWatts)?)null)
            .Where(static powerSample => powerSample.HasValue)
            .Select(static powerSample => powerSample!.Value)
            .OrderBy(static powerSample => powerSample.OffsetSeconds)
            .ToImmutableArray();

        if (powerSamples.IsDefaultOrEmpty)
        {
            return ImmutableArray<double>.Empty;
        }

        int sampleCount = powerSamples.Max(static powerSample => powerSample.OffsetSeconds) + 1;
        double[] oneSecondPowerSamples = new double[sampleCount];
        foreach ((int offsetSeconds, double powerWatts) in powerSamples)
        {
            oneSecondPowerSamples[offsetSeconds] = powerWatts;
        }

        return ImmutableArray.Create(oneSecondPowerSamples);
    }

    private static bool TryCreatePowerSample(
        FitRecord record,
        DateTimeOffset? firstRecordTimestampUtc,
        out (int OffsetSeconds, double PowerWatts) powerSample)
    {
        powerSample = default;
        if (firstRecordTimestampUtc is not DateTimeOffset firstTimestampUtc
            || record.Original.TimestampUtc is not DateTimeOffset recordTimestampUtc)
        {
            return false;
        }

        int offsetSeconds = (int)Math.Round(
            (recordTimestampUtc - firstTimestampUtc).TotalSeconds,
            MidpointRounding.AwayFromZero);
        if (offsetSeconds < 0)
        {
            return false;
        }

        IReadOnlyDictionary<string, FitField> fieldLookup = CreateFieldLookup(record.Fields);
        if (!TryGetCanonicalNumericFieldValue(fieldLookup, "power", out double powerWatts)
            && !TryGetCanonicalNumericFieldValue(fieldLookup, "enhanced_power", out powerWatts))
        {
            return false;
        }

        powerSample = (offsetSeconds, powerWatts);
        return true;
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
        builder.Add(exportOptions.IncludeUnitSuffixInHeaders ? $"{MessageTimestampMetadataHeader} [UTC]" : MessageTimestampMetadataHeader);
        if (exportOptions.IncludeLocalTimeColumns)
        {
            builder.Add(exportOptions.IncludeUnitSuffixInHeaders ? $"{MessageTimestampMetadataHeader} [Local]" : $"{MessageTimestampMetadataHeader}_local");
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
        ImmutableArray<CsvExportArtifactManifestEntry> artifacts = BuildArtifactManifestEntries(
            request,
            ancillaryFamilies,
            exportedArtifacts);
        ImmutableArray<CsvExportFieldDictionaryEntry> fieldDictionary = BuildFieldDictionary(
            request,
            ancillaryFamilies,
            exportedArtifactsByName);
        CsvExportProfileCoverage profileCoverage = BuildProfileCoverage(fieldDictionary);

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
            Artifacts = artifacts,
            FieldDictionary = fieldDictionary,
            ProfileCoverage = profileCoverage,
        };
    }

    private static ImmutableArray<CsvExportArtifactManifestEntry> BuildArtifactManifestEntries(
        CsvExportRequest request,
        ImmutableArray<AncillaryMessageFamily> ancillaryFamilies,
        ImmutableArray<ExportedArtifact> exportedArtifacts)
    {
        ImmutableArray<CsvExportArtifactManifestEntry>.Builder builder = ImmutableArray.CreateBuilder<CsvExportArtifactManifestEntry>(exportedArtifacts.Length);
        Dictionary<string, AncillaryMessageFamily> ancillaryFamiliesByRawArtifactName = ancillaryFamilies.ToDictionary(
            ancillaryFamily => BuildAncillaryArtifactFileName(ancillaryFamily, request.SourceFileNameWithoutExtension),
            ancillaryFamily => ancillaryFamily,
            StringComparer.OrdinalIgnoreCase);

        foreach (ExportedArtifact exportedArtifact in exportedArtifacts)
        {
            builder.Add(BuildArtifactManifestEntry(request, ancillaryFamiliesByRawArtifactName, exportedArtifact));
        }

        return builder
            .OrderBy(static artifact => artifact.ArtifactGroup)
            .ThenBy(static artifact => artifact.ArtifactFileName, StringComparer.OrdinalIgnoreCase)
            .ToImmutableArray();
    }

    private static CsvExportArtifactManifestEntry BuildArtifactManifestEntry(
        CsvExportRequest request,
        IReadOnlyDictionary<string, AncillaryMessageFamily> ancillaryFamiliesByRawArtifactName,
        ExportedArtifact exportedArtifact)
    {
        DetermineArtifactPlacement(request, ancillaryFamiliesByRawArtifactName, exportedArtifact, out CsvExportArtifactLayer artifactLayer, out CsvExportArtifactGroup artifactGroup, out ImmutableArray<string> messageFamilies);

        return new CsvExportArtifactManifestEntry
        {
            ArtifactName = exportedArtifact.ArtifactName,
            ArtifactFileName = BuildRelativeArtifactPath(request.OutputDirectoryPath, exportedArtifact.FilePath),
            ArtifactKind = exportedArtifact.Kind,
            ArtifactLayer = artifactLayer,
            DataView = GetDataView(artifactLayer),
            ArtifactGroup = artifactGroup,
            NodeType = exportedArtifact.NodeType.ToString(),
            RowCount = exportedArtifact.RowCount,
            MessageFamilies = messageFamilies,
        };
    }

    private static CsvExportProfileCoverage BuildProfileCoverage(ImmutableArray<CsvExportFieldDictionaryEntry> fieldDictionary)
    {
        GarminFitProfileCatalog profileCatalog = GarminFitProfileCatalog.Default;
        ImmutableArray<CsvExportProfileCoverageEntry>.Builder entries = ImmutableArray.CreateBuilder<CsvExportProfileCoverageEntry>(fieldDictionary.Length);

        foreach (CsvExportFieldDictionaryEntry fieldEntry in fieldDictionary)
        {
            if (!TryGetProfileCoverageClassification(profileCatalog, fieldEntry, out FitProfileCoverageClassification classification, out string? notes))
            {
                continue;
            }

            entries.Add(
                new CsvExportProfileCoverageEntry
                {
                    CanonicalName = fieldEntry.CanonicalName,
                    SourceMessageFamily = fieldEntry.SourceMessageFamily,
                    SourceMessageNumber = fieldEntry.SourceMessageNumber,
                    SourceFieldName = fieldEntry.SourceFieldName,
                    Classification = classification,
                    Notes = notes,
                });
        }

        ImmutableArray<CsvExportProfileCoverageEntry> coverageEntries = entries
            .OrderBy(static entry => entry.SourceMessageNumber)
            .ThenBy(static entry => entry.CanonicalName, StringComparer.OrdinalIgnoreCase)
            .ToImmutableArray();

        return new CsvExportProfileCoverage
        {
            CatalogSource = profileCatalog.SourceWorkbook,
            MatchedPublicStandardProfileFieldCount = coverageEntries.Count(static entry => entry.Classification == FitProfileCoverageClassification.MatchedPublicStandardProfile),
            DeveloperFieldCount = coverageEntries.Count(static entry => entry.Classification == FitProfileCoverageClassification.DeveloperField),
            UnknownOrUnmappedPreservedFieldCount = coverageEntries.Count(static entry => entry.Classification == FitProfileCoverageClassification.UnknownOrUnmappedPreservedField),
            Entries = coverageEntries,
        };
    }

    private static bool TryGetProfileCoverageClassification(
        GarminFitProfileCatalog profileCatalog,
        CsvExportFieldDictionaryEntry fieldEntry,
        out FitProfileCoverageClassification classification,
        out string? notes)
    {
        switch (fieldEntry.Classification)
        {
            case FitExportFieldClassification.DirectStandardFit
                when profileCatalog.ContainsField(fieldEntry.SourceMessageFamily, fieldEntry.SourceMessageNumber, fieldEntry.SourceFieldName):
                classification = FitProfileCoverageClassification.MatchedPublicStandardProfile;
                notes = "Matched the generated public Garmin FIT profile catalog.";
                return true;

            case FitExportFieldClassification.DirectStandardFit:
                classification = FitProfileCoverageClassification.UnknownOrUnmappedPreservedField;
                notes = "The decoder marked this as standard, but the generated public Garmin FIT profile catalog did not contain the same message/field identity.";
                return true;

            case FitExportFieldClassification.DirectDeveloperField:
                classification = FitProfileCoverageClassification.DeveloperField;
                notes = "Developer field preserved outside the public standard FIT profile.";
                return true;

            case FitExportFieldClassification.UnmappedField:
            case FitExportFieldClassification.UnknownMessageFamily:
            case FitExportFieldClassification.RawPreservedField:
            case FitExportFieldClassification.VendorOrFutureField:
            case FitExportFieldClassification.MappedFromUnmappedFitField:
                classification = FitProfileCoverageClassification.UnknownOrUnmappedPreservedField;
                notes = fieldEntry.Classification == FitExportFieldClassification.MappedFromUnmappedFitField
                    ? "Structured convenience value mapped from a preserved unknown FIT field outside the public standard FIT profile."
                    : "Unknown, vendor, or future field preserved outside the public standard FIT profile.";
                return true;

            default:
                classification = default;
                notes = null;
                return false;
        }
    }

    private static CsvExportDataView GetDataView(CsvExportArtifactLayer artifactLayer) => artifactLayer switch
    {
        CsvExportArtifactLayer.RawLosslessArchive => CsvExportDataView.RawCanonicalFitView,
        CsvExportArtifactLayer.ConsolidatedMachineExport => CsvExportDataView.StructuredMachineView,
        CsvExportArtifactLayer.Manifest => CsvExportDataView.Manifest,
        _ => throw new ArgumentOutOfRangeException(nameof(artifactLayer), artifactLayer, "Unsupported artifact layer."),
    };

    private static void DetermineArtifactPlacement(
        CsvExportRequest request,
        IReadOnlyDictionary<string, AncillaryMessageFamily> ancillaryFamiliesByRawArtifactName,
        ExportedArtifact exportedArtifact,
        out CsvExportArtifactLayer artifactLayer,
        out CsvExportArtifactGroup artifactGroup,
        out ImmutableArray<string> messageFamilies)
    {
        switch (exportedArtifact.Kind)
        {
            case ExportedArtifactKind.NodeCsv:
                artifactLayer = CsvExportArtifactLayer.ConsolidatedMachineExport;
                artifactGroup = CsvExportArtifactGroup.Core;
                messageFamilies = EnumerateNodes(request.SourceActivity, exportedArtifact.NodeType)
                    .Select(static node => node.Original.MessageName)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToImmutableArray();
                return;

            case ExportedArtifactKind.AncillaryCsv:
                artifactLayer = CsvExportArtifactLayer.RawLosslessArchive;
                artifactGroup = CsvExportArtifactGroup.RawLossless;
                messageFamilies = ancillaryFamiliesByRawArtifactName.TryGetValue(exportedArtifact.ArtifactName, out AncillaryMessageFamily? rawAncillaryFamily)
                    ? [rawAncillaryFamily.Key.MessageName]
                    : [exportedArtifact.ArtifactName];
                return;

            case ExportedArtifactKind.ConsolidatedCsv:
                artifactLayer = CsvExportArtifactLayer.ConsolidatedMachineExport;
                if (string.Equals(exportedArtifact.ArtifactName, RawUnmappedArtifactName, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(Path.GetFileName(exportedArtifact.FilePath), request.SourceFileNameWithoutExtension + RawUnmappedFileNameSuffix, StringComparison.OrdinalIgnoreCase))
                {
                    artifactGroup = CsvExportArtifactGroup.RawUnmapped;
                    messageFamilies = BuildRawUnmappedMessageFamilies(request.SourceActivity);
                    return;
                }

                artifactGroup = GetArtifactGroupFromDirectory(exportedArtifact.FilePath);
                CsvExportArtifactGroup consolidatedArtifactGroup = artifactGroup;
                messageFamilies = ancillaryFamiliesByRawArtifactName.Values
                    .Where(ancillaryFamily =>
                        TryGetConsolidatedAncillaryArtifactGroup(ancillaryFamily.Key.MessageName, out CsvExportArtifactGroup ancillaryArtifactGroup)
                        && ancillaryArtifactGroup == consolidatedArtifactGroup)
                    .Select(static ancillaryFamily => ancillaryFamily.Key.MessageName)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(static messageFamily => messageFamily, StringComparer.OrdinalIgnoreCase)
                    .ToImmutableArray();
                return;

            case ExportedArtifactKind.Manifest:
                artifactLayer = CsvExportArtifactLayer.Manifest;
                artifactGroup = CsvExportArtifactGroup.Manifest;
                messageFamilies = [ManifestArtifactName];
                return;

            default:
                throw new ArgumentOutOfRangeException(nameof(exportedArtifact), exportedArtifact.Kind, "Unsupported exported artifact kind.");
        }
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
                    ArtifactFileName = BuildRelativeArtifactPath(request.OutputDirectoryPath, exportedArtifact.FilePath),
                    ArtifactKind = exportedArtifact.Kind,
                    ArtifactLayer = CsvExportArtifactLayer.ConsolidatedMachineExport,
                    DataView = CsvExportDataView.StructuredMachineView,
                    ArtifactGroup = CsvExportArtifactGroup.Core,
                    NodeType = nodeRequest.NodeType.ToString(),
                    RowCount = exportedArtifact.RowCount,
                    ContainsDeveloperFields = nodes.SelectMany(static node => node.Fields).Any(static field => field.Original.Kind == FitFieldKind.Developer),
                    ContainsUnknownOrVendorFields = nodes.SelectMany(static node => node.Fields).Any(static field => field.Original.Kind == FitFieldKind.Unknown),
                });
        }

        foreach (AncillaryMessageFamily ancillaryFamily in ancillaryFamilies)
        {
            if (!TryResolveAncillaryFamilyExportedArtifact(
                request,
                ancillaryFamily,
                exportedArtifactsByName,
                out ExportedArtifact? exportedArtifact,
                out CsvExportArtifactLayer artifactLayer,
                out CsvExportArtifactGroup artifactGroup))
            {
                continue;
            }

            builder.Add(
                new CsvExportMessageFamilyManifestEntry
                {
                    MessageFamily = ancillaryFamily.Key.MessageName,
                    MessageNumber = ancillaryFamily.Key.MessageNumber,
                    ArtifactName = exportedArtifact.ArtifactName,
                    ArtifactFileName = BuildRelativeArtifactPath(request.OutputDirectoryPath, exportedArtifact.FilePath),
                    ArtifactKind = exportedArtifact.Kind,
                    ArtifactLayer = artifactLayer,
                    DataView = GetDataView(artifactLayer),
                    ArtifactGroup = artifactGroup,
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

        AddOmittedNodeFamilyIfNeeded(
            builder,
            request.SourceActivity.Fields,
            FitNodeType.Activity,
            selectedNodeTypes.Contains(FitNodeType.Activity),
            request.SourceFileNameWithoutExtension);
        AddOmittedNodeFamilyIfNeeded(
            builder,
            request.SourceActivity.Sessions.SelectMany(static session => session.Fields).ToImmutableArray(),
            FitNodeType.Session,
            selectedNodeTypes.Contains(FitNodeType.Session),
            request.SourceFileNameWithoutExtension);
        AddOmittedNodeFamilyIfNeeded(
            builder,
            request.SourceActivity.Sessions.SelectMany(static session => session.Laps).SelectMany(static lap => lap.Fields).ToImmutableArray(),
            FitNodeType.Lap,
            selectedNodeTypes.Contains(FitNodeType.Lap),
            request.SourceFileNameWithoutExtension);
        AddOmittedNodeFamilyIfNeeded(
            builder,
            request.SourceActivity.Sessions.SelectMany(static session => session.Records).SelectMany(static record => record.Fields).ToImmutableArray(),
            FitNodeType.Record,
            selectedNodeTypes.Contains(FitNodeType.Record),
            request.SourceFileNameWithoutExtension);

        foreach (AncillaryMessageFamily ancillaryFamily in ancillaryFamilies)
        {
            if (TryResolveAncillaryFamilyExportedArtifact(
                request,
                ancillaryFamily,
                exportedArtifactsByName,
                out _,
                out _,
                out _))
            {
                continue;
            }

            string artifactFileName = BuildAncillaryArtifactFileName(ancillaryFamily, request.SourceFileNameWithoutExtension);
            builder.Add(
                new CsvExportMessageFamilyManifestEntry
                {
                    MessageFamily = ancillaryFamily.Key.MessageName,
                    MessageNumber = ancillaryFamily.Key.MessageNumber,
                    ArtifactName = artifactFileName,
                    ArtifactFileName = BuildRawLosslessArtifactRelativePath(artifactFileName),
                    ArtifactKind = ExportedArtifactKind.AncillaryCsv,
                    ArtifactLayer = CsvExportArtifactLayer.RawLosslessArchive,
                    DataView = CsvExportDataView.RawCanonicalFitView,
                    ArtifactGroup = CsvExportArtifactGroup.RawLossless,
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
        bool isExported,
        string sourceFileNameWithoutExtension)
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
                ArtifactFileName = BuildCoreArtifactRelativePath(
                    $"{sourceFileNameWithoutExtension}_{nodeType.ToString().ToLowerInvariant()}.csv"),
                ArtifactKind = ExportedArtifactKind.NodeCsv,
                ArtifactLayer = CsvExportArtifactLayer.ConsolidatedMachineExport,
                DataView = CsvExportDataView.StructuredMachineView,
                ArtifactGroup = CsvExportArtifactGroup.Core,
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

        AddNodeFieldEntries(
            uniqueFieldEntriesByColumnKey,
            request.SourceActivity.Fields,
            selectedColumnsByKey,
            exportedArtifactsByName,
            request.OutputDirectoryPath,
            request.Options.UnitSystem);
        foreach (FitSession session in request.SourceActivity.Sessions)
        {
            AddNodeFieldEntries(
                uniqueFieldEntriesByColumnKey,
                session.Fields,
                selectedColumnsByKey,
                exportedArtifactsByName,
                request.OutputDirectoryPath,
                request.Options.UnitSystem);
            foreach (FitLap lap in session.Laps)
            {
                AddNodeFieldEntries(
                    uniqueFieldEntriesByColumnKey,
                    lap.Fields,
                    selectedColumnsByKey,
                    exportedArtifactsByName,
                    request.OutputDirectoryPath,
                    request.Options.UnitSystem);
            }

            foreach (FitRecord record in session.Records)
            {
                AddNodeFieldEntries(
                    uniqueFieldEntriesByColumnKey,
                    record.Fields,
                    selectedColumnsByKey,
                    exportedArtifactsByName,
                    request.OutputDirectoryPath,
                    request.Options.UnitSystem);
            }
        }

        foreach (AncillaryMessageFamily ancillaryFamily in ancillaryFamilies)
        {
            string rawArtifactFileName = BuildAncillaryArtifactFileName(ancillaryFamily, request.SourceFileNameWithoutExtension);

            foreach (FitFieldSnapshot field in ancillaryFamily.Messages.SelectMany(static message => message.Fields))
            {
                if (uniqueFieldEntriesByColumnKey.ContainsKey(field.ExportColumnKey))
                {
                    continue;
                }

                FitExportFieldClassification classification = GetClassification(field.MessageName, field.Kind);
                CsvExportFieldAliasMetadata? aliasMetadata = GetAliasMetadata(field.MessageName, field.OriginalName);
                string? artifactName = ResolveAncillaryDictionaryArtifactName(
                    request,
                    exportedArtifactsByName,
                    ancillaryFamily,
                    classification,
                    rawArtifactFileName);

                uniqueFieldEntriesByColumnKey.Add(
                    field.ExportColumnKey,
                    new CsvExportFieldDictionaryEntry
                    {
                        ExportName = field.OriginalName,
                        CanonicalName = BuildCanonicalName(field.MessageName, field.OriginalName),
                        CanonicalMessageFamily = field.MessageName,
                        CanonicalFieldName = field.OriginalName,
                        NodeType = FitNodeType.Ancillary.ToString(),
                        SourceMessageFamily = field.MessageName,
                        SourceMessageNumber = field.Key.MessageNumber,
                        SourceFieldName = field.OriginalName,
                        Classification = classification,
                        Unit = GetNormalizedUnit(field, request.Options.UnitSystem) ?? field.Units,
                        Alias = aliasMetadata?.DisplayAliasDefault,
                        AliasMetadata = aliasMetadata,
                        DerivationFormula = null,
                        IsExported = artifactName is not null,
                        ArtifactName = artifactName,
                        IsArray = field.IsArray,
                        ValueShape = field.IsArray ? ArrayValueShape : ScalarValueShape,
                        ValueSeparator = field.IsArray ? ArrayValueSeparator : null,
                        ValueOrdering = field.IsArray ? ArrayValueOrdering : null,
                        Notes = BuildFieldNotes(field.MessageName, field.OriginalName, field.Kind, isAncillary: true),
                    });
            }
        }

        ImmutableArray<CsvExportFieldDictionaryEntry>.Builder builder = ImmutableArray.CreateBuilder<CsvExportFieldDictionaryEntry>(
            uniqueFieldEntriesByColumnKey.Count + 12);
        builder.AddRange(uniqueFieldEntriesByColumnKey.Values);
        AddDerivedSessionFieldEntries(builder, request, exportedArtifactsByName);
        AddAuditOnlyReferenceEntries(builder, request.SourceActivity);

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
        string outputDirectoryPath,
        FitExportUnitSystem unitSystem)
    {
        foreach (FitField field in fields)
        {
            if (uniqueFieldEntriesByColumnKey.ContainsKey(field.Original.ExportColumnKey))
            {
                continue;
            }

            bool isSelected = selectedColumnsByKey.TryGetValue(field.Original.ExportColumnKey, out CsvExportColumnSelectionMetadata? selectionMetadata);
            string? artifactName = isSelected
                && TryGetExportedArtifactRelativePath(outputDirectoryPath, exportedArtifactsByName, selectionMetadata!.ArtifactName, out string? exportedArtifactRelativePath)
                    ? exportedArtifactRelativePath
                    : null;
            CsvExportFieldAliasMetadata? aliasMetadata = GetAliasMetadata(field.Original.MessageName, field.Original.OriginalName);

            uniqueFieldEntriesByColumnKey.Add(
                field.Original.ExportColumnKey,
                new CsvExportFieldDictionaryEntry
                {
                    ExportName = isSelected ? selectionMetadata!.ColumnName : field.State.ColumnName,
                    CanonicalName = BuildCanonicalName(field.Original.MessageName, field.Original.OriginalName),
                    CanonicalMessageFamily = field.Original.MessageName,
                    CanonicalFieldName = field.Original.OriginalName,
                    NodeType = field.Original.Key.NodeType.ToString(),
                    SourceMessageFamily = field.Original.MessageName,
                    SourceMessageNumber = field.Original.Key.MessageNumber,
                    SourceFieldName = field.Original.OriginalName,
                    Classification = GetClassification(field.Original.MessageName, field.Original.Kind),
                    Unit = GetNormalizedUnit(field, unitSystem) is string normalizedUnit
                        ? normalizedUnit
                        : field.Original.Units,
                    Alias = aliasMetadata?.DisplayAliasDefault,
                    AliasMetadata = aliasMetadata,
                    DerivationFormula = null,
                    IsExported = isSelected && artifactName is not null,
                    ArtifactName = artifactName,
                    IsArray = field.Original.IsArray,
                    ValueShape = field.Original.IsArray ? ArrayValueShape : ScalarValueShape,
                    ValueSeparator = field.Original.IsArray ? ArrayValueSeparator : null,
                    ValueOrdering = field.Original.IsArray ? ArrayValueOrdering : null,
                    Notes = BuildFieldNotes(field.Original.MessageName, field.Original.OriginalName, field.Original.Kind, isAncillary: false),
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
            .Select(artifactName => TryGetExportedArtifactRelativePath(request.OutputDirectoryPath, exportedArtifactsByName, artifactName, out string? artifactRelativePath)
                ? artifactRelativePath
                : null)
            .FirstOrDefault(static artifactName => artifactName is not null);

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
            DerivedSessionFieldKind.EstimatedSweatLoss,
            EstimatedSweatLossExportName,
            "session",
            EstimatedSweatLossAlias,
            "unknown_178",
            unit: "ml");
        AddDerivedSessionFieldEntry(
            builder,
            request,
            sessionArtifactName,
            DerivedSessionFieldKind.BeginningPotential,
            BeginningPotentialExportName,
            "session",
            BeginningPotentialAlias,
            "unknown_205",
            unit: "%");
        AddDerivedSessionFieldEntry(
            builder,
            request,
            sessionArtifactName,
            DerivedSessionFieldKind.EndingPotential,
            EndingPotentialExportName,
            "session",
            EndingPotentialAlias,
            "unknown_206",
            unit: "%");
        AddDerivedSessionFieldEntry(
            builder,
            request,
            sessionArtifactName,
            DerivedSessionFieldKind.MinimumStamina,
            MinimumStaminaExportName,
            "session",
            MinimumStaminaAlias,
            "unknown_207",
            unit: "%");
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
        CsvExportFieldProvenance? provenance = CreateDerivedSessionFieldProvenance(kind, classification, unit);
        builder.Add(
            new CsvExportFieldDictionaryEntry
            {
                ExportName = exportName,
                CanonicalName = BuildCanonicalName(sourceMessageFamily, exportName),
                CanonicalMessageFamily = sourceMessageFamily,
                CanonicalFieldName = exportName,
                NodeType = FitNodeType.Session.ToString(),
                SourceMessageFamily = sourceMessageFamily,
                SourceMessageNumber = sourceMessageFamily.Equals("record", StringComparison.OrdinalIgnoreCase) ? (ushort)20 : (ushort)18,
                SourceFieldName = GetDerivedSourceFieldName(kind),
                Classification = classification,
                Unit = unit,
                Alias = alias,
                AliasMetadata = CreateAliasMetadata(
                    alias,
                    GetDerivedAliasKind(classification),
                    confidence: classification == FitExportFieldClassification.MappedFromUnmappedFitField ? 0.55 : 0.95,
                    isDirectAlias: classification == FitExportFieldClassification.DirectStandardFit,
                    isDerivedAlias: classification is FitExportFieldClassification.DerivedFromFit or FitExportFieldClassification.DerivedFromRestoredFitMessages,
                    notes: classification == FitExportFieldClassification.MappedFromUnmappedFitField
                        ? "Mapped from preserved unknown session fields that match the Garmin Connect reference activity."
                        : null),
                Provenance = provenance,
                DerivationFormula = IsFormulaDerivedClassification(classification) ? derivationFormula : null,
                IsExported = true,
                ArtifactName = sessionArtifactName,
                IsArray = false,
                ValueShape = ScalarValueShape,
                ValueSeparator = null,
                ValueOrdering = null,
                Notes = classification == FitExportFieldClassification.MappedFromUnmappedFitField
                    ? "Mapped unknown-field convenience column added for structured export completeness."
                    : kind == DerivedSessionFieldKind.MovingTime
                        ? MovingSpeedDerivationNotes
                        : "Derived summary column added for structured export completeness.",
            });
    }

    private static CsvExportAliasKind GetDerivedAliasKind(FitExportFieldClassification classification) => classification switch
    {
        FitExportFieldClassification.DirectStandardFit => CsvExportAliasKind.DirectFieldAlias,
        FitExportFieldClassification.MappedFromUnmappedFitField => CsvExportAliasKind.HumanFriendlyAlias,
        _ => CsvExportAliasKind.DerivedFieldAlias
    };

    private static string? GetDerivedSourceFieldName(DerivedSessionFieldKind kind) => kind switch
    {
        DerivedSessionFieldKind.ActiveCalories => "total_calories, metabolic_calories",
        DerivedSessionFieldKind.EstimatedSweatLoss => "unknown_178",
        DerivedSessionFieldKind.BeginningPotential => "unknown_205",
        DerivedSessionFieldKind.EndingPotential => "unknown_206",
        DerivedSessionFieldKind.MinimumStamina => "unknown_207",
        DerivedSessionFieldKind.MovingTime => "total_moving_time or record speed/distance stream",
        DerivedSessionFieldKind.AverageMovingSpeed => "total_distance, moving_time",
        DerivedSessionFieldKind.MaxAveragePowerTwentyMinutes => "record power",
        _ => null
    };

    private static CsvExportFieldProvenance? CreateDerivedSessionFieldProvenance(
        DerivedSessionFieldKind kind,
        FitExportFieldClassification classification,
        string unit)
    {
        if (classification == FitExportFieldClassification.Unavailable)
        {
            return null;
        }

        return classification switch
        {
            FitExportFieldClassification.MappedFromUnmappedFitField => CreateMappedUnknownFieldProvenance(kind, unit),
            FitExportFieldClassification.DirectStandardFit => CreateDirectProjectedFieldProvenance(kind, unit),
            _ => CreateFormulaDerivedFieldProvenance(kind, unit),
        };
    }

    private static CsvExportFieldProvenance CreateFormulaDerivedFieldProvenance(DerivedSessionFieldKind kind, string unit)
        => new()
        {
            Kind = CsvExportFieldProvenanceKind.FormulaDerived,
            SourceFields = GetDerivedProvenanceSourceFields(kind),
            SourceMessageFamilies = GetDerivedProvenanceSourceMessageFamilies(kind),
            Formula = GetDerivedProvenanceFormula(kind),
            Unit = unit,
            RoundingOrTolerance = GetDerivedRoundingOrTolerance(kind),
            SourceEvidence = "Computed from decoded FIT values preserved in View A.",
            MappingReason = null,
            Notes = GetDerivedProvenanceNotes(kind),
        };

    private static CsvExportFieldProvenance CreateMappedUnknownFieldProvenance(DerivedSessionFieldKind kind, string unit)
        => new()
        {
            Kind = CsvExportFieldProvenanceKind.MappedFromUnmappedFitField,
            SourceFields = GetDerivedProvenanceSourceFields(kind),
            SourceMessageFamilies = GetDerivedProvenanceSourceMessageFamilies(kind),
            Formula = null,
            Unit = unit,
            RoundingOrTolerance = MappedUnknownFieldRoundingNotes,
            SourceEvidence = MappedUnknownFieldSourceEvidence,
            MappingReason = MappedUnknownFieldMappingReason,
            Notes = "This is a structured convenience projection of a preserved unknown FIT field, not a public standard FIT profile field and not a formula-derived value.",
        };

    private static CsvExportFieldProvenance CreateDirectProjectedFieldProvenance(DerivedSessionFieldKind kind, string unit)
        => new()
        {
            Kind = CsvExportFieldProvenanceKind.Direct,
            SourceFields = GetDerivedProvenanceSourceFields(kind),
            SourceMessageFamilies = GetDerivedProvenanceSourceMessageFamilies(kind),
            Formula = null,
            Unit = unit,
            RoundingOrTolerance = null,
            SourceEvidence = "Projected from a decoded public standard FIT field when present.",
            MappingReason = null,
            Notes = "The field is emitted as a structured convenience column because Garmin Connect presents the same concept prominently.",
        };

    private static ImmutableArray<string> GetDerivedProvenanceSourceFields(DerivedSessionFieldKind kind) => kind switch
    {
        DerivedSessionFieldKind.ActiveCalories => ["session.total_calories", "session.metabolic_calories"],
        DerivedSessionFieldKind.EstimatedSweatLoss => ["session.unknown_178"],
        DerivedSessionFieldKind.BeginningPotential => ["session.unknown_205"],
        DerivedSessionFieldKind.EndingPotential => ["session.unknown_206"],
        DerivedSessionFieldKind.MinimumStamina => ["session.unknown_207"],
        DerivedSessionFieldKind.MovingTime => ["session.total_moving_time", "session.moving_time", "record.enhanced_speed", "record.speed", "record.distance", "record.timestamp"],
        DerivedSessionFieldKind.AverageMovingSpeed => ["session.total_distance", "session.total_moving_time", "session.moving_time", "record.enhanced_speed", "record.speed", "record.distance"],
        DerivedSessionFieldKind.MaxAveragePowerTwentyMinutes => ["record.power", "record.timestamp"],
        _ => []
    };

    private static ImmutableArray<string> GetDerivedProvenanceSourceMessageFamilies(DerivedSessionFieldKind kind) => kind switch
    {
        DerivedSessionFieldKind.MaxAveragePowerTwentyMinutes => ["record"],
        DerivedSessionFieldKind.MovingTime => ["session", "record"],
        DerivedSessionFieldKind.AverageMovingSpeed => ["session", "record"],
        _ => ["session"]
    };

    private static string? GetDerivedProvenanceFormula(DerivedSessionFieldKind kind) => kind switch
    {
        DerivedSessionFieldKind.ActiveCalories => ActiveCaloriesFormula,
        DerivedSessionFieldKind.MovingTime => "Use session.total_moving_time when present; otherwise sum qualifying record intervals where speed exceeds 0.1 m/s or distance increases.",
        DerivedSessionFieldKind.AverageMovingSpeed => AvgMovingSpeedFormula,
        DerivedSessionFieldKind.MaxAveragePowerTwentyMinutes => MaxAveragePowerTwentyMinutesFormula,
        _ => null
    };

    private static string GetDerivedRoundingOrTolerance(DerivedSessionFieldKind kind) => kind switch
    {
        DerivedSessionFieldKind.MovingTime => "The derived fallback counts whole elapsed seconds from record intervals; direct total_moving_time is emitted without presentation rounding.",
        DerivedSessionFieldKind.MaxAveragePowerTwentyMinutes => "The rolling window uses whole-second sample-hold/zero-fill behavior; the computed numeric value is emitted without presentation rounding.",
        _ => FormulaDerivedRoundingNotes
    };

    private static string? GetDerivedProvenanceNotes(DerivedSessionFieldKind kind) => kind switch
    {
        DerivedSessionFieldKind.MovingTime => MovingSpeedDerivationNotes,
        DerivedSessionFieldKind.MaxAveragePowerTwentyMinutes => "The calculation is intended to match Garmin Connect's 20-minute power summary when the FIT record power stream is complete.",
        _ => null
    };

    private static FitExportFieldClassification GetDerivedClassification(FitActivity activity, DerivedSessionFieldKind kind) => kind switch
    {
        DerivedSessionFieldKind.MovingTime when activity.Sessions.Any(session => HasNamedField(session.Fields, "total_moving_time") || HasNamedField(session.Fields, "moving_time"))
            => FitExportFieldClassification.DirectStandardFit,
        DerivedSessionFieldKind.MovingTime => FitExportFieldClassification.DerivedFromFit,
        DerivedSessionFieldKind.ActiveCalories => FitExportFieldClassification.DerivedFromFit,
        DerivedSessionFieldKind.EstimatedSweatLoss => FitExportFieldClassification.MappedFromUnmappedFitField,
        DerivedSessionFieldKind.BeginningPotential => FitExportFieldClassification.MappedFromUnmappedFitField,
        DerivedSessionFieldKind.EndingPotential => FitExportFieldClassification.MappedFromUnmappedFitField,
        DerivedSessionFieldKind.MinimumStamina => FitExportFieldClassification.MappedFromUnmappedFitField,
        DerivedSessionFieldKind.AverageMovingSpeed => FitExportFieldClassification.DerivedFromFit,
        DerivedSessionFieldKind.MaxAveragePowerTwentyMinutes => FitExportFieldClassification.DerivedFromFit,
        _ => FitExportFieldClassification.Unavailable
    };

    private static bool HasNamedField(ImmutableArray<FitField> fields, string fieldName)
        => fields.Any(field => field.Original.OriginalName.Equals(fieldName, StringComparison.OrdinalIgnoreCase));

    private static void AddAuditOnlyReferenceEntries(ImmutableArray<CsvExportFieldDictionaryEntry>.Builder builder, FitActivity activity)
    {
        builder.Add(
            new CsvExportFieldDictionaryEntry
            {
                ExportName = "stamina",
                CanonicalName = BuildCanonicalName("session", "stamina"),
                CanonicalMessageFamily = "session",
                CanonicalFieldName = "stamina",
                NodeType = FitNodeType.Session.ToString(),
                SourceMessageFamily = "session",
                SourceMessageNumber = 18,
                SourceFieldName = null,
                Classification = FitExportFieldClassification.GarminConnectOnlyOrUnconfirmed,
                Unit = "%",
                Alias = "Stamina",
                AliasMetadata = CreateAliasMetadata("Stamina", CsvExportAliasKind.HumanFriendlyAlias, 0.3, false, false, "Shown in Garmin Connect, but not exposed as a named field in FIT SDK 21.195.0."),
                DerivationFormula = null,
                IsExported = false,
                ArtifactName = null,
                IsArray = false,
                ValueShape = ScalarValueShape,
                ValueSeparator = null,
                ValueOrdering = null,
                Notes = "Garmin FIT SDK 21.195.0 does not expose a named stamina field in Activity, Session, or Record messages. Raw unknown session fields may still carry related device-specific data.",
            });
        AddAuditOnlyReferenceEntryIfMissing(builder, activity, "unknown_205", BeginningPotentialExportName, "%", BeginningPotentialAlias, "No named FIT field is available in Garmin FIT SDK 21.195.0. Preserve raw unknown session fields when present and compare cautiously against Garmin Connect.");
        AddAuditOnlyReferenceEntryIfMissing(builder, activity, "unknown_206", EndingPotentialExportName, "%", EndingPotentialAlias, "No named FIT field is available in Garmin FIT SDK 21.195.0. Preserve raw unknown session fields when present and compare cautiously against Garmin Connect.");
        AddAuditOnlyReferenceEntryIfMissing(builder, activity, "unknown_207", MinimumStaminaExportName, "%", MinimumStaminaAlias, "No named FIT field is available in Garmin FIT SDK 21.195.0. Preserve raw unknown session fields when present and compare cautiously against Garmin Connect.");
        AddAuditOnlyReferenceEntryIfMissing(builder, activity, "unknown_178", EstimatedSweatLossExportName, "ml", EstimatedSweatLossAlias, "No named FIT field is available in Garmin FIT SDK 21.195.0 for this structured export path.");
        builder.Add(
            new CsvExportFieldDictionaryEntry
            {
                ExportName = "fluid_consumed",
                CanonicalName = BuildCanonicalName("session", "fluid_consumed"),
                CanonicalMessageFamily = "session",
                CanonicalFieldName = "fluid_consumed",
                NodeType = FitNodeType.Session.ToString(),
                SourceMessageFamily = "session",
                SourceMessageNumber = 18,
                SourceFieldName = null,
                Classification = FitExportFieldClassification.GarminConnectOnlyOrUnconfirmed,
                Unit = "ml",
                Alias = "Fluid Consumed",
                AliasMetadata = CreateAliasMetadata("Fluid Consumed", CsvExportAliasKind.HumanFriendlyAlias, 0.3, false, false, "Garmin Connect label recorded from the PDF reference."),
                DerivationFormula = null,
                IsExported = false,
                ArtifactName = null,
                IsArray = false,
                ValueShape = ScalarValueShape,
                ValueSeparator = null,
                ValueOrdering = null,
                Notes = "No named FIT field is available in Garmin FIT SDK 21.195.0 for this structured export path.",
            });
        builder.Add(
            new CsvExportFieldDictionaryEntry
            {
                ExportName = "fluid_net",
                CanonicalName = BuildCanonicalName("session", "fluid_net"),
                CanonicalMessageFamily = "session",
                CanonicalFieldName = "fluid_net",
                NodeType = FitNodeType.Session.ToString(),
                SourceMessageFamily = "session",
                SourceMessageNumber = 18,
                SourceFieldName = null,
                Classification = FitExportFieldClassification.GarminConnectOnlyOrUnconfirmed,
                Unit = "ml",
                Alias = "Fluid Net",
                AliasMetadata = CreateAliasMetadata("Fluid Net", CsvExportAliasKind.HumanFriendlyAlias, 0.3, false, false, "Garmin Connect label recorded from the PDF reference."),
                DerivationFormula = null,
                IsExported = false,
                ArtifactName = null,
                IsArray = false,
                ValueShape = ScalarValueShape,
                ValueSeparator = null,
                ValueOrdering = null,
                Notes = "No named FIT field is available in Garmin FIT SDK 21.195.0 for this structured export path.",
            });
        builder.Add(
            new CsvExportFieldDictionaryEntry
            {
                ExportName = "intensity_minutes",
                CanonicalName = BuildCanonicalName("session", "intensity_minutes"),
                CanonicalMessageFamily = "session",
                CanonicalFieldName = "intensity_minutes",
                NodeType = FitNodeType.Session.ToString(),
                SourceMessageFamily = "session",
                SourceMessageNumber = 18,
                SourceFieldName = null,
                Classification = FitExportFieldClassification.GarminConnectOnlyOrUnconfirmed,
                Unit = "min",
                Alias = "Intensity Minutes",
                AliasMetadata = CreateAliasMetadata("Intensity Minutes", CsvExportAliasKind.SectionLabel, 0.3, false, false, "Garmin Connect section label recorded from the PDF reference."),
                DerivationFormula = null,
                IsExported = false,
                ArtifactName = null,
                IsArray = false,
                ValueShape = ScalarValueShape,
                ValueSeparator = null,
                ValueOrdering = null,
                Notes = "No named FIT field is available in Garmin FIT SDK 21.195.0 for this structured export path.",
            });
        AddGarminConnectOnlyReferenceEntry(
            builder,
            "calories_consumed",
            "kcal",
            "Calories Consumed",
            CsvExportAliasKind.HumanFriendlyAlias,
            "Garmin Connect can show nutrition intake, but this is not present as a direct activity FIT field in the reference export path.");
        AddGarminConnectOnlyReferenceEntry(
            builder,
            "calories_net",
            "kcal",
            "Calories Net",
            CsvExportAliasKind.HumanFriendlyAlias,
            "Requires external intake data in addition to activity calories, so it is not a source-native FIT activity value.");
        AddGarminConnectOnlyReferenceEntry(
            builder,
            "gear",
            unit: null,
            alias: "Gear",
            aliasKind: CsvExportAliasKind.SectionLabel,
            notes: "Garmin Connect gear assignment appears to be account metadata outside the exported activity FIT source.");
        AddGarminConnectOnlyReferenceEntry(
            builder,
            "course",
            unit: null,
            alias: "Course",
            aliasKind: CsvExportAliasKind.SectionLabel,
            notes: "No reliable direct FIT course field was surfaced by the decoder for this structured export path.");
        AddGarminConnectOnlyReferenceEntry(
            builder,
            "summary_data",
            unit: null,
            alias: "Summary Data",
            aliasKind: CsvExportAliasKind.HumanFriendlyAlias,
            notes: "Garmin Connect presentation/source label, not a direct activity data field.");
    }

    private static void AddGarminConnectOnlyReferenceEntry(
        ImmutableArray<CsvExportFieldDictionaryEntry>.Builder builder,
        string exportName,
        string? unit,
        string alias,
        CsvExportAliasKind aliasKind,
        string notes)
        => builder.Add(
            new CsvExportFieldDictionaryEntry
            {
                ExportName = exportName,
                CanonicalName = BuildCanonicalName("garmin_connect_reference", exportName),
                CanonicalMessageFamily = "garmin_connect_reference",
                CanonicalFieldName = exportName,
                NodeType = FitNodeType.Activity.ToString(),
                SourceMessageFamily = "garmin_connect_reference",
                SourceMessageNumber = null,
                SourceFieldName = null,
                Classification = FitExportFieldClassification.GarminConnectOnlyOrUnconfirmed,
                Unit = unit,
                Alias = alias,
                AliasMetadata = CreateAliasMetadata(alias, aliasKind, 0.3, false, false, "Garmin Connect label recorded from the PDF reference."),
                DerivationFormula = null,
                IsExported = false,
                ArtifactName = null,
                IsArray = false,
                ValueShape = ScalarValueShape,
                ValueSeparator = null,
                ValueOrdering = null,
                Notes = notes,
            });

    private static void AddAuditOnlyReferenceEntryIfMissing(
        ImmutableArray<CsvExportFieldDictionaryEntry>.Builder builder,
        FitActivity activity,
        string sourceFieldName,
        string exportName,
        string unit,
        string alias,
        string notes)
    {
        if (activity.Sessions.Any(session => HasNamedField(session.Fields, sourceFieldName)))
        {
            return;
        }

        builder.Add(
            new CsvExportFieldDictionaryEntry
            {
                ExportName = exportName,
                CanonicalName = BuildCanonicalName("session", exportName),
                CanonicalMessageFamily = "session",
                CanonicalFieldName = exportName,
                NodeType = FitNodeType.Session.ToString(),
                SourceMessageFamily = "session",
                SourceMessageNumber = 18,
                SourceFieldName = null,
                Classification = FitExportFieldClassification.GarminConnectOnlyOrUnconfirmed,
                Unit = unit,
                Alias = alias,
                AliasMetadata = CreateAliasMetadata(alias, CsvExportAliasKind.HumanFriendlyAlias, 0.3, false, false, "Garmin Connect label recorded from the PDF reference."),
                DerivationFormula = null,
                IsExported = false,
                ArtifactName = null,
                IsArray = false,
                ValueShape = ScalarValueShape,
                ValueSeparator = null,
                ValueOrdering = null,
                Notes = notes,
            });
    }

    private static FitExportFieldClassification GetClassification(string messageName, FitFieldKind kind)
    {
        if (kind == FitFieldKind.Developer)
        {
            return FitExportFieldClassification.DirectDeveloperField;
        }

        if (string.Equals(messageName, "unknown", StringComparison.OrdinalIgnoreCase))
        {
            return FitExportFieldClassification.UnknownMessageFamily;
        }

        return kind switch
        {
            FitFieldKind.Standard => FitExportFieldClassification.DirectStandardFit,
            FitFieldKind.Unknown => FitExportFieldClassification.UnmappedField,
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unsupported FIT field kind."),
        };
    }

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

    private static bool IsFormulaDerivedClassification(FitExportFieldClassification classification)
        => classification is FitExportFieldClassification.DerivedFromFit
            or FitExportFieldClassification.DerivedFromRestoredFitMessages;

    private static bool IsRawUnmappedClassification(FitExportFieldClassification classification)
        => classification is FitExportFieldClassification.UnmappedField
            or FitExportFieldClassification.UnknownMessageFamily
            or FitExportFieldClassification.RawPreservedField
            or FitExportFieldClassification.VendorOrFutureField;

    private static string? BuildFieldNotes(string messageName, string originalName, FitFieldKind kind, bool isAncillary)
    {
        List<string> notes = [];
        if (string.Equals(messageName, "unknown", StringComparison.OrdinalIgnoreCase))
        {
            notes.Add("Preserved from an unknown FIT message family so the source payload remains available for later semantic mapping.");
        }
        else if (kind == FitFieldKind.Unknown)
        {
            notes.Add("Garmin FIT SDK 21.195.0 does not expose a semantic profile name for this field, so the raw source value is preserved under an unknown_* export name.");
        }

        if (isAncillary)
        {
            notes.Add("Exported from a restored ancillary FIT message family outside the Activity/Session/Lap/Record tree.");
        }

        if (s_fieldNotesByOriginalName.TryGetValue(NormalizeSchemaIdentifier(originalName), out string? note))
        {
            notes.Add(note);
        }

        return notes.Count == 0 ? null : string.Join(" ", notes);
    }

    private static string BuildCanonicalName(string messageFamily, string? fieldName)
        => string.IsNullOrWhiteSpace(fieldName)
            ? NormalizeSchemaIdentifier(messageFamily)
            : $"{NormalizeSchemaIdentifier(messageFamily)}.{NormalizeSchemaIdentifier(fieldName)}";

    private static string CreateAliasLookupKey(string messageFamily, string fieldName)
        => $"{NormalizeSchemaIdentifier(messageFamily)}:{NormalizeSchemaIdentifier(fieldName)}";

    private static CsvExportFieldAliasMetadata? GetAliasMetadata(string messageFamily, string fieldName)
        => s_fieldAliasDefinitionsByKey.TryGetValue(CreateAliasLookupKey(messageFamily, fieldName), out CsvFieldAliasDefinition? aliasDefinition)
            ? CreateAliasMetadata(
                aliasDefinition.DisplayAliasDefault,
                aliasDefinition.AliasKind,
                aliasDefinition.Confidence,
                aliasDefinition.IsDirectAlias,
                aliasDefinition.IsDerivedAlias,
                aliasDefinition.Notes)
            : null;

    private static CsvExportFieldAliasMetadata CreateAliasMetadata(
        string displayAliasDefault,
        CsvExportAliasKind aliasKind,
        double confidence,
        bool isDirectAlias,
        bool isDerivedAlias,
        string? notes)
        => new()
        {
            DisplayAliasDefault = displayAliasDefault,
            DisplayAliasSource = GarminConnectPdfAliasSource,
            DisplayAliasLocale = GarminConnectAliasLocale,
            DisplayAliasConfidence = confidence,
            AliasKind = aliasKind,
            IsDirectAlias = isDirectAlias,
            IsDerivedAlias = isDerivedAlias,
            Notes = notes,
        };

    private static string? ResolveAncillaryDictionaryArtifactName(
        CsvExportRequest request,
        FrozenDictionary<string, ExportedArtifact> exportedArtifactsByName,
        AncillaryMessageFamily ancillaryFamily,
        FitExportFieldClassification classification,
        string rawArtifactFileName)
    {
        if (TryResolveAncillaryFamilyExportedArtifact(
            request,
            ancillaryFamily,
            exportedArtifactsByName,
            out ExportedArtifact? exportedArtifact,
            out _,
            out _))
        {
            return BuildRelativeArtifactPath(request.OutputDirectoryPath, exportedArtifact.FilePath);
        }

        bool preferRawArtifact = IsRawUnmappedClassification(classification);

        if (!preferRawArtifact
            && TryGetConsolidatedAncillaryArtifactGroup(ancillaryFamily.Key.MessageName, out CsvExportArtifactGroup artifactGroup))
        {
            string consolidatedArtifactFileName = BuildConsolidatedAncillaryArtifactFileName(
                request.SourceFileNameWithoutExtension,
                artifactGroup);
            if (TryGetExportedArtifactRelativePath(
                request.OutputDirectoryPath,
                exportedArtifactsByName,
                consolidatedArtifactFileName,
                out string? consolidatedArtifactRelativePath))
            {
                return consolidatedArtifactRelativePath;
            }
        }

        return TryGetExportedArtifactRelativePath(
            request.OutputDirectoryPath,
            exportedArtifactsByName,
            rawArtifactFileName,
            out string? rawArtifactRelativePath)
                ? rawArtifactRelativePath
                : null;
    }

    private static bool TryResolveAncillaryFamilyExportedArtifact(
        CsvExportRequest request,
        AncillaryMessageFamily ancillaryFamily,
        FrozenDictionary<string, ExportedArtifact> exportedArtifactsByName,
        out ExportedArtifact exportedArtifact,
        out CsvExportArtifactLayer artifactLayer,
        out CsvExportArtifactGroup artifactGroup)
    {
        string rawArtifactFileName = BuildAncillaryArtifactFileName(ancillaryFamily, request.SourceFileNameWithoutExtension);
        if (exportedArtifactsByName.TryGetValue(rawArtifactFileName, out ExportedArtifact? rawArtifact))
        {
            exportedArtifact = rawArtifact;
            artifactLayer = CsvExportArtifactLayer.RawLosslessArchive;
            artifactGroup = CsvExportArtifactGroup.RawLossless;
            return true;
        }

        if (TryGetConsolidatedAncillaryArtifactGroup(ancillaryFamily.Key.MessageName, out CsvExportArtifactGroup consolidatedArtifactGroup))
        {
            string consolidatedArtifactFileName = BuildConsolidatedAncillaryArtifactFileName(
                request.SourceFileNameWithoutExtension,
                consolidatedArtifactGroup);
            if (exportedArtifactsByName.TryGetValue(consolidatedArtifactFileName, out ExportedArtifact? consolidatedArtifact))
            {
                exportedArtifact = consolidatedArtifact;
                artifactLayer = CsvExportArtifactLayer.ConsolidatedMachineExport;
                artifactGroup = consolidatedArtifactGroup;
                return true;
            }
        }

        string rawUnmappedArtifactFileName = request.SourceFileNameWithoutExtension + RawUnmappedFileNameSuffix;
        if (exportedArtifactsByName.TryGetValue(rawUnmappedArtifactFileName, out ExportedArtifact? rawUnmappedArtifact)
            && ancillaryFamily.Messages.SelectMany(static message => message.Fields).Any(static field => field.Kind == FitFieldKind.Unknown))
        {
            exportedArtifact = rawUnmappedArtifact;
            artifactLayer = CsvExportArtifactLayer.ConsolidatedMachineExport;
            artifactGroup = CsvExportArtifactGroup.RawUnmapped;
            return true;
        }

        exportedArtifact = null!;
        artifactLayer = default;
        artifactGroup = default;
        return false;
    }

    private static bool TryGetExportedArtifactRelativePath(
        string outputDirectoryPath,
        FrozenDictionary<string, ExportedArtifact> exportedArtifactsByName,
        string artifactName,
        out string? artifactRelativePath)
    {
        if (exportedArtifactsByName.TryGetValue(artifactName, out ExportedArtifact? exportedArtifact))
        {
            artifactRelativePath = BuildRelativeArtifactPath(outputDirectoryPath, exportedArtifact.FilePath);
            return true;
        }

        artifactRelativePath = null;
        return false;
    }

    private static string BuildRelativeArtifactPath(string outputDirectoryPath, string filePath)
        => Path.GetRelativePath(outputDirectoryPath, filePath).Replace('\\', '/');

    private static string BuildCoreArtifactRelativePath(string artifactFileName)
        => BuildArtifactRelativePath(CoreDirectoryName, artifactFileName);

    private static string BuildRawLosslessArtifactRelativePath(string artifactFileName)
        => BuildArtifactRelativePath(RawLosslessDirectoryName, artifactFileName);

    private static string BuildArtifactRelativePath(string directoryName, string artifactFileName)
        => $"{directoryName}/{artifactFileName}";

    private static bool TryGetConsolidatedAncillaryArtifactGroup(string messageName, out CsvExportArtifactGroup artifactGroup)
    {
        if (string.Equals(messageName, "unknown", StringComparison.OrdinalIgnoreCase))
        {
            artifactGroup = default;
            return false;
        }

        artifactGroup = s_analyticsMessageFamilies.Contains(messageName)
            ? CsvExportArtifactGroup.Analytics
            : CsvExportArtifactGroup.Metadata;
        return true;
    }

    private static string BuildConsolidatedAncillaryArtifactFileName(
        string sourceFileNameWithoutExtension,
        CsvExportArtifactGroup artifactGroup)
        => $"{sourceFileNameWithoutExtension}_{GetArtifactGroupDirectoryName(artifactGroup)}.csv";

    private static string GetArtifactGroupDirectoryName(CsvExportArtifactGroup artifactGroup) => artifactGroup switch
    {
        CsvExportArtifactGroup.Core => CoreDirectoryName,
        CsvExportArtifactGroup.Metadata => MetadataDirectoryName,
        CsvExportArtifactGroup.Analytics => AnalyticsDirectoryName,
        CsvExportArtifactGroup.RawUnmapped => RawUnmappedDirectoryName,
        CsvExportArtifactGroup.RawLossless => RawLosslessDirectoryName,
        CsvExportArtifactGroup.Manifest => string.Empty,
        _ => throw new ArgumentOutOfRangeException(nameof(artifactGroup), artifactGroup, "Unsupported artifact group."),
    };

    private static CsvExportArtifactGroup GetArtifactGroupFromDirectory(string filePath)
    {
        string? directoryName = Path.GetFileName(Path.GetDirectoryName(filePath));
        return directoryName?.ToLowerInvariant() switch
        {
            CoreDirectoryName => CsvExportArtifactGroup.Core,
            MetadataDirectoryName => CsvExportArtifactGroup.Metadata,
            AnalyticsDirectoryName => CsvExportArtifactGroup.Analytics,
            RawUnmappedDirectoryName => CsvExportArtifactGroup.RawUnmapped,
            RawLosslessDirectoryName => CsvExportArtifactGroup.RawLossless,
            _ => CsvExportArtifactGroup.Manifest,
        };
    }

    private static ImmutableArray<string> BuildRawUnmappedMessageFamilies(FitActivity activity)
    {
        SortedSet<string> messageFamilies = new(StringComparer.OrdinalIgnoreCase);

        foreach (FitField field in activity.Fields)
        {
            AddRawUnmappedMessageFamilyIfNeeded(messageFamilies, field.Original.MessageName, field.Original.Kind);
        }

        foreach (FitSession session in activity.Sessions)
        {
            foreach (FitField field in session.Fields)
            {
                AddRawUnmappedMessageFamilyIfNeeded(messageFamilies, field.Original.MessageName, field.Original.Kind);
            }

            foreach (FitLap lap in session.Laps)
            {
                foreach (FitField field in lap.Fields)
                {
                    AddRawUnmappedMessageFamilyIfNeeded(messageFamilies, field.Original.MessageName, field.Original.Kind);
                }
            }

            foreach (FitRecord record in session.Records)
            {
                foreach (FitField field in record.Fields)
                {
                    AddRawUnmappedMessageFamilyIfNeeded(messageFamilies, field.Original.MessageName, field.Original.Kind);
                }
            }
        }

        foreach (FitAncillaryMessage ancillaryMessage in activity.AncillaryData.Messages)
        {
            if (string.Equals(ancillaryMessage.Original.MessageName, "unknown", StringComparison.OrdinalIgnoreCase))
            {
                _ = messageFamilies.Add(ancillaryMessage.Original.MessageName);
            }

            foreach (FitFieldSnapshot field in ancillaryMessage.Fields)
            {
                AddRawUnmappedMessageFamilyIfNeeded(messageFamilies, field.MessageName, field.Kind);
            }
        }

        return messageFamilies.ToImmutableArray();
    }

    private static void AddRawUnmappedMessageFamilyIfNeeded(ISet<string> messageFamilies, string messageName, FitFieldKind fieldKind)
    {
        if (fieldKind == FitFieldKind.Unknown || string.Equals(messageName, "unknown", StringComparison.OrdinalIgnoreCase))
        {
            _ = messageFamilies.Add(messageName);
        }
    }

    private static ImmutableArray<RawUnmappedRow> BuildRawUnmappedRows(FitActivity activity, string sourceFileNameWithoutExtension)
    {
        ImmutableArray<RawUnmappedRow>.Builder builder = ImmutableArray.CreateBuilder<RawUnmappedRow>();

        AppendRawUnmappedRows(
            builder,
            activity.Original,
            activity.Fields.Select(static field => field.Original),
            BuildCoreArtifactRelativePath($"{sourceFileNameWithoutExtension}_activity.csv"));

        foreach (FitSession session in activity.Sessions)
        {
            AppendRawUnmappedRows(
                builder,
                session.Original,
                session.Fields.Select(static field => field.Original),
                BuildCoreArtifactRelativePath($"{sourceFileNameWithoutExtension}_session.csv"));

            foreach (FitLap lap in session.Laps)
            {
                AppendRawUnmappedRows(
                    builder,
                    lap.Original,
                    lap.Fields.Select(static field => field.Original),
                    BuildCoreArtifactRelativePath($"{sourceFileNameWithoutExtension}_lap.csv"));
            }

            foreach (FitRecord record in session.Records)
            {
                AppendRawUnmappedRows(
                    builder,
                    record.Original,
                    record.Fields.Select(static field => field.Original),
                    BuildCoreArtifactRelativePath($"{sourceFileNameWithoutExtension}_record.csv"));
            }
        }

        foreach (AncillaryMessageFamily ancillaryFamily in GroupAncillaryFamilies(activity.AncillaryData.Messages))
        {
            string sourceArtifactName = BuildRawLosslessArtifactRelativePath(
                BuildAncillaryArtifactFileName(ancillaryFamily, sourceFileNameWithoutExtension));

            foreach (FitAncillaryMessage ancillaryMessage in ancillaryFamily.Messages)
            {
                AppendRawUnmappedRows(builder, ancillaryMessage.Original, ancillaryMessage.Fields, sourceArtifactName);
            }
        }

        return builder
            .OrderBy(static row => row.MessageNumber)
            .ThenBy(static row => row.RowSequence)
            .ThenBy(static row => row.FieldNumber)
            .ThenBy(static row => row.ValueIndex)
            .ToImmutableArray();
    }

    private static void AppendRawUnmappedRows(
        ImmutableArray<RawUnmappedRow>.Builder builder,
        FitNodeSnapshot nodeSnapshot,
        IEnumerable<FitFieldSnapshot> fields,
        string sourceArtifactName)
    {
        foreach (FitFieldSnapshot field in fields)
        {
            FitExportFieldClassification classification = GetClassification(field.MessageName, field.Kind);
            if (!IsRawUnmappedClassification(classification))
            {
                continue;
            }

            for (int valueIndex = 0; valueIndex < field.OriginalValues.Length; valueIndex++)
            {
                FitFieldValue fieldValue = field.OriginalValues[valueIndex];
                builder.Add(
                    new RawUnmappedRow(
                        nodeSnapshot.Identity.NodeType.ToString(),
                        nodeSnapshot.MessageNumber,
                        nodeSnapshot.MessageName,
                        nodeSnapshot.Identity.SequenceNumber,
                        nodeSnapshot.Identity.MessageIndex,
                        nodeSnapshot.LocalMessageNumber,
                        nodeSnapshot.TimestampUtc,
                        field.Key.FieldNumber,
                        field.OriginalName,
                        valueIndex,
                        field.OriginalValues.Length,
                        FormatSingleValue(fieldValue.RawValue),
                        FormatSingleValue(fieldValue.DecodedValue),
                        field.Units,
                        sourceArtifactName,
                        classification,
                        BuildFieldNotes(field.MessageName, field.OriginalName, field.Kind, nodeSnapshot.Identity.NodeType == FitNodeType.Ancillary)));
            }
        }
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
            if (exportOptions.IncludeLocalTimeColumns
                && IsTimestampField(referenceField)
                && !IsLocalSourceTimestampField(referenceField))
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
            string timestampQualifier = isLocalTimeDuplicate
                ? "Local"
                : IsLocalSourceTimestampField(referenceField)
                    ? LocalSourceTimestampQualifier
                    : "UTC";
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
            if (IsLocalSourceTimestampField(field) && !isLocalTimeDuplicate)
            {
                return value;
            }

            return NormalizeTimestampValue(value, isLocalTimeDuplicate, exportOptions.LocalTimeZone);
        }

        if (TryNormalizeDurationToSeconds(value, field.Units, out double normalizedDurationValue))
        {
            return normalizedDurationValue;
        }

        if (TryNormalizeDistanceValue(field, value, exportOptions.UnitSystem, out double normalizedDistanceValue))
        {
            return normalizedDistanceValue;
        }

        if (TryNormalizeSpeedValue(field, value, exportOptions.UnitSystem, out double normalizedSpeedValue))
        {
            return normalizedSpeedValue;
        }

        if (TryNormalizeWorkValue(field, value, out double normalizedWorkValue))
        {
            return normalizedWorkValue;
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
        FitFieldSnapshot field,
        object? value,
        FitExportUnitSystem unitSystem,
        out double normalizedValue)
    {
        if (!IsDistanceLikeField(field)
            || !TryNormalizeDistanceToMeters(value, field.Units, out double distanceInMeters))
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
        FitFieldSnapshot field,
        object? value,
        FitExportUnitSystem unitSystem,
        out double normalizedValue)
    {
        if (!IsSpeedLikeField(field)
            || !TryNormalizeSpeedToMetersPerSecond(value, field.Units, out double speedInMetersPerSecond))
        {
            normalizedValue = default;
            return false;
        }

        normalizedValue = unitSystem == FitExportUnitSystem.Metric
            ? speedInMetersPerSecond * KilometersPerHourPerMeterPerSecond
            : speedInMetersPerSecond * MilesPerHourPerMeterPerSecond;
        return true;
    }

    private static bool TryNormalizeWorkValue(FitFieldSnapshot field, object? value, out double normalizedValue)
    {
        if (!IsWorkLikeField(field) || !TryConvertToDouble(value, out double numericValue))
        {
            normalizedValue = default;
            return false;
        }

        normalizedValue = NormalizeUnit(field.Units) switch
        {
            "j" => numericValue / JoulesPerKilojoule,
            "kj" => numericValue,
            _ => double.NaN,
        };

        return !double.IsNaN(normalizedValue);
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

    private static bool IsLocalSourceTimestampField(FitField field) => IsLocalSourceTimestampField(field.Original);

    private static bool IsLocalSourceTimestampField(FitFieldSnapshot field)
        => field.ProfileTypeName.Equals("LocalDateTime", StringComparison.OrdinalIgnoreCase)
            || field.OriginalName.Equals("local_timestamp", StringComparison.OrdinalIgnoreCase);

    private static string? GetNormalizedUnit(FitField field, FitExportUnitSystem unitSystem)
        => GetNormalizedUnit(field.Original, unitSystem);

    private static string? GetNormalizedUnit(FitFieldSnapshot field, FitExportUnitSystem unitSystem)
    {
        if (IsTimestampField(field))
        {
            return null;
        }

        if (s_unitOverridesByFieldName.TryGetValue(field.OriginalName, out string? unitOverride))
        {
            return unitOverride;
        }

        if (TryNormalizeDurationUnit(field.Units, out string durationUnit))
        {
            return durationUnit;
        }

        if (TryNormalizeDistanceUnit(field, unitSystem, out string distanceUnit))
        {
            return distanceUnit;
        }

        if (TryNormalizeSpeedUnit(field, unitSystem, out string speedUnit))
        {
            return speedUnit;
        }

        if (TryNormalizeWorkUnit(field, out string workUnit))
        {
            return workUnit;
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

    private static bool TryNormalizeDistanceUnit(FitFieldSnapshot field, FitExportUnitSystem unitSystem, out string normalizedUnit)
    {
        if (!IsDistanceLikeField(field))
        {
            normalizedUnit = string.Empty;
            return false;
        }

        switch (NormalizeUnit(field.Units))
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

    private static bool TryNormalizeSpeedUnit(FitFieldSnapshot field, FitExportUnitSystem unitSystem, out string normalizedUnit)
    {
        if (!IsSpeedLikeField(field))
        {
            normalizedUnit = string.Empty;
            return false;
        }

        switch (NormalizeUnit(field.Units))
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

    private static bool TryNormalizeWorkUnit(FitFieldSnapshot field, out string normalizedUnit)
    {
        if (!IsWorkLikeField(field))
        {
            normalizedUnit = string.Empty;
            return false;
        }

        switch (NormalizeUnit(field.Units))
        {
            case "j":
            case "kj":
                normalizedUnit = "kJ";
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

    private static bool IsDistanceLikeField(FitFieldSnapshot field)
        => field.OriginalName.Contains("distance", StringComparison.OrdinalIgnoreCase);

    private static bool IsSpeedLikeField(FitFieldSnapshot field)
        => field.OriginalName.Contains("speed", StringComparison.OrdinalIgnoreCase);

    private static bool IsWorkLikeField(FitFieldSnapshot field)
        => field.OriginalName.Contains("work", StringComparison.OrdinalIgnoreCase);

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
        ImmutableArray<byte> immutableByteArrayValue => string.Join("|", immutableByteArrayValue.Select(static byteValue => byteValue.ToString(CultureInfo.InvariantCulture))),
        byte[] byteArrayValue => string.Join("|", byteArrayValue.Select(static byteValue => byteValue.ToString(CultureInfo.InvariantCulture))),
        Array arrayValue when arrayValue.Rank == 1 => string.Join("|", arrayValue.Cast<object?>().Select(FormatSingleValue)),
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

    private static string NormalizeSchemaIdentifier(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        StringBuilder builder = new(value.Length + 8);
        bool previousWasSeparator = false;

        for (int index = 0; index < value.Length; index++)
        {
            char currentCharacter = value[index];
            if (!char.IsLetterOrDigit(currentCharacter))
            {
                if (builder.Length > 0 && !previousWasSeparator)
                {
                    _ = builder.Append('_');
                    previousWasSeparator = true;
                }

                continue;
            }

            if (builder.Length > 0 && ShouldInsertIdentifierSeparator(value, index))
            {
                _ = builder.Append('_');
            }

            _ = builder.Append(char.ToLowerInvariant(currentCharacter));
            previousWasSeparator = false;
        }

        return builder.ToString().Trim('_');
    }

    private static bool ShouldInsertIdentifierSeparator(string value, int index)
    {
        if (index <= 0)
        {
            return false;
        }

        char previousCharacter = value[index - 1];
        char currentCharacter = value[index];
        char? nextCharacter = index + 1 < value.Length ? value[index + 1] : null;

        if (!char.IsLetterOrDigit(previousCharacter) || !char.IsLetterOrDigit(currentCharacter))
        {
            return false;
        }

        if (char.IsDigit(previousCharacter) != char.IsDigit(currentCharacter))
        {
            return true;
        }

        return char.IsUpper(currentCharacter)
            && (char.IsLower(previousCharacter)
                || (char.IsUpper(previousCharacter) && nextCharacter is char lookaheadCharacter && char.IsLower(lookaheadCharacter)));
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

    private sealed record CsvFieldAliasDefinition(
        string DisplayAliasDefault,
        CsvExportAliasKind AliasKind,
        double Confidence,
        bool IsDirectAlias,
        bool IsDerivedAlias,
        string? Notes);

    private sealed record RawUnmappedRow(
        string NodeType,
        ushort MessageNumber,
        string MessageName,
        int RowSequence,
        ushort? MessageIndex,
        byte? LocalMessageNumber,
        DateTimeOffset? TimestampUtc,
        byte FieldNumber,
        string FieldName,
        int ValueIndex,
        int ValueCount,
        string RawValue,
        string DecodedValue,
        string? Unit,
        string? SourceArtifactName,
        FitExportFieldClassification Classification,
        string? Notes);

    private sealed record ConsolidatedAncillaryRow(
        string MessageName,
        ushort MessageNumber,
        int RowSequence,
        ushort? MessageIndex,
        byte? LocalMessageNumber,
        DateTimeOffset? TimestampUtc,
        byte FieldNumber,
        string FieldName,
        int ValueIndex,
        int ValueCount,
        string RawValue,
        string DecodedValue,
        string? Unit,
        FitExportFieldClassification Classification,
        string? Notes);

    private readonly record struct AncillaryFamilyKey(string MessageName, ushort MessageNumber);

    private enum DerivedSessionFieldKind
    {
        ActiveCalories = 0,
        EstimatedSweatLoss = 1,
        BeginningPotential = 2,
        EndingPotential = 3,
        MinimumStamina = 4,
        MovingTime = 5,
        AverageMovingSpeed = 6,
        MaxAveragePowerTwentyMinutes = 7
    }
}
