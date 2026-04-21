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
}
