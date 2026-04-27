namespace BionicAthlete.Training.Exporting;

using BionicAthlete.Training.Domain.Activities;

/// <summary>
/// Describes one generated export artifact.
/// </summary>
public sealed class ExportedArtifact
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ExportedArtifact"/> class.
    /// </summary>
    /// <param name="kind">The artifact kind.</param>
    /// <param name="nodeType">The node type written into the artifact when the artifact is FIT-node specific.</param>
    /// <param name="artifactName">The logical artifact name used inside the export bundle.</param>
    /// <param name="filePath">The generated file path.</param>
    /// <param name="rowCount">The number of data rows written to the file.</param>
    /// <param name="bundlePath">
    /// The path that should be used inside an export bundle.
    /// When <see langword="null"/>, <paramref name="artifactName"/> is used.
    /// </param>
    public ExportedArtifact(
        ExportedArtifactKind kind,
        FitNodeType nodeType,
        string artifactName,
        string filePath,
        int rowCount,
        string? bundlePath = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(artifactName);
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        if (rowCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(rowCount), rowCount, "Row count must be zero or greater.");
        }

        Kind = kind;
        NodeType = nodeType;
        ArtifactName = artifactName;
        FilePath = filePath;
        RowCount = rowCount;
        BundlePath = string.IsNullOrWhiteSpace(bundlePath)
            ? artifactName
            : bundlePath.Replace('\\', '/');
    }

    /// <summary>
    /// Gets the artifact kind.
    /// </summary>
    public ExportedArtifactKind Kind { get; }

    /// <summary>
    /// Gets the node type written into the artifact.
    /// </summary>
    public FitNodeType NodeType { get; }

    /// <summary>
    /// Gets the logical artifact name used inside the export bundle.
    /// </summary>
    public string ArtifactName { get; }

    /// <summary>
    /// Gets the generated file path.
    /// </summary>
    public string FilePath { get; }

    /// <summary>
    /// Gets the relative path that should be used when the artifact is packaged into an export bundle.
    /// </summary>
    public string BundlePath { get; }

    /// <summary>
    /// Gets the number of data rows written to the file.
    /// </summary>
    public int RowCount { get; }
}
