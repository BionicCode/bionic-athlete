namespace BionicAthlete.Training.Domain.Exporting;

/// <summary>
/// Identifies the intended export target for an export request.
/// </summary>
/// <remarks>
/// <see cref="StructuredCsv"/> is optimized for machine parsing, stable schema, and deterministic normalization.
/// <see cref="PresentationExport"/> is reserved for future human-readable report-style exports.
/// </remarks>
public enum FitExportTarget
{
    /// <summary>
    /// Export structured, machine-parseable CSV with deterministic normalization rules.
    /// </summary>
    StructuredCsv = 0,

    /// <summary>
    /// Export human-readable presentation output.
    /// This target is planned but not implemented by the current CSV exporter.
    /// </summary>
    PresentationExport = 1
}