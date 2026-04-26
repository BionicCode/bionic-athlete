namespace FitToCsvConverter.Reporting;

/// <summary>
/// Identifies the print-layout preset used by the HTML report and WebView2 PDF settings.
/// </summary>
public enum ActivityReportPagePreset
{
    /// <summary>
    /// A custom page size supplied by <see cref="ActivityReportPageSettings"/>.
    /// </summary>
    Custom = 0,

    /// <summary>
    /// A4 portrait page preset.
    /// </summary>
    A4Portrait = 1,

    /// <summary>
    /// US Letter portrait page preset.
    /// </summary>
    UsLetterPortrait = 2
}
