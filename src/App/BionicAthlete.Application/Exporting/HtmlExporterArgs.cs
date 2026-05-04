namespace BionicAthlete.Application.Exporting;

using BionicAthlete.Application.Reporting.Html;

public abstract class HtmlExporterArgs
{
    protected HtmlExporterArgs(HtmlDocument document, Uri destinationUri)
    {
        Document = document;
        DestinationUri = destinationUri;
    }

    public HtmlDocument Document { get; init; }
    public Uri DestinationUri { get; init; }
}
