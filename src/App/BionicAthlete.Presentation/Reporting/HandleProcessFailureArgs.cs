namespace BionicAthlete.Presentation.Reporting;

using BionicAthlete.Application.Reporting;

public sealed partial class WebView2PdfExporter
{
    internal readonly record struct HandleProcessFailureArgs(
        PdfExportRequest Request,
        WebView2StatusReport StatusReport,
        bool CanRetry,
        string ExportSubject,
        CancellationToken CancellationToken);

}
