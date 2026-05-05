namespace BionicAthlete.Application.Reporting.Html;

using BionicCode.Utilities.Net;

public readonly record struct HtmlDocument
{
    public HtmlDocument(
    string content,
    ReportPagePreset pagePreset,
    int reportSchemaVersion,
    string rendererVersion)
    {
        ArgumentExceptionAdvanced.ThrowIfTrue(reportSchemaVersion <= 0, nameof(reportSchemaVersion), "Report schema version must be a positive integer.");
        ArgumentNullExceptionAdvanced.ThrowIfNullOrWhiteSpace(rendererVersion);
        ArgumentNullExceptionAdvanced.ThrowIfNullOrWhiteSpace(content);
        ArgumentExceptionAdvanced.ThrowIfEnumIsNotDefined<ReportPagePreset>(pagePreset);
        ArgumentExceptionAdvanced.ThrowIfEnumEqualsAny(pagePreset, [ReportPagePreset.Undefined]);
        
        Content = content;
        PagePreset = pagePreset;
        ReportSchemaVersion = reportSchemaVersion;
        RendererVersion = rendererVersion;
    }

    public static readonly HtmlDocument Default = new HtmlDocument(string.Empty, ReportPagePreset.Undefined, -1, string.Empty);

    public string Content { get; init; }
    public ReportPagePreset PagePreset { get; init; }
    public int ReportSchemaVersion { get; init; }
    public string RendererVersion { get; init; }
};
