namespace FitToCsvConverter.Data.Exporting;

/// <summary>
/// Defines how missing export values are rendered in generated output.
/// </summary>
public enum FitExportMissingValueStyle
{
    /// <summary>
    /// Render missing values as blank cells.
    /// </summary>
    Blank = 0,

    /// <summary>
    /// Render missing values using the literal configured on the export options.
    /// </summary>
    Literal = 1
}
