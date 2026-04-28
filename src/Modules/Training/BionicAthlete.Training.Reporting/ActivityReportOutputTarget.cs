namespace BionicAthlete.Training.Reporting;

/// <summary>
/// Defines which user-facing View C artifacts should be produced from the generated HTML report package.
/// </summary>
/// <remarks>
/// All v1 targets keep the generated HTML report. PDF generation is a later UI-bound rendering step over that same HTML.
/// </remarks>
public enum ActivityReportOutputTarget
{
    /// <summary>
    /// Generate only <c>activity-report.html</c> and <c>report-manifest.json</c>.
    /// </summary>
    HtmlOnly = 0,

    /// <summary>
    /// Generate the HTML package and request a PDF from that generated HTML.
    /// </summary>
    PdfFromGeneratedHtml = 1,

    /// <summary>
    /// Generate the HTML package and a PDF while explicitly communicating that both outputs are desired.
    /// </summary>
    HtmlAndPdf = 2
}
