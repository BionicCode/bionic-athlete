namespace FitToCsvConverter.Test.Exporting;

using FitToCsvConverter.Data.Activities;
using FitToCsvConverter.Data.Exporting;
using FitToCsvConverter.Data.Fields;
using FitToCsvConverter.Test.Fixtures;

public sealed class CsvActivityExporterTests
{
    [Fact]
    public async Task ShouldWriteSeparateCsvFilesWhenNodeRequestsAreSelected()
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
                    Assert.Equal(FitNodeType.Activity, artifact.NodeType);
                    Assert.EndsWith("_activity.csv", artifact.FilePath, StringComparison.OrdinalIgnoreCase);
                    Assert.Equal(1, artifact.RowCount);
                },
                artifact =>
                {
                    Assert.Equal(FitNodeType.Session, artifact.NodeType);
                    Assert.EndsWith("_session.csv", artifact.FilePath, StringComparison.OrdinalIgnoreCase);
                    Assert.Equal(1, artifact.RowCount);
                },
                artifact =>
                {
                    Assert.Equal(FitNodeType.Lap, artifact.NodeType);
                    Assert.EndsWith("_lap.csv", artifact.FilePath, StringComparison.OrdinalIgnoreCase);
                    Assert.Equal(1, artifact.RowCount);
                },
                artifact =>
                {
                    Assert.Equal(FitNodeType.Record, artifact.NodeType);
                    Assert.EndsWith("_record.csv", artifact.FilePath, StringComparison.OrdinalIgnoreCase);
                    Assert.Equal(2, artifact.RowCount);
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
            string[] lines = await File.ReadAllLinesAsync(result.ExportedArtifacts[0].FilePath, cancellationToken);

            Assert.Equal("Heart Rate (BPM)", lines[0]);
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
            string[] lines = await File.ReadAllLinesAsync(result.ExportedArtifacts[0].FilePath, cancellationToken);

            Assert.Equal("1 | 2 | 3", lines[1]);
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
            string[] lines = await File.ReadAllLinesAsync(result.ExportedArtifacts[0].FilePath, cancellationToken);

            Assert.Equal("999", lines[1]);
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
            string[] lines = await File.ReadAllLinesAsync(result.ExportedArtifacts[0].FilePath, cancellationToken);

            Assert.Equal("enhanced_avg_speed [km/h]", lines[0]);
            Assert.Equal("18.3888", lines[1]);
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
            string[] lines = await File.ReadAllLinesAsync(result.ExportedArtifacts[0].FilePath, cancellationToken);

            Assert.Equal("total_distance [km]", lines[0]);
            Assert.Equal("6.14749", lines[1]);
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
            string[] lines = await File.ReadAllLinesAsync(result.ExportedArtifacts[0].FilePath, cancellationToken);

            Assert.Equal("total_elapsed_time [s]", lines[0]);
            Assert.Equal("1247.782", lines[1]);
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
            string[] lines = await File.ReadAllLinesAsync(result.ExportedArtifacts[0].FilePath, cancellationToken);

            Assert.Equal("total_elapsed_time [s],total_timer_time [s]", lines[0]);
            Assert.Equal("1247.782,1203.591", lines[1]);
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
            string[] lines = await File.ReadAllLinesAsync(result.ExportedArtifacts[0].FilePath, cancellationToken);

            Assert.Equal("avg_power_position [watts]", lines[0]);
            Assert.Equal(string.Empty, lines[1]);
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
            string[] lines = await File.ReadAllLinesAsync(result.ExportedArtifacts[0].FilePath, cancellationToken);

            Assert.Equal("enhanced_speed [km/h]", lines[0]);
            Assert.Equal("18", lines[1]);
            Assert.Equal("21.6", lines[2]);
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
            string[] lines = await File.ReadAllLinesAsync(result.ExportedArtifacts[0].FilePath, cancellationToken);

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
}