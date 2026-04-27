namespace BionicAthlete.Training.Domain.Exporting;

using System.Collections.Immutable;
using BionicAthlete.Training.Domain.Activities;

/// <summary>
/// Describes one node-level CSV output within a <see cref="CsvExportRequest"/>.
/// </summary>
public sealed class CsvNodeExportRequest
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CsvNodeExportRequest"/> class.
    /// </summary>
    /// <param name="nodeType">The node type to export.</param>
    /// <param name="destinationFilePath">The destination CSV file path for this node output.</param>
    /// <param name="columns">The ordered columns to write for this node output.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="destinationFilePath"/> is <see langword="null"/>, empty, or whitespace.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="nodeType"/> is <see cref="FitNodeType.Ancillary"/>.
    /// </exception>
    public CsvNodeExportRequest(
        FitNodeType nodeType,
        string destinationFilePath,
        ImmutableArray<CsvColumnSelection> columns)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationFilePath);
        ValidateNodeType(nodeType);

        if (columns.IsDefaultOrEmpty)
        {
            throw new ArgumentException("At least one selected column is required for a node export request.", nameof(columns));
        }

        foreach (CsvColumnSelection column in columns)
        {
            if (column.NodeType != nodeType)
            {
                throw new ArgumentException(
                    $"Column '{column.SourceName}' targets node type '{column.NodeType}', which does not match '{nodeType}'.",
                    nameof(columns));
            }
        }

        NodeType = nodeType;
        DestinationFilePath = destinationFilePath;
        Columns = columns;
    }

    /// <summary>
    /// Gets the node type to export.
    /// </summary>
    public FitNodeType NodeType { get; }

    /// <summary>
    /// Gets the destination CSV file path for this node output.
    /// </summary>
    public string DestinationFilePath { get; }

    /// <summary>
    /// Gets the ordered columns to write for this node output.
    /// </summary>
    public ImmutableArray<CsvColumnSelection> Columns { get; }

    private static void ValidateNodeType(FitNodeType nodeType)
    {
        if (nodeType == FitNodeType.Ancillary)
        {
            throw new ArgumentOutOfRangeException(nameof(nodeType), nodeType, "Ancillary messages are not exported through the node CSV writer.");
        }
    }
}
