namespace BionicAthlete.FileSystem.Abstractions;

using System.Text;
using BionicAthlete.Application.Exporting;
using BionicAthlete.Application.Reporting.Html;

public interface IHtmlExporterArgsFactory
{
    HtmlExporterArgs Create(HtmlDocument document, Uri destinationUri, bool isOverWriteExistingAllowed, Encoding? encoding);
}