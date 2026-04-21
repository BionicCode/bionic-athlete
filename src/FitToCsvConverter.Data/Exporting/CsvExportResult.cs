namespace FitToCsvConverter.Data.Exporting;

using System.Collections.Immutable;

/// <summary>
/// Represents the outcome of a CSV export operation.
/// </summary>
public sealed class CsvExportResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CsvExportResult"/> class.
    /// </summary>
    /// <param name="exportedArtifacts">The generated CSV artifacts.</param>
    public CsvExportResult(ImmutableArray<ExportedArtifact> exportedArtifacts)
        => ExportedArtifacts = exportedArtifacts.IsDefault ? ImmutableArray<ExportedArtifact>.Empty : exportedArtifacts;

    /// <summary>
    /// Gets the generated CSV artifacts.
    /// </summary>
    public ImmutableArray<ExportedArtifact> ExportedArtifacts { get; }
}
