namespace BionicAthlete.Application.Exporting;

using System.Text;
using BionicAthlete.Application;

public class HtmlFileExporterArgsFactory : IHtmlExporterArgsFactory
{
    public HtmlExporterArgs Create(HtmlDocument document, Uri destinationUri, bool isOverWriteExistingAllowed, Encoding? encoding) => new HtmlFileExporterArgs(document, destinationUri, isOverWriteExistingAllowed, encoding);
}