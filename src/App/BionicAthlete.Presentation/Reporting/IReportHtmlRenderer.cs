namespace BionicAthlete.Training.Reporting;

using BionicAthlete.Presentation.Reporting;

/// <summary>
/// Writes a neutral report model as a self-contained HTML report package.
/// </summary>
public interface IReportHtmlRenderer
{
    /// <summary>
    /// Renders <paramref name="report"/> into an HTML report folder.
    /// </summary>
    /// <param name="report">The report model to render.</param>
    /// <param name="options">Deterministic report options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The generated HTML package metadata.</returns>
    Task<HtmlReportPackage> RenderAsync(
        Report report,
        ReportExportOptions options,
        CancellationToken cancellationToken = default);
}
