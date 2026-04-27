namespace BionicAthlete.Training.Exporting;

using System.Collections.Immutable;

/// <summary>
/// Represents the outcome of a structured export operation.
/// </summary>
public sealed class CsvExportResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CsvExportResult"/> class.
    /// </summary>
    /// <param name="exportedArtifacts">The generated export artifacts.</param>
    public CsvExportResult(ImmutableArray<ExportedArtifact> exportedArtifacts)
        => ExportedArtifacts = exportedArtifacts.IsDefault ? ImmutableArray<ExportedArtifact>.Empty : exportedArtifacts;

    /// <summary>
    /// Gets the generated export artifacts.
    /// </summary>
    public ImmutableArray<ExportedArtifact> ExportedArtifacts { get; }
}
