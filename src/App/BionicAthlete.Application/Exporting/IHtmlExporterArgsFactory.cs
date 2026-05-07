namespace BionicAthlete.Application.Exporting;

using System.Text;
using BionicAthlete.Application;

public interface IHtmlExporterArgsFactory
{
    HtmlExporterArgs Create(HtmlDocument document, Uri destinationUri, bool isOverWriteExistingAllowed, Encoding? encoding);
}