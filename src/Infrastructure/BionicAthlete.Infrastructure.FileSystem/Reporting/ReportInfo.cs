namespace BionicAthlete.Infrastructure.FileSystem.Reporting;

using System.Collections.Immutable;
public sealed record ReportInfo(
    int ReportSchemaVersion,
    string RendererVersion,
    string ReportId,
    string SourceFilePath,
    DateTimeOffset GeneratedAtUtc,
    ReportOutputTarget OutputTarget,
    ReportPagePreset PagePreset,
    ImmutableArray<ReportManifestArtifact> Artifacts,
    ImmutableArray<string> SectionIds,
    ImmutableArray<ReportDiagnostic> Diagnostics);