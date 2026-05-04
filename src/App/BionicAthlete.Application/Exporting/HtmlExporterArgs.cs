namespace BionicAthlete.Application.Exporting;

using BionicAthlete.Application.Reporting.Html;

public abstract class HtmlExporterArgs
{
    protected HtmlExporterArgs(HtmlDocument document) => Document = document;

    public HtmlDocument Document { get; init; }
}
