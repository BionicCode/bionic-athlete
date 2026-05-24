namespace FitToCsvConverter.Data.Exporting;

using FitToCsvConverter.Data.Activities;
using FitToCsvConverter.Data.Fields;

/// <summary>
/// Describes one candidate column coming from UI or application selection state before the final CSV request is built.
/// </summary>
/// <remarks>
/// This type keeps checkbox and naming decisions outside the exporter contract itself.
/// Callers map their own selection state into <see cref="CsvExportColumnRequest"/> instances and then use
/// <see cref="CsvExportRequestFactory"/> to build the final <see cref="CsvExportRequest"/>.
/// </remarks>
public sealed class CsvExportColumnRequest
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CsvExportColumnRequest"/> class.
    /// </summary>
    /// <param name="columnKey">The stable export column key.</param>
    /// <param name="sourceName">The original FIT field name.</param>
    /// <param name="effectiveColumnName">The effective CSV column name to write.</param>
    /// <param name="order">The deterministic UI/application ordering for the column.</param>
    /// <param name="isSelected">
    /// A value indicating whether the column should be included in the final <see cref="CsvExportRequest"/>.
    /// </param>
    public CsvExportColumnRequest(
        FitExportColumnKey columnKey,
        string sourceName,
        string effectiveColumnName,
        int order,
        bool isSelected)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceName);
        ArgumentException.ThrowIfNullOrWhiteSpace(effectiveColumnName);

        if (order < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(order), order, "Column order must be zero or greater.");
        }

        ColumnKey = columnKey;
        SourceName = sourceName;
        EffectiveColumnName = effectiveColumnName;
        Order = order;
        IsSelected = isSelected;
    }

    /// <summary>
    /// Gets the stable export column key.
    /// </summary>
    public FitExportColumnKey ColumnKey { get; }

    /// <summary>
    /// Gets the node type that owns the column.
    /// </summary>
    public FitNodeType NodeType => ColumnKey.NodeType;

    /// <summary>
    /// Gets the original FIT field name.
    /// </summary>
    public string SourceName { get; }

    /// <summary>
    /// Gets the effective CSV column name.
    /// </summary>
    public string EffectiveColumnName { get; }

    /// <summary>
    /// Gets the deterministic order of the column inside its node output.
    /// </summary>
    public int Order { get; }

    /// <summary>
    /// Gets a value indicating whether the column should be included in the final CSV export.
    /// </summary>
    public bool IsSelected { get; }
}
