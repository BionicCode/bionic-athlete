namespace BionicAthlete.Training.Test.Reporting;

public sealed class ReportingProjectBoundaryTests
{
    [Fact]
    public void ShouldTargetNet10AndAvoidUiReferencesWhenProjectIsInspected()
    {
        string projectText = File.ReadAllText(GetReportingProjectPath());

        Assert.Contains("<TargetFramework>net10.0</TargetFramework>", projectText, StringComparison.Ordinal);
        Assert.DoesNotContain("net10.0-windows", projectText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<UseWPF>true</UseWPF>", projectText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Microsoft.Web.WebView2", projectText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ShouldNotReferenceWpfOrWebView2WhenReportingSourceIsInspected()
    {
        string sourceText = string.Join(
            Environment.NewLine,
            Directory.EnumerateFiles(GetReportingProjectDirectory(), "*.cs", SearchOption.AllDirectories)
                .Select(File.ReadAllText));

        Assert.DoesNotContain("Microsoft.Web.WebView2", sourceText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("System.Windows", sourceText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Window", sourceText, StringComparison.Ordinal);
        Assert.DoesNotContain("DispatcherObject", sourceText, StringComparison.Ordinal);
        Assert.DoesNotContain("DependencyObject", sourceText, StringComparison.Ordinal);
    }

    private static string GetReportingProjectPath()
        => Path.Combine(GetRepositoryRoot(), "src", "BionicAthlete.Training.Reporting", "BionicAthlete.Training.Reporting.csproj");

    private static string GetReportingProjectDirectory()
        => Path.GetDirectoryName(GetReportingProjectPath()) ?? throw new InvalidOperationException("Reporting project directory could not be resolved.");

    private static string GetRepositoryRoot()
        => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
}
