namespace BionicAthlete.Application.Reporting;

using System.Collections.Immutable;

public static class ReportManifestCreator
{
    public static ReportManifest CreateManifest(
        ReportInfo reportInfo,
        bool includePdfArtifact)
    {
        ImmutableArray<ReportManifestArtifact>.Builder artifacts = ImmutableArray.CreateBuilder<ReportManifestArtifact>();
        artifacts.Add(new ReportManifestArtifact("HtmlReport", "activity-report.html", "text/html"));
        artifacts.Add(new ReportManifestArtifact("ReportManifest", "report-manifest.json", "application/json"));
        if (includePdfArtifact && !string.IsNullOrWhiteSpace(package.PdfFilePath))
        {
            artifacts.Add(new ReportManifestArtifact("PdfReport", "activity-report.pdf", "application/pdf"));
        }

        return new ReportManifest(
            reportInfo.ReportSchemaVersion,
            reportInfo.RendererVersion,
            reportInfo.ReportId,
            reportInfo.SourceFilePath,
            reportInfo.GeneratedAtUtc,
            reportInfo.OutputTarget,
            reportInfo.PagePreset,
            artifacts.ToImmutable(),
            reportInfo.SectionIds,
            reportInfo.Diagnostics);
    }

    public static ReportManifest CreateManifestForUpdate(string reportFilePath, ReportManifest currentManifest)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reportFilePath);
        ArgumentNullException.ThrowIfNull(currentManifest);

        ImmutableArray<ReportManifestArtifact>.Builder artifacts = ImmutableArray.CreateBuilder<ReportManifestArtifact>();
        artifacts.AddRange(currentManifest.Artifacts.Where(static artifact => artifact.ArtifactKind != "PdfReport"));
        artifacts.Add(new ReportManifestArtifact("PdfReport", "activity-report.pdf", "application/pdf"));

        return currentManifest with { Artifacts = artifacts.ToImmutable() };
    }
}