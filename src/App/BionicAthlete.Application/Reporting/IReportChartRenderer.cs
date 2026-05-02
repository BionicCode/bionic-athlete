namespace BionicAthlete.Application.Reporting;

using BionicAthlete.Application.Reporting;

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
    string RenderChart(ReportChart chart, ReportExportOptions options);
}
