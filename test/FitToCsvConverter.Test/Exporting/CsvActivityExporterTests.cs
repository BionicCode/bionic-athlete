namespace FitToCsvConverter.Test.Exporting;

using System.Collections.Immutable;
using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using BionicCode.Utilities.Net;
using FitToCsvConverter.Data;
using FitToCsvConverter.Data.Activities;
using FitToCsvConverter.Data.Decoding;
using FitToCsvConverter.Data.Decoding.Garmin;
using FitToCsvConverter.Data.Exporting;
using FitToCsvConverter.Data.Fields;
using FitToCsvConverter.Test.Fixtures;

public sealed class CsvActivityExporterTests
{
    [Fact]
    public async Task ShouldWriteSeparateCsvFilesAndManifestWhenNodeRequestsAreSelected()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        FitActivity activity = FitActivityModelFactory.CreateActivityForExport();
        CsvActivityExporter exporter = new();
        string outputDirectoryPath = CreateTemporaryDirectory();

        try
        {
            CsvExportRequest request = CsvExportRequestFactory.Create(
                activity,
                "sample",
                outputDirectoryPath,
                [
                    CreateColumnRequest(activity.Fields.Single(), order: 0),
                    CreateColumnRequest(activity.Sessions[0].Fields.Single(), order: 0),
                    CreateColumnRequest(activity.Sessions[0].Laps[0].Fields.Single(), order: 0),
                    CreateColumnRequest(GetRecordField(activity, "heart_rate"), order: 0)
                ]);

            CsvExportResult result = await exporter.ExportAsync(request, cancellationToken);

            Assert.Collection(
                result.ExportedArtifacts,
                artifact =>
                {
                    Assert.Equal(ExportedArtifactKind.NodeCsv, artifact.Kind);
                    Assert.Equal(FitNodeType.Activity, artifact.NodeType);
                    Assert.EndsWith("_activity.csv", artifact.FilePath, StringComparison.OrdinalIgnoreCase);
                    Assert.Contains($"{Path.DirectorySeparatorChar}core{Path.DirectorySeparatorChar}", artifact.FilePath, StringComparison.OrdinalIgnoreCase);
                    Assert.EndsWith("_activity.csv", artifact.BundlePath, StringComparison.OrdinalIgnoreCase);
                    Assert.StartsWith("core/", artifact.BundlePath, StringComparison.OrdinalIgnoreCase);
                    Assert.Equal(1, artifact.RowCount);
                },
                artifact =>
                {
                    Assert.Equal(ExportedArtifactKind.NodeCsv, artifact.Kind);
                    Assert.Equal(FitNodeType.Session, artifact.NodeType);
                    Assert.EndsWith("_session.csv", artifact.FilePath, StringComparison.OrdinalIgnoreCase);
                    Assert.Contains($"{Path.DirectorySeparatorChar}core{Path.DirectorySeparatorChar}", artifact.FilePath, StringComparison.OrdinalIgnoreCase);
                    Assert.StartsWith("core/", artifact.BundlePath, StringComparison.OrdinalIgnoreCase);
                    Assert.Equal(1, artifact.RowCount);
                },
                artifact =>
                {
                    Assert.Equal(ExportedArtifactKind.NodeCsv, artifact.Kind);
                    Assert.Equal(FitNodeType.Lap, artifact.NodeType);
                    Assert.EndsWith("_lap.csv", artifact.FilePath, StringComparison.OrdinalIgnoreCase);
                    Assert.Contains($"{Path.DirectorySeparatorChar}core{Path.DirectorySeparatorChar}", artifact.FilePath, StringComparison.OrdinalIgnoreCase);
                    Assert.StartsWith("core/", artifact.BundlePath, StringComparison.OrdinalIgnoreCase);
                    Assert.Equal(1, artifact.RowCount);
                },
                artifact =>
                {
                    Assert.Equal(ExportedArtifactKind.NodeCsv, artifact.Kind);
                    Assert.Equal(FitNodeType.Record, artifact.NodeType);
                    Assert.EndsWith("_record.csv", artifact.FilePath, StringComparison.OrdinalIgnoreCase);
                    Assert.Contains($"{Path.DirectorySeparatorChar}core{Path.DirectorySeparatorChar}", artifact.FilePath, StringComparison.OrdinalIgnoreCase);
                    Assert.StartsWith("core/", artifact.BundlePath, StringComparison.OrdinalIgnoreCase);
                    Assert.Equal(2, artifact.RowCount);
                },
                artifact =>
                {
                    Assert.Equal(ExportedArtifactKind.Manifest, artifact.Kind);
                    Assert.Equal("manifest", artifact.ArtifactName);
                    Assert.EndsWith("_manifest.json", artifact.FilePath, StringComparison.OrdinalIgnoreCase);
                    Assert.EndsWith("_manifest.json", artifact.BundlePath, StringComparison.OrdinalIgnoreCase);
                    Assert.Equal(1, artifact.RowCount);
                });
        }
        finally
        {
            DeleteTemporaryDirectory(outputDirectoryPath);
        }
    }

    [Fact]
    public async Task ShouldWriteOverriddenColumnNamesWhenFieldStateWasRenamed()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        FitActivity activity = FitActivityModelFactory.CreateActivityForExport();
        FitField field = GetRecordField(activity, "heart_rate");
        field.SetColumnName("Heart Rate (BPM)");
        CsvActivityExporter exporter = new();
        string outputDirectoryPath = CreateTemporaryDirectory();

        try
        {
            CsvExportRequest request = CsvExportRequestFactory.Create(
                activity,
                "sample",
                outputDirectoryPath,
                [CreateColumnRequest(field, order: 0)]);

            CsvExportResult result = await exporter.ExportAsync(request, cancellationToken);
            string[] lines = await ReadAllLinesAsync(result, ExportedArtifactKind.NodeCsv, FitNodeType.Record, cancellationToken);

            Assert.Equal("Heart Rate (BPM)", SplitCsvLine(lines[0])[0]);
        }
        finally
        {
            DeleteTemporaryDirectory(outputDirectoryPath);
        }
    }

    [Fact]
    public async Task ShouldWriteArrayValuesIntoSingleCellWhenFieldHasMultipleDecodedValues()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        FitActivity activity = FitActivityModelFactory.CreateActivityForExport();
        FitField field = GetRecordField(activity, "power_zones");
        CsvActivityExporter exporter = new();
        string outputDirectoryPath = CreateTemporaryDirectory();

        try
        {
            CsvExportRequest request = CsvExportRequestFactory.Create(
                activity,
                "sample",
                outputDirectoryPath,
                [CreateColumnRequest(field, order: 0)]);

            CsvExportResult result = await exporter.ExportAsync(request, cancellationToken);
            string[] lines = await ReadAllLinesAsync(result, ExportedArtifactKind.NodeCsv, FitNodeType.Record, cancellationToken);

            Assert.Equal("1 | 2 | 3", SplitCsvLine(lines[1])[0]);
        }
        finally
        {
            DeleteTemporaryDirectory(outputDirectoryPath);
        }
    }

    [Fact]
    public async Task ShouldWriteEditedDecodedValuesWhenFieldHasEditedValues()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        FitActivity activity = FitActivityModelFactory.CreateActivityForExport();
        FitField firstRecordField = GetRecordField(activity, "heart_rate");
        firstRecordField.SetEditedDecodedValues([999]);
        CsvActivityExporter exporter = new();
        string outputDirectoryPath = CreateTemporaryDirectory();

        try
        {
            CsvExportRequest request = CsvExportRequestFactory.Create(
                activity,
                "sample",
                outputDirectoryPath,
                [CreateColumnRequest(firstRecordField, order: 0)]);

            CsvExportResult result = await exporter.ExportAsync(request, cancellationToken);
            string[] lines = await ReadAllLinesAsync(result, ExportedArtifactKind.NodeCsv, FitNodeType.Record, cancellationToken);

            Assert.Equal("999", SplitCsvLine(lines[1])[0]);
        }
        finally
        {
            DeleteTemporaryDirectory(outputDirectoryPath);
        }
    }

    [Fact]
    public async Task ShouldWriteNonNumericTextValuesWhenStructuredCsvIsRequested()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        FitActivity activity = FitActivityModelFactory.CreateActivityForExport();
        FitField field = GetRecordField(activity, "heart_rate");
        field.SetEditedDecodedValues(["Manual"]);
        CsvActivityExporter exporter = new();
        string outputDirectoryPath = CreateTemporaryDirectory();

        try
        {
            CsvExportRequest request = CsvExportRequestFactory.Create(
                activity,
                "sample",
                outputDirectoryPath,
                [CreateColumnRequest(field, order: 0)]);

            CsvExportResult result = await exporter.ExportAsync(request, cancellationToken);
            string[] lines = await ReadAllLinesAsync(result, ExportedArtifactKind.NodeCsv, FitNodeType.Record, cancellationToken);

            Assert.Equal("Manual", SplitCsvLine(lines[1])[0]);
        }
        finally
        {
            DeleteTemporaryDirectory(outputDirectoryPath);
        }
    }

    [Fact]
    public async Task ShouldNormalizeAverageSpeedToKilometersPerHourWhenMetricStructuredCsvIsRequested()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        FitActivity activity = FitActivityModelFactory.CreateActivityForStructuredCsvExport();
        FitField field = GetSessionField(activity, "enhanced_avg_speed");
        CsvActivityExporter exporter = new();
        string outputDirectoryPath = CreateTemporaryDirectory();

        try
        {
            CsvExportRequest request = CsvExportRequestFactory.Create(
                activity,
                "sample",
                outputDirectoryPath,
                [CreateColumnRequest(field, order: 0)],
                options: new FitExportOptions(unitSystem: FitExportUnitSystem.Metric));

            CsvExportResult result = await exporter.ExportAsync(request, cancellationToken);
            string[] lines = await ReadAllLinesAsync(result, ExportedArtifactKind.NodeCsv, FitNodeType.Session, cancellationToken);
            string[] headerCells = SplitCsvLine(lines[0]);
            string[] valueCells = SplitCsvLine(lines[1]);

            Assert.Equal("enhanced_avg_speed [km/h]", headerCells[0]);
            Assert.Equal("18.3888", valueCells[0]);
        }
        finally
        {
            DeleteTemporaryDirectory(outputDirectoryPath);
        }
    }

    [Fact]
    public async Task ShouldNormalizeDistanceToKilometersWhenMetricStructuredCsvIsRequested()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        FitActivity activity = FitActivityModelFactory.CreateActivityForStructuredCsvExport();
        FitField field = GetSessionField(activity, "total_distance");
        CsvActivityExporter exporter = new();
        string outputDirectoryPath = CreateTemporaryDirectory();

        try
        {
            CsvExportRequest request = CsvExportRequestFactory.Create(
                activity,
                "sample",
                outputDirectoryPath,
                [CreateColumnRequest(field, order: 0)],
                options: new FitExportOptions(unitSystem: FitExportUnitSystem.Metric));

            CsvExportResult result = await exporter.ExportAsync(request, cancellationToken);
            string[] lines = await ReadAllLinesAsync(result, ExportedArtifactKind.NodeCsv, FitNodeType.Session, cancellationToken);
            string[] headerCells = SplitCsvLine(lines[0]);
            string[] valueCells = SplitCsvLine(lines[1]);

            Assert.Equal("total_distance [km]", headerCells[0]);
            Assert.Equal("6.14749", valueCells[0]);
        }
        finally
        {
            DeleteTemporaryDirectory(outputDirectoryPath);
        }
    }

    [Fact]
    public async Task ShouldNormalizeDurationsToSecondsWhenStructuredCsvIsRequested()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        FitActivity activity = FitActivityModelFactory.CreateActivityForStructuredCsvExport();
        FitField field = GetSessionField(activity, "total_elapsed_time");
        CsvActivityExporter exporter = new();
        string outputDirectoryPath = CreateTemporaryDirectory();

        try
        {
            CsvExportRequest request = CsvExportRequestFactory.Create(
                activity,
                "sample",
                outputDirectoryPath,
                [CreateColumnRequest(field, order: 0)]);

            CsvExportResult result = await exporter.ExportAsync(request, cancellationToken);
            string[] lines = await ReadAllLinesAsync(result, ExportedArtifactKind.NodeCsv, FitNodeType.Session, cancellationToken);
            string[] headerCells = SplitCsvLine(lines[0]);
            string[] valueCells = SplitCsvLine(lines[1]);

            Assert.Equal("total_elapsed_time [s]", headerCells[0]);
            Assert.Equal("1247.782", valueCells[0]);
        }
        finally
        {
            DeleteTemporaryDirectory(outputDirectoryPath);
        }
    }

    [Fact]
    public async Task ShouldPreserveDistinctDurationSemanticsWhenElapsedAndTimerTimeAreSelected()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        FitActivity activity = FitActivityModelFactory.CreateActivityForStructuredCsvExport();
        FitField elapsedTimeField = GetSessionField(activity, "total_elapsed_time");
        FitField timerTimeField = GetSessionField(activity, "total_timer_time");
        CsvActivityExporter exporter = new();
        string outputDirectoryPath = CreateTemporaryDirectory();

        try
        {
            CsvExportRequest request = CsvExportRequestFactory.Create(
                activity,
                "sample",
                outputDirectoryPath,
                [
                    CreateColumnRequest(elapsedTimeField, order: 0),
                    CreateColumnRequest(timerTimeField, order: 1)
                ]);

            CsvExportResult result = await exporter.ExportAsync(request, cancellationToken);
            string[] lines = await ReadAllLinesAsync(result, ExportedArtifactKind.NodeCsv, FitNodeType.Session, cancellationToken);
            string[] headerCells = SplitCsvLine(lines[0]);
            string[] valueCells = SplitCsvLine(lines[1]);

            Assert.Equal("total_elapsed_time [s]", headerCells[0]);
            Assert.Equal("total_timer_time [s]", headerCells[1]);
            Assert.Equal("1247.782", valueCells[0]);
            Assert.Equal("1203.591", valueCells[1]);
        }
        finally
        {
            DeleteTemporaryDirectory(outputDirectoryPath);
        }
    }

    [Fact]
    public async Task ShouldWriteBlankCellWhenFieldUsesFitInvalidSentinel()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        FitActivity activity = FitActivityModelFactory.CreateActivityForStructuredCsvExport();
        FitField field = GetSessionField(activity, "avg_power_position");
        CsvActivityExporter exporter = new();
        string outputDirectoryPath = CreateTemporaryDirectory();

        try
        {
            CsvExportRequest request = CsvExportRequestFactory.Create(
                activity,
                "sample",
                outputDirectoryPath,
                [CreateColumnRequest(field, order: 0)]);

            CsvExportResult result = await exporter.ExportAsync(request, cancellationToken);
            string[] lines = await ReadAllLinesAsync(result, ExportedArtifactKind.NodeCsv, FitNodeType.Session, cancellationToken);
            string[] headerCells = SplitCsvLine(lines[0]);
            string[] valueCells = SplitCsvLine(lines[1]);

            Assert.Equal("avg_power_position [watts]", headerCells[0]);
            Assert.Equal(string.Empty, valueCells[0]);
        }
        finally
        {
            DeleteTemporaryDirectory(outputDirectoryPath);
        }
    }

    [Fact]
    public async Task ShouldKeepOneUnitPerColumnWhenMultipleRecordRowsAreExported()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        FitActivity activity = FitActivityModelFactory.CreateActivityForStructuredCsvExport();
        FitField field = GetRecordField(activity, "enhanced_speed");
        CsvActivityExporter exporter = new();
        string outputDirectoryPath = CreateTemporaryDirectory();

        try
        {
            CsvExportRequest request = CsvExportRequestFactory.Create(
                activity,
                "sample",
                outputDirectoryPath,
                [CreateColumnRequest(field, order: 0)]);

            CsvExportResult result = await exporter.ExportAsync(request, cancellationToken);
            string[] lines = await ReadAllLinesAsync(result, ExportedArtifactKind.NodeCsv, FitNodeType.Record, cancellationToken);
            string[] headerCells = SplitCsvLine(lines[0]);

            Assert.Equal("enhanced_speed [km/h]", headerCells[0]);
            Assert.Equal("18", SplitCsvLine(lines[1])[0]);
            Assert.Equal("21.6", SplitCsvLine(lines[2])[0]);
        }
        finally
        {
            DeleteTemporaryDirectory(outputDirectoryPath);
        }
    }

    [Fact]
    public async Task ShouldWriteUtcAndLocalTimestampColumnsWhenLocalTimestampDuplicationIsEnabled()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        FitActivity activity = FitActivityModelFactory.CreateActivityForStructuredCsvExport();
        FitField field = GetRecordField(activity, "timestamp");
        CsvActivityExporter exporter = new();
        string outputDirectoryPath = CreateTemporaryDirectory();
        TimeZoneInfo localTimeZone = TimeZoneInfo.CreateCustomTimeZone("UTC+02", TimeSpan.FromHours(2), "UTC+02", "UTC+02");

        try
        {
            CsvExportRequest request = CsvExportRequestFactory.Create(
                activity,
                "sample",
                outputDirectoryPath,
                [CreateColumnRequest(field, order: 0)],
                options: new FitExportOptions(includeLocalTimeColumns: true, localTimeZone: localTimeZone));

            CsvExportResult result = await exporter.ExportAsync(request, cancellationToken);
            string[] lines = await ReadAllLinesAsync(result, ExportedArtifactKind.NodeCsv, FitNodeType.Record, cancellationToken);

            Assert.Equal("timestamp [UTC],timestamp [Local]", lines[0]);
            Assert.Equal(
                $"{new DateTimeOffset(2024, 07, 14, 08, 35, 00, TimeSpan.Zero):O},{new DateTimeOffset(2024, 07, 14, 10, 35, 00, TimeSpan.FromHours(2)):O}",
                lines[1]);
        }
        finally
        {
            DeleteTemporaryDirectory(outputDirectoryPath);
        }
    }

    [Fact]
    public async Task ShouldWriteDerivedSessionColumnsWhenStructuredSummaryValuesAreDerivable()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        FitActivity activity = FitActivityModelFactory.CreateActivityForDerivedSessionExport();
        CsvActivityExporter exporter = new();
        string outputDirectoryPath = CreateTemporaryDirectory();

        try
        {
            CsvExportRequest request = CsvExportRequestFactory.Create(
                activity,
                "derived",
                outputDirectoryPath,
                [CreateColumnRequest(GetSessionField(activity, "total_distance"), order: 0)]);

            CsvExportResult result = await exporter.ExportAsync(request, cancellationToken);
            string[] lines = await ReadAllLinesAsync(result, ExportedArtifactKind.NodeCsv, FitNodeType.Session, cancellationToken);
            string[] headerCells = SplitCsvLine(lines[0]);
            string[] valueCells = SplitCsvLine(lines[1]);

            Assert.Equal(
                [
                    "total_distance [km]",
                    "active_calories [kcal]",
                    "moving_time [s]",
                    "avg_moving_speed [km/h]",
                    "max_avg_power_20min [watts]"
                ],
                headerCells);
            Assert.Equal(["10", "380", "1800", "20", "250"], valueCells);
        }
        finally
        {
            DeleteTemporaryDirectory(outputDirectoryPath);
        }
    }

    [Fact]
    public async Task ShouldPreferDirectMovingTimeWhenStructuredSummaryContainsTotalMovingTime()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        FitActivity activity = FitActivityModelFactory.CreateActivityForDirectMovingTimeExport();
        CsvActivityExporter exporter = new();
        string outputDirectoryPath = CreateTemporaryDirectory();

        try
        {
            CsvExportRequest request = CsvExportRequestFactory.Create(
                activity,
                "direct-moving",
                outputDirectoryPath,
                [CreateColumnRequest(GetSessionField(activity, "total_distance"), order: 0)]);

            CsvExportResult result = await exporter.ExportAsync(request, cancellationToken);
            string[] lines = await ReadAllLinesAsync(result, ExportedArtifactKind.NodeCsv, FitNodeType.Session, cancellationToken);
            string[] valueCells = SplitCsvLine(lines[1]);

            Assert.Equal("1500", valueCells[2]);
            Assert.Equal("24", valueCells[3]);
        }
        finally
        {
            DeleteTemporaryDirectory(outputDirectoryPath);
        }
    }

    [Fact]
    public async Task ShouldExportRawCanonicalAncillaryArtifactsWhenRawCanonicalViewIsRequested()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        FitActivity activity = FitActivityModelFactory.CreateActivityWithUnknownAncillaryDataForExport();
        CsvActivityExporter exporter = new();
        string outputDirectoryPath = CreateTemporaryDirectory();

        try
        {
            CsvExportRequest request = CsvExportRequestFactory.Create(
                activity,
                "ancillary",
                outputDirectoryPath,
                [CreateColumnRequest(activity.Fields.Single(), order: 0)],
                options: new FitExportOptions(dataView: FitExportDataView.RawCanonical));

            CsvExportResult result = await exporter.ExportAsync(request, cancellationToken);

            ExportedArtifact unknownArtifact = Assert.Single(
                result.ExportedArtifacts.Where(artifact =>
                    artifact.Kind == ExportedArtifactKind.AncillaryCsv
                    && artifact.ArtifactName.Contains("unknown_250", StringComparison.OrdinalIgnoreCase)));
            string[] unknownLines = await File.ReadAllLinesAsync(unknownArtifact.FilePath, cancellationToken);

            Assert.Contains(result.ExportedArtifacts, artifact =>
                artifact.Kind == ExportedArtifactKind.AncillaryCsv
                && artifact.ArtifactName.Contains("file_id_0", StringComparison.OrdinalIgnoreCase));
            Assert.Contains($"{Path.DirectorySeparatorChar}raw_lossless{Path.DirectorySeparatorChar}", unknownArtifact.FilePath, StringComparison.OrdinalIgnoreCase);
            Assert.StartsWith("raw_lossless/", unknownArtifact.BundlePath, StringComparison.OrdinalIgnoreCase);
            Assert.Equal("message_name,message_number,sequence_number,message_index,local_message_number,message_timestamp [UTC],unknown_0", unknownLines[0]);
            Assert.Equal("unknown,250,1,,0,2024-07-14T08:30:01.0000000+00:00,98", unknownLines[1]);
        }
        finally
        {
            DeleteTemporaryDirectory(outputDirectoryPath);
        }
    }

    [Fact]
    public async Task ShouldDefaultToStructuredMachineViewAndConsolidateUnknownAncillaryData()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        FitActivity activity = FitActivityModelFactory.CreateActivityWithUnknownAncillaryDataForExport();
        CsvActivityExporter exporter = new();
        string outputDirectoryPath = CreateTemporaryDirectory();

        try
        {
            CsvExportRequest request = CsvExportRequestFactory.Create(
                activity,
                "ancillary-default",
                outputDirectoryPath,
                [CreateColumnRequest(activity.Fields.Single(), order: 0)]);

            CsvExportResult result = await exporter.ExportAsync(request, cancellationToken);

            Assert.DoesNotContain(result.ExportedArtifacts, artifact => artifact.Kind == ExportedArtifactKind.AncillaryCsv);
            Assert.DoesNotContain(result.ExportedArtifacts, artifact => artifact.BundlePath.StartsWith("raw_lossless/", StringComparison.OrdinalIgnoreCase));

            ExportedArtifact metadataArtifact = Assert.Single(result.ExportedArtifacts.Where(artifact =>
                artifact.Kind == ExportedArtifactKind.ConsolidatedCsv
                && artifact.BundlePath.StartsWith("metadata/", StringComparison.OrdinalIgnoreCase)));
            ExportedArtifact rawUnmappedArtifact = Assert.Single(result.ExportedArtifacts.Where(artifact =>
                artifact.Kind == ExportedArtifactKind.ConsolidatedCsv
                && artifact.BundlePath.StartsWith("raw_unmapped/", StringComparison.OrdinalIgnoreCase)));
            string[] metadataLines = await File.ReadAllLinesAsync(metadataArtifact.FilePath, cancellationToken);
            string[] rawUnmappedLines = await File.ReadAllLinesAsync(rawUnmappedArtifact.FilePath, cancellationToken);

            Assert.Contains(metadataLines, line => line.Contains("file_id", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(rawUnmappedLines, line => line.Contains("unknown_0", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            DeleteTemporaryDirectory(outputDirectoryPath);
        }
    }

    [Fact]
    public async Task ShouldGenerateStableManifestMetadataWhenStructuredExportCompletes()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        FitActivity activity = FitActivityModelFactory.CreateActivityForDirectMovingTimeExport();
        CsvActivityExporter exporter = new();
        string outputDirectoryPath = CreateTemporaryDirectory();

        try
        {
            CsvExportRequest request = CsvExportRequestFactory.Create(
                activity,
                "manifest",
                outputDirectoryPath,
                [CreateColumnRequest(GetSessionField(activity, "total_distance"), order: 0)]);

            CsvExportResult result = await exporter.ExportAsync(request, cancellationToken);
            using JsonDocument manifest = await ReadManifestAsync(result, cancellationToken);
            JsonElement root = manifest.RootElement;
            JsonElement fieldDictionary = root.GetProperty("fieldDictionary");

            Assert.Equal("2.0.0", root.GetProperty("exportSchemaVersion").GetString());
            Assert.Equal("UTC ISO-8601", root.GetProperty("timezoneSemantics").GetProperty("canonicalTimestampColumns").GetString());
            Assert.Equal("Exercise Load", FindFieldDictionaryEntry(fieldDictionary, "training_load_peak").GetProperty("alias").GetString());
            Assert.Equal("Total Strokes", FindFieldDictionaryEntry(fieldDictionary, "total_cycles").GetProperty("alias").GetString());
        }
        finally
        {
            DeleteTemporaryDirectory(outputDirectoryPath);
        }
    }

    [Fact]
    public async Task ShouldDeduplicateManifestFieldDictionaryEntriesWhenRepeatedRecordFieldsExist()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        FitActivity activity = FitActivityModelFactory.CreateActivityForExport();
        CsvActivityExporter exporter = new();
        string outputDirectoryPath = CreateTemporaryDirectory();

        try
        {
            CsvExportRequest request = CsvExportRequestFactory.Create(
                activity,
                "dedupe",
                outputDirectoryPath,
                CreateAllColumnRequests(activity));

            CsvExportResult result = await exporter.ExportAsync(request, cancellationToken);
            using JsonDocument manifest = await ReadManifestAsync(result, cancellationToken);
            JsonElement fieldDictionary = manifest.RootElement.GetProperty("fieldDictionary");

            int heartRateEntryCount = fieldDictionary.EnumerateArray().Count(entry =>
                entry.GetProperty("sourceMessageFamily").GetString() == "record"
                && entry.GetProperty("sourceFieldName").GetString() == "heart_rate");

            Assert.Equal(1, heartRateEntryCount);
        }
        finally
        {
            DeleteTemporaryDirectory(outputDirectoryPath);
        }
    }

    [Fact]
    public async Task ShouldMarkUnknownRawCoverageInManifestWhenUnknownAncillaryMessagesArePresent()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        FitActivity activity = FitActivityModelFactory.CreateActivityWithUnknownAncillaryDataForExport();
        CsvActivityExporter exporter = new();
        string outputDirectoryPath = CreateTemporaryDirectory();

        try
        {
            CsvExportRequest request = CsvExportRequestFactory.Create(
                activity,
                "manifest-unknown",
                outputDirectoryPath,
                [CreateColumnRequest(activity.Fields.Single(), order: 0)]);

            CsvExportResult result = await exporter.ExportAsync(request, cancellationToken);
            using JsonDocument manifest = await ReadManifestAsync(result, cancellationToken);
            JsonElement root = manifest.RootElement;
            JsonElement unknownEntry = FindFieldDictionaryEntry(root.GetProperty("fieldDictionary"), "unknown_0");

            Assert.True(root.GetProperty("hasUnknownOrVendorFields").GetBoolean());
            Assert.Equal("UnknownMessageFamily", unknownEntry.GetProperty("classification").GetString());
            Assert.Contains(
                "unknown FIT message family",
                unknownEntry.GetProperty("notes").GetString(),
                StringComparison.Ordinal);
        }
        finally
        {
            DeleteTemporaryDirectory(outputDirectoryPath);
        }
    }

    [Fact]
    public async Task ShouldExportAllDecodedMessageFamiliesWhenExampleFitFileIsExported()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        GarminFitActivityDecoder decoder = new();
        CsvActivityExporter exporter = new();
        string outputDirectoryPath = CreateTemporaryDirectory();

        try
        {
            FitActivityDecodeResult decodeResult = await decoder.DecodeFileAsync(FitTestFileFactory.GetExampleFitFilePath(), cancellationToken);
            Assert.True(decodeResult.IsSuccess);
            FitActivity activity = Assert.IsType<FitActivity>(decodeResult.Activity);

            CsvExportRequest request = CsvExportRequestFactory.Create(
                activity,
                "example",
                outputDirectoryPath,
                CreateAllColumnRequests(activity));

            CsvExportResult result = await exporter.ExportAsync(request, cancellationToken);
            using JsonDocument manifest = await ReadManifestAsync(result, cancellationToken);
            JsonElement root = manifest.RootElement;

            Assert.NotEmpty(activity.AncillaryData.Messages);
            Assert.DoesNotContain(result.ExportedArtifacts, artifact => artifact.Kind == ExportedArtifactKind.AncillaryCsv);
            Assert.Contains(result.ExportedArtifacts, artifact =>
                artifact.Kind == ExportedArtifactKind.ConsolidatedCsv
                && artifact.BundlePath.StartsWith("metadata/", StringComparison.OrdinalIgnoreCase));
            Assert.Empty(root.GetProperty("omittedMessageFamilies").EnumerateArray());
        }
        finally
        {
            DeleteTemporaryDirectory(outputDirectoryPath);
        }
    }

    [Fact]
    public async Task ShouldWriteGroupedMachineArtifactsAndRawUnmappedCoverageWhenReferenceActivityIsExported()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        GarminFitActivityDecoder decoder = new();
        CsvActivityExporter exporter = new();
        string outputDirectoryPath = CreateTemporaryDirectory();

        try
        {
            FitActivityDecodeResult decodeResult = await decoder.DecodeFileAsync(FitTestFileFactory.GetReferenceArtifactFitFilePath(), cancellationToken);
            Assert.True(decodeResult.IsSuccess);
            FitActivity activity = Assert.IsType<FitActivity>(decodeResult.Activity);

            CsvExportRequest request = CsvExportRequestFactory.Create(
                activity,
                "reference",
                outputDirectoryPath,
                CreateAllColumnRequests(activity));

            CsvExportResult result = await exporter.ExportAsync(request, cancellationToken);

            Assert.DoesNotContain(result.ExportedArtifacts, artifact => artifact.Kind == ExportedArtifactKind.AncillaryCsv);
            Assert.DoesNotContain(result.ExportedArtifacts, artifact => artifact.BundlePath.StartsWith("raw_lossless/", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(result.ExportedArtifacts, artifact =>
                artifact.Kind == ExportedArtifactKind.ConsolidatedCsv
                && artifact.BundlePath.StartsWith("metadata/", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(result.ExportedArtifacts, artifact =>
                artifact.Kind == ExportedArtifactKind.ConsolidatedCsv
                && artifact.BundlePath.StartsWith("analytics/", StringComparison.OrdinalIgnoreCase));

            ExportedArtifact rawUnmappedArtifact = Assert.Single(result.ExportedArtifacts.Where(artifact =>
                artifact.Kind == ExportedArtifactKind.ConsolidatedCsv
                && artifact.BundlePath.StartsWith("raw_unmapped/", StringComparison.OrdinalIgnoreCase)));
            string[] rawUnmappedLines = await File.ReadAllLinesAsync(rawUnmappedArtifact.FilePath, cancellationToken);

            Assert.Contains(rawUnmappedLines, line => line.Contains(",unknown_178,", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(rawUnmappedLines, line => line.Contains(",unknown_205,", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(rawUnmappedLines, line => line.Contains(",unknown_206,", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(rawUnmappedLines, line => line.Contains(",unknown_207,", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            DeleteTemporaryDirectory(outputDirectoryPath);
        }
    }

    [Fact]
    public async Task ShouldExportCleanedReferenceSessionValuesAndRestoredDerivedFields()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        GarminFitActivityDecoder decoder = new();
        CsvActivityExporter exporter = new();
        string outputDirectoryPath = CreateTemporaryDirectory();

        try
        {
            FitActivityDecodeResult decodeResult = await decoder.DecodeFileAsync(FitTestFileFactory.GetReferenceArtifactFitFilePath(), cancellationToken);
            Assert.True(decodeResult.IsSuccess);
            FitActivity activity = Assert.IsType<FitActivity>(decodeResult.Activity);

            CsvExportRequest request = CsvExportRequestFactory.Create(
                activity,
                "reference",
                outputDirectoryPath,
                CreateAllColumnRequests(activity));

            CsvExportResult result = await exporter.ExportAsync(request, cancellationToken);
            string[] lines = await ReadAllLinesAsync(result, ExportedArtifactKind.NodeCsv, FitNodeType.Session, cancellationToken);
            string[] headerCells = SplitCsvLine(lines[0]);
            string[] valueCells = SplitCsvLine(lines[1]);

            Assert.Equal("INDOOR", GetColumnValue(headerCells, valueCells, "sport_profile_name"));
            Assert.DoesNotContain('|', GetColumnValue(headerCells, valueCells, "sport_profile_name"));
            Assert.Equal("77", GetColumnValue(headerCells, valueCells, "est_sweat_loss [ml]"));
            Assert.Equal("100", GetColumnValue(headerCells, valueCells, "beginning_potential [%]"));
            Assert.Equal("92", GetColumnValue(headerCells, valueCells, "ending_potential [%]"));
            Assert.Equal("92", GetColumnValue(headerCells, valueCells, "min_stamina [%]"));

            double averageMovingSpeed = double.Parse(GetColumnValue(headerCells, valueCells, "avg_moving_speed [km/h]"), CultureInfo.InvariantCulture);
            double maxAveragePowerTwentyMinutes = double.Parse(GetColumnValue(headerCells, valueCells, "max_avg_power_20min [watts]"), CultureInfo.InvariantCulture);

            Assert.InRange(averageMovingSpeed, 18.5d, 18.6d);
            Assert.InRange(maxAveragePowerTwentyMinutes, 72.1d, 72.3d);
        }
        finally
        {
            DeleteTemporaryDirectory(outputDirectoryPath);
        }
    }

    [Fact]
    public async Task ShouldDescribeAliasAndGroupingMetadataInManifestWhenReferenceActivityIsExported()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        GarminFitActivityDecoder decoder = new();
        CsvActivityExporter exporter = new();
        string outputDirectoryPath = CreateTemporaryDirectory();

        try
        {
            FitActivityDecodeResult decodeResult = await decoder.DecodeFileAsync(FitTestFileFactory.GetReferenceArtifactFitFilePath(), cancellationToken);
            Assert.True(decodeResult.IsSuccess);
            FitActivity activity = Assert.IsType<FitActivity>(decodeResult.Activity);

            CsvExportRequest request = CsvExportRequestFactory.Create(
                activity,
                "reference",
                outputDirectoryPath,
                CreateAllColumnRequests(activity));

            CsvExportResult result = await exporter.ExportAsync(request, cancellationToken);
            using JsonDocument manifest = await ReadManifestAsync(result, cancellationToken);
            JsonElement root = manifest.RootElement;
            JsonElement fieldDictionary = root.GetProperty("fieldDictionary");
            JsonElement artifacts = root.GetProperty("artifacts");

            JsonElement totalWorkEntry = FindFieldDictionaryEntryByCanonicalName(fieldDictionary, "session.total_work");
            JsonElement restoredSweatLossEntry = FindFieldDictionaryEntryByCanonicalName(fieldDictionary, "session.est_sweat_loss");

            Assert.Contains(artifacts.EnumerateArray(), artifact =>
                artifact.GetProperty("artifactGroup").GetString() == "Metadata");
            Assert.Contains(artifacts.EnumerateArray(), artifact =>
                artifact.GetProperty("artifactGroup").GetString() == "Analytics");
            Assert.Contains(artifacts.EnumerateArray(), artifact =>
                artifact.GetProperty("artifactGroup").GetString() == "RawUnmapped");
            Assert.All(artifacts.EnumerateArray(), artifact =>
                Assert.True(
                    artifact.GetProperty("dataView").GetString() is "StructuredMachineView" or "Manifest",
                    "Default structured exports should describe only View B artifacts plus the manifest."));

            Assert.Equal("kJ", totalWorkEntry.GetProperty("unit").GetString());
            Assert.Equal("Work", totalWorkEntry.GetProperty("aliasMetadata").GetProperty("displayAliasDefault").GetString());
            Assert.Equal("GarminConnectPdf", totalWorkEntry.GetProperty("aliasMetadata").GetProperty("displayAliasSource").GetString());
            Assert.Equal("DirectFieldAlias", totalWorkEntry.GetProperty("aliasMetadata").GetProperty("aliasKind").GetString());
            Assert.Equal("DerivedFromRestoredFitMessages", restoredSweatLossEntry.GetProperty("classification").GetString());
        }
        finally
        {
            DeleteTemporaryDirectory(outputDirectoryPath);
        }
    }

    [Fact]
    public async Task ShouldWriteManifestArtifactPathsThatMatchGeneratedBundlePaths()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        GarminFitActivityDecoder decoder = new();
        CsvActivityExporter exporter = new();
        string outputDirectoryPath = CreateTemporaryDirectory();

        try
        {
            FitActivityDecodeResult decodeResult = await decoder.DecodeFileAsync(FitTestFileFactory.GetReferenceArtifactFitFilePath(), cancellationToken);
            Assert.True(decodeResult.IsSuccess);
            FitActivity activity = Assert.IsType<FitActivity>(decodeResult.Activity);

            CsvExportRequest request = CsvExportRequestFactory.Create(
                activity,
                "reference",
                outputDirectoryPath,
                CreateAllColumnRequests(activity));

            CsvExportResult result = await exporter.ExportAsync(request, cancellationToken);
            using JsonDocument manifest = await ReadManifestAsync(result, cancellationToken);

            string[] generatedBundlePaths = result.ExportedArtifacts
                .Select(static artifact => artifact.BundlePath)
                .OrderBy(static bundlePath => bundlePath, StringComparer.OrdinalIgnoreCase)
                .ToArray();
            string[] manifestArtifactPaths = manifest.RootElement
                .GetProperty("artifacts")
                .EnumerateArray()
                .Select(static artifact => artifact.GetProperty("artifactFileName").GetString() ?? string.Empty)
                .OrderBy(static artifactFileName => artifactFileName, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            Assert.Equal(generatedBundlePaths, manifestArtifactPaths);
        }
        finally
        {
            DeleteTemporaryDirectory(outputDirectoryPath);
        }
    }

    [Fact]
    public async Task ShouldUseArchiveEntryNamesWhenCreatingZipBundles()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string outputDirectoryPath = CreateTemporaryDirectory();

        try
        {
            string coreFilePath = Path.Combine(outputDirectoryPath, "activity.csv");
            string metadataFilePath = Path.Combine(outputDirectoryPath, "metadata.csv");
            await File.WriteAllTextAsync(coreFilePath, "activity", cancellationToken);
            await File.WriteAllTextAsync(metadataFilePath, "metadata", cancellationToken);

            FileDescriptor[] fileDescriptors =
            [
                new(coreFilePath, isRenamingRequired: false, "core/activity.csv"),
                new(metadataFilePath, isRenamingRequired: false, "metadata/metadata.csv")
            ];
            FileBatch fileBatch = new(fileDescriptors, fileDescriptors.Length, outputDirectoryPath, "bundle", Encoding.UTF8, CompressionLevel.Fastest);
            FileBatches fileBatches = new([fileBatch], batchesCount: 1);
            ZipArchiveManager archiveManager = new(new TestTemporaryFileManager(outputDirectoryPath));

            await archiveManager.CreateArchivesAsync(fileBatches, new Progress<ProgressData>(), cancellationToken);

            using ZipArchive zipArchive = ZipFile.OpenRead(Path.Combine(outputDirectoryPath, "bundle.zip"));
            string[] entryNames = zipArchive.Entries.Select(static entry => entry.FullName).Order(StringComparer.OrdinalIgnoreCase).ToArray();

            Assert.Equal(["core/activity.csv", "metadata/metadata.csv"], entryNames);
        }
        finally
        {
            DeleteTemporaryDirectory(outputDirectoryPath);
        }
    }

    [Fact]
    public async Task ShouldReportProfileCoverageForDocumentedStandardAndDeveloperFields()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        GarminFitActivityDecoder decoder = new();
        CsvActivityExporter exporter = new();
        string outputDirectoryPath = CreateTemporaryDirectory();

        try
        {
            await using MemoryStream fitStream = new(FitTestFileFactory.CreateSingleSessionActivityWithDeveloperFields());
            FitActivityDecodeResult decodeResult = await decoder.DecodeAsync(fitStream, "developer-fields.fit", cancellationToken);
            Assert.True(decodeResult.IsSuccess);
            FitActivity activity = Assert.IsType<FitActivity>(decodeResult.Activity);

            CsvExportRequest request = CsvExportRequestFactory.Create(
                activity,
                "developer-fields",
                outputDirectoryPath,
                CreateAllColumnRequests(activity));

            CsvExportResult result = await exporter.ExportAsync(request, cancellationToken);
            using JsonDocument manifest = await ReadManifestAsync(result, cancellationToken);
            JsonElement profileCoverage = manifest.RootElement.GetProperty("profileCoverage");
            JsonElement entries = profileCoverage.GetProperty("entries");

            Assert.True(profileCoverage.GetProperty("matchedPublicStandardProfileFieldCount").GetInt32() > 0);
            Assert.True(profileCoverage.GetProperty("developerFieldCount").GetInt32() > 0);
            Assert.Contains(entries.EnumerateArray(), entry =>
                entry.GetProperty("classification").GetString() == "MatchedPublicStandardProfile"
                && entry.GetProperty("sourceMessageFamily").GetString() == "record"
                && entry.GetProperty("sourceFieldName").GetString() == "timestamp");
            Assert.Contains(entries.EnumerateArray(), entry =>
                entry.GetProperty("classification").GetString() == "DeveloperField"
                && entry.GetProperty("sourceFieldName").GetString() == "session_score");
        }
        finally
        {
            DeleteTemporaryDirectory(outputDirectoryPath);
        }
    }

    [Fact]
    public async Task ShouldReportProfileCoverageForUnknownPreservedFields()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        FitActivity activity = FitActivityModelFactory.CreateActivityWithUnknownAncillaryDataForExport();
        CsvActivityExporter exporter = new();
        string outputDirectoryPath = CreateTemporaryDirectory();

        try
        {
            CsvExportRequest request = CsvExportRequestFactory.Create(
                activity,
                "unknown-coverage",
                outputDirectoryPath,
                [CreateColumnRequest(activity.Fields.Single(), order: 0)]);

            CsvExportResult result = await exporter.ExportAsync(request, cancellationToken);
            using JsonDocument manifest = await ReadManifestAsync(result, cancellationToken);
            JsonElement profileCoverage = manifest.RootElement.GetProperty("profileCoverage");
            JsonElement entries = profileCoverage.GetProperty("entries");

            Assert.True(profileCoverage.GetProperty("unknownOrUnmappedPreservedFieldCount").GetInt32() > 0);
            Assert.Contains(entries.EnumerateArray(), entry =>
                entry.GetProperty("classification").GetString() == "UnknownOrUnmappedPreservedField"
                && entry.GetProperty("sourceMessageFamily").GetString() == "unknown"
                && entry.GetProperty("sourceFieldName").GetString() == "unknown_0");
        }
        finally
        {
            DeleteTemporaryDirectory(outputDirectoryPath);
        }
    }

    [Fact]
    public async Task ShouldThrowNotSupportedExceptionWhenPresentationExportIsRequested()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        FitActivity activity = FitActivityModelFactory.CreateActivityForStructuredCsvExport();
        FitField field = GetSessionField(activity, "enhanced_avg_speed");
        CsvActivityExporter exporter = new();
        string outputDirectoryPath = CreateTemporaryDirectory();

        try
        {
            CsvExportRequest request = CsvExportRequestFactory.Create(
                activity,
                "sample",
                outputDirectoryPath,
                [CreateColumnRequest(field, order: 0)],
                options: new FitExportOptions(target: FitExportTarget.PresentationExport));

            _ = await Assert.ThrowsAsync<NotSupportedException>(() => exporter.ExportAsync(request, cancellationToken));
        }
        finally
        {
            DeleteTemporaryDirectory(outputDirectoryPath);
        }
    }

    private static ImmutableArray<CsvExportColumnRequest> CreateAllColumnRequests(FitActivity activity)
    {
        ImmutableArray<CsvExportColumnRequest>.Builder builder = ImmutableArray.CreateBuilder<CsvExportColumnRequest>();
        HashSet<FitExportColumnKey> seenColumnKeys = [];
        int order = 0;

        foreach (FitField field in activity.Fields)
        {
            AddColumnRequestIfMissing(field, builder, seenColumnKeys, ref order);
        }

        foreach (FitSession session in activity.Sessions)
        {
            foreach (FitField field in session.Fields)
            {
                AddColumnRequestIfMissing(field, builder, seenColumnKeys, ref order);
            }

            foreach (FitLap lap in session.Laps)
            {
                foreach (FitField field in lap.Fields)
                {
                    AddColumnRequestIfMissing(field, builder, seenColumnKeys, ref order);
                }
            }

            foreach (FitRecord record in session.Records)
            {
                foreach (FitField field in record.Fields)
                {
                    AddColumnRequestIfMissing(field, builder, seenColumnKeys, ref order);
                }
            }
        }

        return builder.ToImmutable();
    }

    private static void AddColumnRequestIfMissing(
        FitField field,
        ImmutableArray<CsvExportColumnRequest>.Builder builder,
        HashSet<FitExportColumnKey> seenColumnKeys,
        ref int order)
    {
        if (!seenColumnKeys.Add(field.Original.ExportColumnKey))
        {
            return;
        }

        builder.Add(CreateColumnRequest(field, order));
        order++;
    }

    private static JsonElement FindFieldDictionaryEntry(JsonElement fieldDictionary, string exportName)
        => fieldDictionary.EnumerateArray().Single(entry => entry.GetProperty("exportName").GetString() == exportName);

    private static JsonElement FindFieldDictionaryEntryByCanonicalName(JsonElement fieldDictionary, string canonicalName)
        => fieldDictionary.EnumerateArray().Single(entry => entry.GetProperty("canonicalName").GetString() == canonicalName);

    private static string GetColumnValue(string[] headerCells, string[] valueCells, string columnName)
        => valueCells[Array.IndexOf(headerCells, columnName)];

    private static async Task<string[]> ReadAllLinesAsync(
        CsvExportResult result,
        ExportedArtifactKind kind,
        FitNodeType nodeType,
        CancellationToken cancellationToken)
    {
        ExportedArtifact artifact = result.ExportedArtifacts.Single(exportedArtifact =>
            exportedArtifact.Kind == kind && exportedArtifact.NodeType == nodeType);
        return await File.ReadAllLinesAsync(artifact.FilePath, cancellationToken);
    }

    private static async Task<JsonDocument> ReadManifestAsync(CsvExportResult result, CancellationToken cancellationToken)
    {
        ExportedArtifact manifestArtifact = result.ExportedArtifacts.Single(artifact => artifact.Kind == ExportedArtifactKind.Manifest);
        string json = await File.ReadAllTextAsync(manifestArtifact.FilePath, cancellationToken);
        return JsonDocument.Parse(json);
    }

    private static string[] SplitCsvLine(string line)
        => line.Split(',', StringSplitOptions.None);

    private static CsvExportColumnRequest CreateColumnRequest(FitField field, int order)
        => new(
            field.Original.ExportColumnKey,
            field.Original.OriginalName,
            field.State.ColumnName,
            order,
            isSelected: true);

    private static string CreateTemporaryDirectory()
    {
        string outputDirectoryPath = Path.Combine(Path.GetTempPath(), "FitToCsvConverter.Test", Guid.NewGuid().ToString("N"));
        _ = Directory.CreateDirectory(outputDirectoryPath);
        return outputDirectoryPath;
    }

    private static void DeleteTemporaryDirectory(string outputDirectoryPath)
    {
        if (Directory.Exists(outputDirectoryPath))
        {
            Directory.Delete(outputDirectoryPath, recursive: true);
        }
    }

    private static FitField GetRecordField(FitActivity activity, string originalName)
        => activity.Sessions[0].Records[0].Fields.Single(field => field.Original.OriginalName == originalName);

    private static FitField GetSessionField(FitActivity activity, string originalName)
        => activity.Sessions[0].Fields.Single(field => field.Original.OriginalName == originalName);

    private sealed class TestTemporaryFileManager(string temporaryDirectoryPath) : ITemporaryFileManager
    {
        public string TemporaryDirectoryPath { get; } = temporaryDirectoryPath;

        public string CreateTemporaryFilePath(string fileName)
            => Path.Combine(TemporaryDirectoryPath, Path.GetFileName(fileName));

        public string MakeFileNameUnique(string fileName)
            => fileName;

        public void RegisterTemporaryFilePath(string filePath)
        {
        }

        public void CleanUpTemporaryFiles()
        {
        }
    }
}
