namespace BionicAthlete.Application.Reporting.Html;

public readonly record struct HtmlDocument(
    string Content)
{
    public static readonly HtmlDocument Default = new HtmlDocument() { Content = string.Empty };
};
