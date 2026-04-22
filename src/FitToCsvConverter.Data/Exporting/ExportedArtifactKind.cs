namespace FitToCsvConverter.Data.Exporting;

/// <summary>
/// Describes the kind of artifact produced during structured export.
/// </summary>
public enum ExportedArtifactKind
{
    /// <summary>
    /// A CSV artifact that represents one FIT node family such as activity, session, lap, or record.
    /// </summary>
    NodeCsv = 0,

    /// <summary>
    /// A CSV artifact that represents one ancillary FIT message family preserved outside the activity tree.
    /// </summary>
    AncillaryCsv = 1,

    /// <summary>
    /// A machine-readable manifest that describes schema, coverage, and field classifications for the bundle.
    /// </summary>
    Manifest = 2
}
