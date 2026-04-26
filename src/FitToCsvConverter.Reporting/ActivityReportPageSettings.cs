namespace FitToCsvConverter.Reporting;

/// <summary>
/// Neutral print settings used by report generation and later mapped to WebView2 print settings in the UI layer.
/// </summary>
/// <remarks>
/// Values are expressed in inches because WebView2 print settings use inch-based page and margin dimensions.
/// </remarks>
public sealed record ActivityReportPageSettings(
    ActivityReportPagePreset PagePreset,
    ActivityReportPageOrientation Orientation,
    double PageWidthInches,
    double PageHeightInches,
    double MarginTopInches,
    double MarginBottomInches,
    double MarginLeftInches,
    double MarginRightInches,
    double ScaleFactor,
    bool ShouldPrintBackgrounds,
    bool ShouldPrintHeaderAndFooter,
    string? PageRanges = null)
{
    /// <summary>
    /// Gets the default A4 portrait page settings.
    /// </summary>
    public static ActivityReportPageSettings A4Portrait { get; } = new(
        ActivityReportPagePreset.A4Portrait,
        ActivityReportPageOrientation.Portrait,
        PageWidthInches: 8.27d,
        PageHeightInches: 11.69d,
        MarginTopInches: 0.55d,
        MarginBottomInches: 0.55d,
        MarginLeftInches: 0.55d,
        MarginRightInches: 0.55d,
        ScaleFactor: 1d,
        ShouldPrintBackgrounds: true,
        ShouldPrintHeaderAndFooter: false);

    /// <summary>
    /// Gets the default US Letter portrait page settings.
    /// </summary>
    public static ActivityReportPageSettings UsLetterPortrait { get; } = new(
        ActivityReportPagePreset.UsLetterPortrait,
        ActivityReportPageOrientation.Portrait,
        PageWidthInches: 8.5d,
        PageHeightInches: 11d,
        MarginTopInches: 0.55d,
        MarginBottomInches: 0.55d,
        MarginLeftInches: 0.55d,
        MarginRightInches: 0.55d,
        ScaleFactor: 1d,
        ShouldPrintBackgrounds: true,
        ShouldPrintHeaderAndFooter: false);
}
