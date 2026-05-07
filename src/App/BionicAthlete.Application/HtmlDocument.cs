namespace BionicAthlete.Application;

using BionicAthlete.Application.Exporting;
using BionicCode.Utilities.Net;

public readonly record struct HtmlDocument
{
    public HtmlDocument(
    string content,
    PagePreset pagePreset,
    int reportSchemaVersion,
    string rendererVersion)
    {
        ArgumentExceptionAdvanced.ThrowIfTrue(reportSchemaVersion <= 0, nameof(reportSchemaVersion), "Report schema version must be a positive integer.");
        ArgumentNullExceptionAdvanced.ThrowIfNullOrWhiteSpace(rendererVersion);
        ArgumentNullExceptionAdvanced.ThrowIfNullOrWhiteSpace(content);
        ArgumentExceptionAdvanced.ThrowIfEnumIsNotDefined<PagePreset>(pagePreset);
        ArgumentExceptionAdvanced.ThrowIfEnumEqualsAny(pagePreset, [PagePreset.Undefined]);

        Content = content;
        PagePreset = pagePreset;
        ReportSchemaVersion = reportSchemaVersion;
        RendererVersion = rendererVersion;
    }

    public static readonly HtmlDocument Default = new(string.Empty, PagePreset.Undefined, -1, string.Empty);

    public string Content { get; init; }
    public PagePreset PagePreset { get; init; }
    public int ReportSchemaVersion { get; init; }
    public string RendererVersion { get; init; }
};
