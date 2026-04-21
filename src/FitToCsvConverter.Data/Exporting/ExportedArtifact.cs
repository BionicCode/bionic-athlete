namespace FitToCsvConverter.Data.Exporting;

using FitToCsvConverter.Data.Activities;

/// <summary>
/// Describes one generated CSV artifact.
/// </summary>
public sealed class ExportedArtifact
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ExportedArtifact"/> class.
    /// </summary>
    /// <param name="nodeType">The node type written into the artifact.</param>
    /// <param name="filePath">The generated file path.</param>
    /// <param name="rowCount">The number of data rows written to the file.</param>
    public ExportedArtifact(FitNodeType nodeType, string filePath, int rowCount)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        if (rowCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(rowCount), rowCount, "Row count must be zero or greater.");
        }

        NodeType = nodeType;
        FilePath = filePath;
        RowCount = rowCount;
    }

    /// <summary>
    /// Gets the node type written into the artifact.
    /// </summary>
    public FitNodeType NodeType { get; }

    /// <summary>
    /// Gets the generated file path.
    /// </summary>
    public string FilePath { get; }

    /// <summary>
    /// Gets the number of data rows written to the file.
    /// </summary>
    public int RowCount { get; }
}
