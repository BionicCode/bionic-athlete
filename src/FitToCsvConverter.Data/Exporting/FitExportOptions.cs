namespace FitToCsvConverter.Data.Exporting;

/// <summary>
/// Defines export-level policy that must stay separate from the decoded FIT source model.
/// </summary>
/// <remarks>
/// These options control export intent, normalization, and timestamp projection.
/// They intentionally do not live on <c>FitField</c> or other decoded-model types because the decoded activity tree
/// must remain independent from output-specific presentation policy.
/// </remarks>
public sealed class FitExportOptions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FitExportOptions"/> class.
    /// </summary>
    /// <param name="target">The intended export target.</param>
    /// <param name="unitSystem">The unit system to use for normalized export values.</param>
    /// <param name="includeUnitSuffixInHeaders">
    /// <see langword="true"/> to append explicit unit or timestamp suffixes to column headers;
    /// otherwise, <see langword="false"/>.
    /// </param>
    /// <param name="includeLocalTimeColumns">
    /// <see langword="true"/> to duplicate timestamp columns as local-time columns;
    /// otherwise, <see langword="false"/>.
    /// </param>
    /// <param name="missingValueStyle">The rendering style for missing values.</param>
    /// <param name="missingValueLiteral">
    /// The literal to render when <paramref name="missingValueStyle"/> is <see cref="FitExportMissingValueStyle.Literal"/>.
    /// </param>
    /// <param name="localTimeZone">
    /// The time zone used for local timestamp duplicates.
    /// When <see langword="null"/>, <see cref="TimeZoneInfo.Local"/> is used.
    /// </param>
    public FitExportOptions(
        FitExportTarget target = FitExportTarget.StructuredCsv,
        FitExportUnitSystem unitSystem = FitExportUnitSystem.Metric,
        bool includeUnitSuffixInHeaders = true,
        bool includeLocalTimeColumns = false,
        FitExportMissingValueStyle missingValueStyle = FitExportMissingValueStyle.Blank,
        string? missingValueLiteral = null,
        TimeZoneInfo? localTimeZone = null)
    {
        Target = target;
        UnitSystem = unitSystem;
        IncludeUnitSuffixInHeaders = includeUnitSuffixInHeaders;
        IncludeLocalTimeColumns = includeLocalTimeColumns;
        MissingValueStyle = missingValueStyle;
        MissingValueLiteral = missingValueLiteral ?? string.Empty;
        LocalTimeZone = localTimeZone ?? TimeZoneInfo.Local;
    }

    /// <summary>
    /// Gets the intended export target.
    /// </summary>
    public FitExportTarget Target { get; }

    /// <summary>
    /// Gets the unit system used for normalized export values.
    /// </summary>
    public FitExportUnitSystem UnitSystem { get; }

    /// <summary>
    /// Gets a value indicating whether explicit unit or timestamp suffixes are appended to headers.
    /// </summary>
    public bool IncludeUnitSuffixInHeaders { get; }

    /// <summary>
    /// Gets a value indicating whether timestamp columns are duplicated as local-time columns.
    /// </summary>
    public bool IncludeLocalTimeColumns { get; }

    /// <summary>
    /// Gets the rendering style used for missing values.
    /// </summary>
    public FitExportMissingValueStyle MissingValueStyle { get; }

    /// <summary>
    /// Gets the literal rendered for missing values when <see cref="MissingValueStyle"/> is
    /// <see cref="FitExportMissingValueStyle.Literal"/>.
    /// </summary>
    public string MissingValueLiteral { get; }

    /// <summary>
    /// Gets the time zone used for local timestamp duplicates.
    /// </summary>
    public TimeZoneInfo LocalTimeZone { get; }
}
