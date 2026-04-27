namespace BionicAthlete.Training.Test.Reporting;

public sealed class WebView2PdfExporterSourceTests
{
    [Fact]
    public void ShouldCreateFreshPrintSettingsAndPassThemToPrintToPdfAsyncWhenSourceIsInspected()
    {
        string mapperSource = File.ReadAllText(GetControlsSourcePath("WebView2PrintSettingsMapper.cs"));
        string exporterSource = File.ReadAllText(GetControlsSourcePath("WebView2ActivityReportPdfExporter.cs"));

        Assert.Contains("environment.CreatePrintSettings()", mapperSource, StringComparison.Ordinal);
        Assert.Contains("PrintToPdfAsync(", exporterSource, StringComparison.Ordinal);
        Assert.Contains("printSettings", exporterSource, StringComparison.Ordinal);
        Assert.Contains("if (!isPdfGenerated)", exporterSource, StringComparison.Ordinal);
    }

    [Fact]
    public void ShouldWaitForReportReadyAndFailOnReportFailedWhenSourceIsInspected()
    {
        string waiterSource = File.ReadAllText(GetControlsSourcePath("WebView2ReportReadinessWaiter.cs"));

        Assert.Contains("ReportReady", waiterSource, StringComparison.Ordinal);
        Assert.Contains("ReportFailed", waiterSource, StringComparison.Ordinal);
        Assert.Contains("NavigationCompleted", waiterSource, StringComparison.Ordinal);
        Assert.Contains("TimeoutException", waiterSource, StringComparison.Ordinal);
    }

    [Fact]
    public void ShouldUsePerOperationHostWithoutSharedOperationFieldsWhenSourceIsInspected()
    {
        string exporterSource = File.ReadAllText(GetControlsSourcePath("WebView2ActivityReportPdfExporter.cs"));

        Assert.Contains("using HiddenWebView2ReportHost reportHost", exporterSource, StringComparison.Ordinal);
        Assert.DoesNotContain("_browser", exporterSource, StringComparison.Ordinal);
        Assert.DoesNotContain("_current", exporterSource, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("SemaphoreSlim", exporterSource, StringComparison.Ordinal);
    }

    private static string GetControlsSourcePath(string fileName)
        => Path.Combine(
            GetRepositoryRoot(),
            "src",
            "BionicAthlete.Presentation",
            "Reporting",
            fileName);

    private static string GetRepositoryRoot()
        => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
}
