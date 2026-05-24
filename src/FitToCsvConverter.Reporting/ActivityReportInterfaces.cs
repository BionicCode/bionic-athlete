namespace FitToCsvConverter.Reporting;

using FitToCsvConverter.Data.Activities;

/// <summary>
/// Projects decoded FIT activity data into the neutral View C report model.
/// </summary>
public interface IActivityReportProjector
{
    /// <summary>
    /// Creates a semantic report model for one decoded activity.
    /// </summary>
    /// <param name="activity">The decoded activity source.</param>
    /// <param name="options">Deterministic report options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The projected report model.</returns>
    Task<ActivityReport> ProjectAsync(
        FitActivity activity,
        ActivityReportExportOptions options,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Writes a neutral report model as a self-contained HTML report package.
/// </summary>
public interface IActivityReportHtmlRenderer
{
    /// <summary>
    /// Renders <paramref name="report"/> into an HTML report folder.
    /// </summary>
    /// <param name="report">The report model to render.</param>
    /// <param name="options">Deterministic report options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The generated HTML package metadata.</returns>
    Task<HtmlReportPackage> RenderAsync(
        ActivityReport report,
        ActivityReportExportOptions options,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Updates a View C report manifest after UI-bound artifact generation completes.
/// </summary>
public interface IActivityReportManifestUpdater
{
    /// <summary>
    /// Adds the generated PDF artifact to an existing report manifest.
    /// </summary>
    /// <param name="reportPackage">The report package whose manifest should be updated.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes after the manifest has been rewritten.</returns>
    Task AddPdfArtifactAsync(HtmlReportPackage reportPackage, CancellationToken cancellationToken = default);
}

/// <summary>
/// Renders deterministic inline SVG charts for report HTML.
/// </summary>
public interface IReportChartRenderer
{
    /// <summary>
    /// Renders <paramref name="chart"/> as inline SVG.
    /// </summary>
    /// <param name="chart">Chart data to render.</param>
    /// <param name="options">Report options for culture and layout context.</param>
    /// <returns>Inline SVG markup.</returns>
    string RenderChart(ActivityReportChart chart, ActivityReportExportOptions options);
}
