namespace BionicAthlete.Training.Reporting;

using System.Globalization;
using System.Text;
using BionicAthlete.Presentation.Reporting;
using BionicAthlete.Presentation.Reporting.Html;

/// <summary>
/// Renders small deterministic inline SVG charts for View C reports.
/// </summary>
public sealed class InlineSvgReportChartRenderer : IReportChartRenderer
{
    private const int Width = 760;
    private const int Height = 220;
    private const int PaddingLeft = 52;
    private const int PaddingRight = 20;
    private const int PaddingTop = 20;
    private const int PaddingBottom = 36;

    /// <inheritdoc />
    public string RenderChart(ReportChart chart, ReportExportOptions options)
    {
        ArgumentNullException.ThrowIfNull(chart);
        ArgumentNullException.ThrowIfNull(options);

        if (chart.Points.IsDefaultOrEmpty)
        {
            return "<p class=\"empty-chart\">No chart data available.</p>";
        }

        double minimumValue = chart.Points.Min(static point => point.Value);
        double maximumValue = chart.Points.Max(static point => point.Value);
        if (Math.Abs(maximumValue - minimumValue) < double.Epsilon)
        {
            maximumValue += 1d;
            minimumValue -= 1d;
        }

        var builder = new StringBuilder();
        _ = builder.Append(CultureInfo.InvariantCulture, $"<svg id=\"{HtmlText.Encode(chart.Id)}\" class=\"chart-svg\" viewBox=\"0 0 {Width} {Height}\" role=\"img\" aria-label=\"{HtmlText.Encode(chart.Title)} chart\">");
        _ = builder.Append("<rect class=\"chart-background\" x=\"0\" y=\"0\" width=\"100%\" height=\"100%\" rx=\"14\" />");
        _ = builder.Append(CultureInfo.InvariantCulture, $"<line class=\"chart-axis\" x1=\"{PaddingLeft}\" y1=\"{Height - PaddingBottom}\" x2=\"{Width - PaddingRight}\" y2=\"{Height - PaddingBottom}\" />");
        _ = builder.Append(CultureInfo.InvariantCulture, $"<line class=\"chart-axis\" x1=\"{PaddingLeft}\" y1=\"{PaddingTop}\" x2=\"{PaddingLeft}\" y2=\"{Height - PaddingBottom}\" />");
        _ = builder.Append(CultureInfo.InvariantCulture, $"<text class=\"chart-label\" x=\"{PaddingLeft}\" y=\"16\">{HtmlText.Encode(chart.ValueLabel)}{FormatUnit(chart.Unit)}</text>");
        _ = builder.Append(CultureInfo.InvariantCulture, $"<text class=\"chart-tick\" x=\"10\" y=\"{PaddingTop + 8}\">{HtmlText.Encode(FormatValue(maximumValue, options.Culture))}</text>");
        _ = builder.Append(CultureInfo.InvariantCulture, $"<text class=\"chart-tick\" x=\"10\" y=\"{Height - PaddingBottom}\">{HtmlText.Encode(FormatValue(minimumValue, options.Culture))}</text>");
        _ = builder.Append("<polyline class=\"chart-line\" fill=\"none\" points=\"");

        for (int index = 0; index < chart.Points.Length; index++)
        {
            ReportChartPoint point = chart.Points[index];
            double x = PaddingLeft + ((Width - PaddingLeft - PaddingRight) * (index / Math.Max(chart.Points.Length - 1d, 1d)));
            double normalizedValue = (point.Value - minimumValue) / (maximumValue - minimumValue);
            double y = Height - PaddingBottom - ((Height - PaddingTop - PaddingBottom) * normalizedValue);
            _ = builder.Append(CultureInfo.InvariantCulture, $"{x:0.###},{y:0.###} ");
        }

        _ = builder.Append("\" />");
        _ = builder.Append("</svg>");
        return builder.ToString();
    }

    private static string FormatValue(double value, CultureInfo culture)
        => value.ToString("N0", culture);

    private static string FormatUnit(string? unit)
        => string.IsNullOrWhiteSpace(unit) ? string.Empty : $" ({HtmlText.Encode(unit)})";
}
