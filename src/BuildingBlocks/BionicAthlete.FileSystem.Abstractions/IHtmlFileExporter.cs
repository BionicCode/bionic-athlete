namespace BionicAthlete.FileSystem.Abstractions;

using BionicAthlete.Application.Exporting;

public interface IHtmlFileExporter : IHtmlExporter
{
    void Export(HtmlFileExporterArgs args);
    Task ExportAsync(HtmlFileExporterArgs args, CancellationToken cancellationToken);
}
