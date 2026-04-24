namespace FitToCsvConverter.Data.Exporting;

using FitToCsvConverter.Data.Activities;

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
    public ExportedArtifact(ExportedArtifactKind kind, FitNodeType nodeType, string artifactName, string filePath, int rowCount)
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
    /// Gets the number of data rows written to the file.
    /// </summary>
    public int RowCount { get; }
}
