namespace BionicAthlete.Application.Reporting;

using System.Collections.Immutable;
using BionicAthlete.Application;
using BionicAthlete.Application.Exporting;
using BionicCode.Utilities.Net;

public sealed class ReportDescriptor
{
    public ReportDescriptor(
    int reportSchemaVersion,
    string rendererVersion,
    string reportId,
    string sourceFilePath,
    DateTimeOffset generatedAtUtc,
    ReportOutputTarget outputTarget,
    PagePreset pagePreset,
    ImmutableArray<string> sectionIds,
    ImmutableArray<ReportDiagnostic> diagnostics)
    {
        ArgumentExceptionAdvanced.ThrowIfTrue(reportSchemaVersion <= 0, nameof(reportSchemaVersion), "Report schema version must be a positive integer.");
        ArgumentNullExceptionAdvanced.ThrowIfNullOrWhiteSpace(rendererVersion);
        ArgumentNullExceptionAdvanced.ThrowIfNullOrWhiteSpace(reportId);
        ArgumentNullExceptionAdvanced.ThrowIfNullOrWhiteSpace(sourceFilePath);
        ArgumentExceptionAdvanced.ThrowIfEnumIsNotDefined<ReportOutputTarget>(outputTarget);
        ArgumentExceptionAdvanced.ThrowIfEnumEqualsAny(outputTarget, [ReportOutputTarget.Undefined]);
        ArgumentExceptionAdvanced.ThrowIfEnumIsNotDefined<PagePreset>(pagePreset);
        ArgumentExceptionAdvanced.ThrowIfEnumEqualsAny(pagePreset, [PagePreset.Undefined]);
        ArgumentNullExceptionAdvanced.ThrowIfNull(sectionIds);
        ArgumentNullExceptionAdvanced.ThrowIfNull(diagnostics);

        ReportSchemaVersion = reportSchemaVersion;
        RendererVersion = rendererVersion;
        ReportId = reportId;
        SourceFilePath = sourceFilePath;
        GeneratedAtUtc = generatedAtUtc;
        OutputTarget = outputTarget;
        PagePreset = pagePreset;
        SectionIds = sectionIds;
        Diagnostics = diagnostics;
    }

    public static ReportDescriptor Create(HtmlDocument reportHtmlDocument, Report report, ReportOutputTarget outputTarget)
    {
        ArgumentNullExceptionAdvanced.ThrowIfDefault(reportHtmlDocument);
        ArgumentNullExceptionAdvanced.ThrowIfNull(report);
        ArgumentExceptionAdvanced.ThrowIfEnumIsNotDefined<ReportOutputTarget>(outputTarget);
        ArgumentExceptionAdvanced.ThrowIfEnumEqualsAny<ReportOutputTarget>(outputTarget, [ReportOutputTarget.Undefined]);

        return new ReportDescriptor(
            reportHtmlDocument.ReportSchemaVersion,
            reportHtmlDocument.RendererVersion,
            report.ReportId,
            report.SourceFilePath,
            report.GeneratedAtUtc,
            outputTarget,
            reportHtmlDocument.PagePreset,
            report.Sections.Select(section => section.Id).ToImmutableArray(),
            report.Diagnostics);
    }

    public int ReportSchemaVersion { get; init; }
    public string RendererVersion { get; init; }
    public string ReportId { get; init; }
    public string SourceFilePath { get; init; }
    public DateTimeOffset GeneratedAtUtc { get; init; }
    public ReportOutputTarget OutputTarget { get; init; }
    public PagePreset PagePreset { get; init; }
    public ImmutableArray<ReportManifestArtifact> Artifacts { get; init; }
    public ImmutableArray<string> SectionIds { get; init; }
    public ImmutableArray<ReportDiagnostic> Diagnostics { get; init; }
};