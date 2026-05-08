namespace BionicAthlete.Application.Reporting;

public class PdfExportRequestEventArgs : EventArgs
{
    private readonly TaskCompletionSource _taskCompletionSource;
    private bool _handled;

    public PdfExportRequestEventArgs(PdfExportResult pdfExportResult, TaskCompletionSource taskCompletionSource)
    {
        PdfExportResult = pdfExportResult;
        _taskCompletionSource = taskCompletionSource;
    }

    public PdfExportResult PdfExportResult { get; }
    public bool IsSuccessful => PdfExportResult.IsSuccessful;

    public bool Handled
    {
        get => _handled;
        set
        {
            _handled = value;
            if (_handled)
            {
                _ = _taskCompletionSource.TrySetResult();
            }
        }
    }
}