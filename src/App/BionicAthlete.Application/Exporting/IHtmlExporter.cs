namespace BionicAthlete.Application.Exporting;

public interface IHtmlExporter
{
    void Export(HtmlExporterArgs args);
    Task ExportAsync(HtmlExporterArgs args, CancellationToken cancellationToken);
}
