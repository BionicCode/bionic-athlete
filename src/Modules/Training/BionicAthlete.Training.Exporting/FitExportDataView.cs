namespace BionicAthlete.Training.Exporting;

/// <summary>
/// Selects which data view is physically emitted by a structured export.
/// </summary>
/// <remarks>
/// <see cref="StructuredMachine"/> is View B: the default machine-readable projection intended for normal export.
/// <see cref="RawCanonical"/> is View A: the debug/audit-oriented raw canonical FIT view.
/// </remarks>
public enum FitExportDataView
{
    /// <summary>
    /// Emit the grouped, normalized machine projection.
    /// </summary>
    StructuredMachine = 0,

    /// <summary>
    /// Emit raw canonical FIT artifacts for debugging, audit, or persistence-ingestion validation.
    /// </summary>
    RawCanonical = 1
}
