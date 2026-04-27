namespace BionicAthlete.Training.Exporting;

/// <summary>
/// Exports decoded FIT activity data to structured export artifacts.
/// </summary>
/// <remarks>
/// This contract is intentionally limited to outward-facing file export.
/// It does not define the application's persistence model or any remote sync shape.
/// The current implementation supports <see cref="FitExportTarget.StructuredCsv"/> requests and reserves
/// presentation-oriented export targets for future work.
/// </remarks>
public interface ICsvActivityExporter
{
    /// <summary>
    /// Exports the selected portions of a decoded activity to a structured bundle of CSV files and metadata artifacts.
    /// </summary>
    /// <param name="request">The export request that describes which nodes and columns to write.</param>
    /// <param name="cancellationToken">A token that can cancel the export operation.</param>
    /// <returns>A result that describes the generated export artifacts.</returns>
    Task<CsvExportResult> ExportAsync(CsvExportRequest request, CancellationToken cancellationToken = default);
}
