namespace FitToCsvConverter.Data.Exporting;

using System.Collections.Immutable;
using System.Text;
using FitToCsvConverter.Data.Activities;

/// <summary>
/// Creates <see cref="CsvExportRequest"/> instances from higher-level column selection state.
/// </summary>
/// <remarks>
/// This factory keeps UI-oriented selection state out of the exporter contract.
/// The final <see cref="CsvExportRequest"/> is the only shape that the CSV exporter needs to understand.
/// </remarks>
public static class CsvExportRequestFactory
{
    /// <summary>
    /// Creates a CSV export request from the selected column requests for one decoded activity.
    /// </summary>
    /// <param name="sourceActivity">The decoded activity to export.</param>
    /// <param name="sourceFileNameWithoutExtension">
    /// The source file stem that should be used when generating per-node CSV file names.
    /// </param>
    /// <param name="outputDirectoryPath">The destination directory for generated CSV files.</param>
    /// <param name="columnRequests">The UI/application column requests to transform into node-specific exports.</param>
    /// <param name="encoding">
    /// The text encoding to use when writing the CSV files.
    /// When <see langword="null"/>, UTF-8 without BOM is used.
    /// </param>
    /// <param name="options">
    /// Export-level policy that controls target intent, normalization, and timestamp projection.
    /// When <see langword="null"/>, machine-parseable structured CSV defaults are used.
    /// </param>
    /// <param name="delimiter">The CSV delimiter to use when writing files.</param>
    /// <returns>The final CSV export request.</returns>
    public static CsvExportRequest Create(
        FitActivity sourceActivity,
        string sourceFileNameWithoutExtension,
        string outputDirectoryPath,
        IEnumerable<CsvExportColumnRequest> columnRequests,
        Encoding? encoding = null,
        FitExportOptions? options = null,
        char delimiter = ',')
    {
        ArgumentNullException.ThrowIfNull(sourceActivity);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceFileNameWithoutExtension);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectoryPath);
        ArgumentNullException.ThrowIfNull(columnRequests);

        // Keep node grouping and generated file naming centralized here so UI wrappers only contribute
        // selection state and do not implicitly define the CSV contract themselves.
        ImmutableArray<CsvNodeExportRequest> nodeRequests = columnRequests
            .Where(columnRequest => columnRequest.IsSelected)
            .GroupBy(columnRequest => columnRequest.NodeType)
            .OrderBy(group => group.Key)
            .Select(group => CreateNodeRequest(group, sourceFileNameWithoutExtension, outputDirectoryPath))
            .ToImmutableArray();

        return new CsvExportRequest(
            sourceActivity,
            sourceFileNameWithoutExtension,
            outputDirectoryPath,
            nodeRequests,
            encoding,
            options,
            delimiter);
    }

    private static CsvNodeExportRequest CreateNodeRequest(
        IGrouping<FitNodeType, CsvExportColumnRequest> columnGroup,
        string sourceFileNameWithoutExtension,
        string outputDirectoryPath)
    {
        ImmutableArray<CsvColumnSelection> orderedColumns = columnGroup
            // Preserve explicit order first and use source name as a stable tie-breaker so repeated exports
            // keep the same column order even when multiple fields share the same display position.
            .OrderBy(columnRequest => columnRequest.Order)
            .ThenBy(columnRequest => columnRequest.SourceName, StringComparer.OrdinalIgnoreCase)
            .Select(columnRequest => new CsvColumnSelection(
                columnRequest.ColumnKey,
                columnRequest.NodeType,
                columnRequest.SourceName,
                columnRequest.EffectiveColumnName,
                columnRequest.Order))
            .ToImmutableArray();

        string destinationFilePath = Path.Combine(
            outputDirectoryPath,
            $"{sourceFileNameWithoutExtension}_{columnGroup.Key.ToString().ToLowerInvariant()}.csv");

        return new CsvNodeExportRequest(columnGroup.Key, destinationFilePath, orderedColumns);
    }
}
