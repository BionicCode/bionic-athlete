namespace FitToCsvConverter.Data.Exporting;

/// <summary>
/// Defines the unit system used when normalized export values support unit conversion.
/// </summary>
public enum FitExportUnitSystem
{
    /// <summary>
    /// Use metric normalization rules.
    /// </summary>
    Metric = 0,

    /// <summary>
    /// Use imperial normalization rules.
    /// </summary>
    Imperial = 1
}