namespace BionicAthlete.Training.Test.Exporting;

using BionicAthlete.Training.Domain.Activities;
using BionicAthlete.Training.Domain.Exporting;
using BionicAthlete.Training.Domain.Fields;
using BionicAthlete.Training.Test.Fixtures;

public sealed class CsvExportRequestFactoryTests
{
    [Fact]
    public void ShouldFilterOutUnselectedColumnsWhenBuildingRequest()
    {
        FitActivity activity = FitActivityModelFactory.CreateActivityForExport();
        FitField selectedField = GetRecordField(activity, "heart_rate");
        FitField unselectedField = GetRecordField(activity, "cadence");

        CsvExportRequest request = CsvExportRequestFactory.Create(
            activity,
            "sample",
            @"C:\exports",
            [
                CreateColumnRequest(selectedField, order: 0, isSelected: true),
                CreateColumnRequest(unselectedField, order: 1, isSelected: false)
            ]);

        CsvNodeExportRequest recordRequest = Assert.Single(request.NodeRequests);
        CsvColumnSelection selectedColumn = Assert.Single(recordRequest.Columns);
        Assert.Equal(selectedField.Original.ExportColumnKey, selectedColumn.ColumnKey);
    }

    [Fact]
    public void ShouldOrderColumnsByOrderThenSourceNameWhenBuildingRequest()
    {
        FitActivity activity = FitActivityModelFactory.CreateActivityForExport();
        FitField heartRateField = GetRecordField(activity, "heart_rate");
        FitField cadenceField = GetRecordField(activity, "cadence");
        FitField powerZonesField = GetRecordField(activity, "power_zones");

        CsvExportRequest request = CsvExportRequestFactory.Create(
            activity,
            "sample",
            @"C:\exports",
            [
                CreateColumnRequest(heartRateField, order: 2),
                CreateColumnRequest(powerZonesField, order: 0),
                CreateColumnRequest(cadenceField, order: 0)
            ]);

        CsvNodeExportRequest recordRequest = Assert.Single(request.NodeRequests);
        Assert.Collection(
            recordRequest.Columns,
            column => Assert.Equal("cadence", column.SourceName),
            column => Assert.Equal("power_zones", column.SourceName),
            column => Assert.Equal("heart_rate", column.SourceName));
    }

    [Fact]
    public void ShouldPreserveEffectiveColumnNamesWhenBuildingRequest()
    {
        FitActivity activity = FitActivityModelFactory.CreateActivityForExport();
        FitField field = GetRecordField(activity, "heart_rate");
        field.SetColumnName("Heart Rate (BPM)");

        CsvExportRequest request = CsvExportRequestFactory.Create(
            activity,
            "sample",
            @"C:\exports",
            [CreateColumnRequest(field, order: 0)]);

        CsvNodeExportRequest recordRequest = Assert.Single(request.NodeRequests);
        CsvColumnSelection column = Assert.Single(recordRequest.Columns);
        Assert.Equal("Heart Rate (BPM)", column.ColumnName);
    }

    [Fact]
    public void ShouldUseOriginalFieldNameAsDefaultColumnNameWhenNoOverrideWasApplied()
    {
        FitActivity activity = FitActivityModelFactory.CreateActivityForExport();
        FitField field = GetRecordField(activity, "heart_rate");

        CsvExportRequest request = CsvExportRequestFactory.Create(
            activity,
            "sample",
            @"C:\exports",
            [CreateColumnRequest(field, order: 0)]);

        CsvNodeExportRequest recordRequest = Assert.Single(request.NodeRequests);
        CsvColumnSelection column = Assert.Single(recordRequest.Columns);
        Assert.Equal(field.Original.OriginalName, column.ColumnName);
    }

    [Fact]
    public void ShouldCreateSeparateNodeRequestsWhenSelectedColumnsSpanMultipleNodeTypes()
    {
        FitActivity activity = FitActivityModelFactory.CreateActivityForExport();

        CsvExportRequest request = CsvExportRequestFactory.Create(
            activity,
            "sample",
            @"C:\exports",
            [
                CreateColumnRequest(activity.Fields.Single(), order: 0),
                CreateColumnRequest(activity.Sessions[0].Fields.Single(), order: 0),
                CreateColumnRequest(activity.Sessions[0].Laps[0].Fields.Single(), order: 0),
                CreateColumnRequest(GetRecordField(activity, "heart_rate"), order: 0)
            ]);

        Assert.Collection(
            request.NodeRequests,
            nodeRequest => Assert.Equal(FitNodeType.Activity, nodeRequest.NodeType),
            nodeRequest => Assert.Equal(FitNodeType.Session, nodeRequest.NodeType),
            nodeRequest => Assert.Equal(FitNodeType.Lap, nodeRequest.NodeType),
            nodeRequest => Assert.Equal(FitNodeType.Record, nodeRequest.NodeType));
    }

    [Fact]
    public void ShouldDefaultToStructuredCsvOptionsWhenNoExportOptionsAreProvided()
    {
        FitActivity activity = FitActivityModelFactory.CreateActivityForExport();
        FitField field = GetRecordField(activity, "heart_rate");

        CsvExportRequest request = CsvExportRequestFactory.Create(
            activity,
            "sample",
            @"C:\exports",
            [CreateColumnRequest(field, order: 0)]);

        Assert.Equal(FitExportTarget.StructuredCsv, request.Options.Target);
        Assert.Equal(FitExportDataView.StructuredMachine, request.Options.DataView);
        Assert.Equal(FitExportUnitSystem.Metric, request.Options.UnitSystem);
        Assert.True(request.Options.IncludeUnitSuffixInHeaders);
        Assert.False(request.Options.IncludeLocalTimeColumns);
        Assert.Equal(FitExportMissingValueStyle.Blank, request.Options.MissingValueStyle);
    }

    [Fact]
    public void ShouldPreserveExplicitExportOptionsWhenOptionsAreProvided()
    {
        FitActivity activity = FitActivityModelFactory.CreateActivityForExport();
        FitField field = GetRecordField(activity, "heart_rate");
        TimeZoneInfo localTimeZone = TimeZoneInfo.CreateCustomTimeZone("UTC+01", TimeSpan.FromHours(1), "UTC+01", "UTC+01");
        FitExportOptions options = new(
            unitSystem: FitExportUnitSystem.Imperial,
            includeUnitSuffixInHeaders: false,
            includeLocalTimeColumns: true,
            missingValueStyle: FitExportMissingValueStyle.Literal,
            missingValueLiteral: "NA",
            localTimeZone: localTimeZone);

        CsvExportRequest request = CsvExportRequestFactory.Create(
            activity,
            "sample",
            @"C:\exports",
            [CreateColumnRequest(field, order: 0)],
            options: options);

        Assert.Same(options, request.Options);
    }

    private static CsvExportColumnRequest CreateColumnRequest(FitField field, int order, bool isSelected = true)
        => new(
            field.Original.ExportColumnKey,
            field.Original.OriginalName,
            field.State.ColumnName,
            order,
            isSelected);

    private static FitField GetRecordField(FitActivity activity, string originalName)
        => activity.Sessions[0].Records[0].Fields.Single(field => field.Original.OriginalName == originalName);
}
