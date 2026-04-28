namespace BionicAthlete.Presentation.Reporting;

using BionicAthlete.Training.Reporting;
using Microsoft.Web.WebView2.Core;

internal static class WebView2PrintSettingsMapper
{
    public static CoreWebView2PrintSettings CreatePrintSettings(
        CoreWebView2Environment environment,
        ActivityReportPageSettings pageSettings)
    {
        ArgumentNullException.ThrowIfNull(environment);
        ArgumentNullException.ThrowIfNull(pageSettings);

        CoreWebView2PrintSettings printSettings = environment.CreatePrintSettings();
        printSettings.Orientation = pageSettings.Orientation == ActivityReportPageOrientation.Landscape
            ? CoreWebView2PrintOrientation.Landscape
            : CoreWebView2PrintOrientation.Portrait;
        printSettings.PageWidth = pageSettings.PageWidthInches;
        printSettings.PageHeight = pageSettings.PageHeightInches;
        printSettings.MarginTop = pageSettings.MarginTopInches;
        printSettings.MarginBottom = pageSettings.MarginBottomInches;
        printSettings.MarginLeft = pageSettings.MarginLeftInches;
        printSettings.MarginRight = pageSettings.MarginRightInches;
        printSettings.ScaleFactor = pageSettings.ScaleFactor;
        printSettings.ShouldPrintBackgrounds = pageSettings.ShouldPrintBackgrounds;
        printSettings.ShouldPrintHeaderAndFooter = pageSettings.ShouldPrintHeaderAndFooter;

        if (!string.IsNullOrWhiteSpace(pageSettings.PageRanges))
        {
            printSettings.PageRanges = pageSettings.PageRanges;
        }

        return printSettings;
    }
}
