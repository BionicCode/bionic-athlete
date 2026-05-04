namespace BionicAthlete.Training.Application.Reporting;

using System.Collections.Immutable;
using BionicAthlete.Application.Reporting;
using BionicAthlete.FileSystem.Abstractions;
using BionicCode.Utilities.Net;

public class FitActivityReportManifestHandler : ReportManifestHandler, IFitActivityReportManifestHandler
{
    public override ReportManifest CreateManifest(
        ReportInfo reportInfo,
        bool includePdfArtifact)
    {
        ArgumentNullExceptionAdvanced.ThrowIfNull(reportInfo);

        ImmutableArray<ReportManifestArtifact>.Builder artifacts = ImmutableArray.CreateBuilder<ReportManifestArtifact>();
        artifacts.Add(new ReportManifestArtifact("HtmlReport", "activity-report.html", "text/html"));
        artifacts.Add(new ReportManifestArtifact("ReportManifest", "report-manifest.json", "application/json"));
        if (includePdfArtifact)
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

    public override ReportManifest UpdateManifest(ReportManifest currentManifest)
    {
        ArgumentNullException.ThrowIfNull(currentManifest);

        ImmutableArray<ReportManifestArtifact>.Builder artifacts = ImmutableArray.CreateBuilder<ReportManifestArtifact>();
        artifacts.AddRange(currentManifest.Artifacts.Where(static artifact => artifact.ArtifactKind != "PdfReport"));
        artifacts.Add(new ReportManifestArtifact("PdfReport", "activity-report.pdf", "application/pdf"));

        return currentManifest with { Artifacts = artifacts.ToImmutable() };
    }
}