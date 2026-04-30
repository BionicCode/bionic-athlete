namespace BionicAthlete.Training.Test.Reporting;

using System.Globalization;
using System.Text.Json;
using BionicAthlete.Infrastructure.FileSystem.Reporting;
using BionicAthlete.Training.Test.Fixtures;

public sealed class ActivityReportHtmlRendererTests
{
    [Fact]
    public async Task ShouldGenerateDeterministicHtmlWhenInputAndOptionsMatch()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string firstOutputDirectoryPath = CreateTemporaryDirectory();
        string secondOutputDirectoryPath = CreateTemporaryDirectory();

        try
        {
            string firstHtml = await RenderHtmlAsync(firstOutputDirectoryPath, cancellationToken);
            string secondHtml = await RenderHtmlAsync(secondOutputDirectoryPath, cancellationToken);

            Assert.Equal(firstHtml, secondHtml);
        }
        finally
        {
            DeleteTemporaryDirectory(firstOutputDirectoryPath);
            DeleteTemporaryDirectory(secondOutputDirectoryPath);
        }
    }

    [Fact]
    public async Task ShouldGenerateBrowserSafeReadinessScriptWhenRenderingHtml()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string outputDirectoryPath = CreateTemporaryDirectory();

        try
        {
            string html = await RenderHtmlAsync(outputDirectoryPath, cancellationToken);

            Assert.Contains("window.chrome?.webview?.postMessage", html, StringComparison.Ordinal);
            Assert.Contains("ReportReady", html, StringComparison.Ordinal);
            Assert.Contains("ReportFailed", html, StringComparison.Ordinal);
        }
        finally
        {
            DeleteTemporaryDirectory(outputDirectoryPath);
        }
    }

    [Fact]
    public async Task ShouldUsePrintCssAndInlineSvgWithoutRemoteResourcesWhenRenderingHtml()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string outputDirectoryPath = CreateTemporaryDirectory();

        try
        {
            string html = await RenderHtmlAsync(outputDirectoryPath, cancellationToken);

            Assert.Contains("@media print", html, StringComparison.Ordinal);
            Assert.Contains("@page a4-report", html, StringComparison.Ordinal);
            Assert.Contains("@page letter-report", html, StringComparison.Ordinal);
            Assert.Contains("break-inside: avoid", html, StringComparison.Ordinal);
            Assert.Contains("page-break-inside: avoid", html, StringComparison.Ordinal);
            Assert.Contains("thead", html, StringComparison.Ordinal);
            Assert.Contains("<svg", html, StringComparison.Ordinal);
            Assert.DoesNotContain("http://", html, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("https://", html, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("cdn", html, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("@import", html, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            DeleteTemporaryDirectory(outputDirectoryPath);
        }
    }

    [Fact]
    public async Task ShouldWriteHtmlOnlyPackageWithoutPdfWhenOutputTargetIsHtmlOnly()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string outputDirectoryPath = CreateTemporaryDirectory();

        try
        {
            HtmlReportPackage package = await RenderPackageAsync(outputDirectoryPath, ReportOutputTarget.HtmlOnly, cancellationToken);

            Assert.True(File.Exists(package.HtmlFilePath));
            Assert.True(File.Exists(package.ManifestFilePath));
            Assert.Null(package.PdfFilePath);
            Assert.False(File.Exists(Path.Combine(package.ReportDirectoryPath, "activity-report.pdf")));
        }
        finally
        {
            DeleteTemporaryDirectory(outputDirectoryPath);
        }
    }

    [Fact]
    public async Task ShouldKeepHtmlPackageWhenPdfTargetIsRequested()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string outputDirectoryPath = CreateTemporaryDirectory();

        try
        {
            HtmlReportPackage package = await RenderPackageAsync(outputDirectoryPath, ReportOutputTarget.PdfFromGeneratedHtml, cancellationToken);

            Assert.True(File.Exists(package.HtmlFilePath));
            Assert.True(File.Exists(package.ManifestFilePath));
            Assert.Equal(Path.Combine(package.ReportDirectoryPath, "activity-report.pdf"), package.PdfFilePath);
            Assert.False(File.Exists(package.PdfFilePath));
        }
        finally
        {
            DeleteTemporaryDirectory(outputDirectoryPath);
        }
    }

    [Fact]
    public async Task ShouldWriteManifestPathsThatExistWhenRenderingHtml()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string outputDirectoryPath = CreateTemporaryDirectory();

        try
        {
            HtmlReportPackage package = await RenderPackageAsync(outputDirectoryPath, ReportOutputTarget.HtmlOnly, cancellationToken);
            using var manifest = JsonDocument.Parse(await File.ReadAllTextAsync(package.ManifestFilePath, cancellationToken));

            string[] relativePaths = manifest.RootElement
                .GetProperty("Artifacts")
                .EnumerateArray()
                .Select(static artifact => artifact.GetProperty("RelativePath").GetString() ?? string.Empty)
                .ToArray();

            Assert.All(relativePaths, relativePath => Assert.True(File.Exists(Path.Combine(package.ReportDirectoryPath, relativePath))));
        }
        finally
        {
            DeleteTemporaryDirectory(outputDirectoryPath);
        }
    }

    private static async Task<string> RenderHtmlAsync(string outputDirectoryPath, CancellationToken cancellationToken)
    {
        HtmlReportPackage package = await RenderPackageAsync(outputDirectoryPath, ReportOutputTarget.HtmlOnly, cancellationToken);
        return await File.ReadAllTextAsync(package.HtmlFilePath, cancellationToken);
    }

    private static async Task<HtmlReportPackage> RenderPackageAsync(
        string outputDirectoryPath,
        ReportOutputTarget outputTarget,
        CancellationToken cancellationToken)
    {
        var options = new ActivityReportExportOptions(
            outputDirectoryPath,
            outputTarget,
            CultureInfo.InvariantCulture,
            TimeZoneInfo.Utc,
            new DateTimeOffset(2026, 04, 26, 12, 00, 00, TimeSpan.Zero),
            PdfPageSettings.A4Portrait);
        var projector = new ActivityReportProjector();
        var renderer = new ActivityReportHtmlRenderer(new InlineSvgReportChartRenderer());

        Report report = await projector.ProjectAsync(
            FitActivityModelFactory.CreateActivityForDerivedSessionExport(),
            options,
            cancellationToken);
        return await renderer.RenderAsync(report, options, cancellationToken);
    }

    private static string CreateTemporaryDirectory()
    {
        string outputDirectoryPath = Path.Combine(Path.GetTempPath(), "BionicAthlete.Training.Test", Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture));
        _ = Directory.CreateDirectory(outputDirectoryPath);
        return outputDirectoryPath;
    }

    private static void DeleteTemporaryDirectory(string outputDirectoryPath)
    {
        if (Directory.Exists(outputDirectoryPath))
        {
            Directory.Delete(outputDirectoryPath, recursive: true);
        }
    }
}
