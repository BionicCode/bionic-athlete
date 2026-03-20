namespace BionicCode.Utilities.Net;

using System;
using System.Diagnostics;

internal class ObservableProgressReporter : IProgress<ProgressData>
{
    private readonly ObservableProgressData _observableProgressData;
    private readonly SendOrPostCallback _invokeReportProgress;
    private readonly Action<ObservableProgressData> _reportAction;
    private readonly SynchronizationContext _synchronizationContext;

    public ObservableProgressReporter(Action<ObservableProgressData> reportAction, ObservableProgressData progressData)
    {
        ArgumentNullExceptionAdvanced.ThrowIfNull(reportAction);

        // Capture the current synchronization context.
        // If there is no current context, we use a default instance targeting the ThreadPool.
        _synchronizationContext = SynchronizationContext.Current ?? new SynchronizationContext();
        Debug.Assert(_synchronizationContext != null);

        // Cache the callback and state for use when reporting progress.
        _invokeReportProgress = ReportProgress;

        _reportAction = reportAction;
        _observableProgressData = progressData;
    }

    private void ReportProgress(object? state)
    {
        var progressData = (ProgressData)state!;
        double oldProgress = _observableProgressData.Progress;
        _observableProgressData.Update(progressData);
        _reportAction.Invoke(_observableProgressData);
        OnProgressReported(oldProgress, _observableProgressData.Progress, _observableProgressData.Message);
    }

    protected virtual void OnReport(ProgressData value) => _synchronizationContext.Post(_invokeReportProgress, value);

    void IProgress<ProgressData>.Report(ProgressData value) => OnReport(value);

    protected virtual void OnProgressReported(double oldProgress, double newProgress, string message) => ProgressReported?.Invoke(this, new ObservableProgressChangedEventArgs(oldProgress, newProgress, message, _observableProgressData));
    public event EventHandler<ObservableProgressChangedEventArgs>? ProgressReported;
}
