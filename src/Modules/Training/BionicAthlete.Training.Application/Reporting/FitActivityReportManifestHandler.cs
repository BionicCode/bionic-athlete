namespace BionicAthlete.Training.Application.Reporting;

using System.Collections.Immutable;
using BionicAthlete.Application.Reporting;
using BionicAthlete.FileSystem.Abstractions;
using BionicCode.Utilities.Net;

public class FitActivityReportManifestHandler : ReportManifestHandler, IFitActivityReportManifestHandler
{
    public override ReportManifest CreateManifest(
        ReportInfo reportInfo,
        string htmlReportFileName)
    {
        ArgumentNullExceptionAdvanced.ThrowIfNull(reportInfo);

        ImmutableArray<ReportManifestArtifact>.Builder artifacts = ImmutableArray.CreateBuilder<ReportManifestArtifact>();
        artifacts.Add(new ReportManifestArtifact("HtmlReport", htmlReportFileName, "text/html"));
        artifacts.Add(new ReportManifestArtifact("ReportManifest", ArtifactInfo.ManifestFileName, "application/json"));
        
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

    public override ReportManifest UpdateManifest(ReportManifest currentManifest, ReportManifestArtifact reportManifestArtifact)
    {
        ArgumentNullException.ThrowIfNull(currentManifest);

        ImmutableArray<ReportManifestArtifact>.Builder artifacts = ImmutableArray.CreateBuilder<ReportManifestArtifact>();
        artifacts.AddRange(currentManifest.Artifacts.Where(static artifact => artifact.ArtifactKind != "PdfReport"));
        artifacts.Add(reportManifestArtifact);

        return currentManifest with { Artifacts = artifacts.ToImmutable() };
    }
}