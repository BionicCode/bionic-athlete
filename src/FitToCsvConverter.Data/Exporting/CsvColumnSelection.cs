namespace FitToCsvConverter.Data.Exporting;

using FitToCsvConverter.Data.Activities;
using FitToCsvConverter.Data.Fields;

/// <summary>
/// Describes one selected CSV column within a node export.
/// </summary>
public sealed class CsvColumnSelection
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CsvColumnSelection"/> class.
    /// </summary>
    /// <param name="columnKey">The stable export column key.</param>
    /// <param name="nodeType">The node type that owns the column.</param>
    /// <param name="sourceName">The original FIT field name.</param>
    /// <param name="columnName">The effective CSV column name.</param>
    /// <param name="order">The deterministic order of the column in the output file.</param>
    public CsvColumnSelection(
        FitExportColumnKey columnKey,
        FitNodeType nodeType,
        string sourceName,
        string columnName,
        int order)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceName);
        ArgumentException.ThrowIfNullOrWhiteSpace(columnName);

        if (order < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(order), order, "Column order must be zero or greater.");
        }

        if (columnKey.NodeType != nodeType)
        {
            throw new ArgumentException(
                $"The export column key targets node type '{columnKey.NodeType}', which does not match '{nodeType}'.",
                nameof(columnKey));
        }

        ColumnKey = columnKey;
        NodeType = nodeType;
        SourceName = sourceName;
        ColumnName = columnName;
        Order = order;
    }

    /// <summary>
    /// Gets the stable export column key.
    /// </summary>
    public FitExportColumnKey ColumnKey { get; }

    /// <summary>
    /// Gets the node type that owns the column.
    /// </summary>
    public FitNodeType NodeType { get; }

    /// <summary>
    /// Gets the original FIT field name.
    /// </summary>
    public string SourceName { get; }

    /// <summary>
    /// Gets the effective CSV column name.
    /// </summary>
    public string ColumnName { get; }

    /// <summary>
    /// Gets the deterministic output order for the column.
    /// </summary>
    public int Order { get; }
}
