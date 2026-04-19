namespace BionicCode.Utilities.Net;

using System;
using System.Diagnostics;

internal class ObservableProgressReporter : IProgress<ProgressData>
{
    private readonly ObservableProgressData _observableProgressData;
    private readonly SendOrPostCallback _invokeReportProgress;
    private readonly Action<ObservableProgressData>? _reportAction;
    private readonly SynchronizationContext _synchronizationContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="ObservableProgressReporter"/> class 
    /// that creates a new <see cref="ObservableProgressData"/> instance which can be obtained via the <see cref="ProgressData"/> property.
    /// </summary>
    /// <remarks>This constructor prepares the progress reporter for use. It captures the current <see cref="SynchronizationContext"/> 
    /// and uses this to forward the changes to ensure the <see cref="ProgressData"/> is always updated on the correct thread. 
    /// For UI context this thread should be the UI  thread (dispatcher thread). 
    /// <para/>After instantiation, progress updates can be reported and observed by subscribers of the <see cref="ProgressReported"/> event or 
    /// by binding to a reference of the <see cref="ObservableProgressData"/> returned by the <see cref="ProgressData"/> property.
    /// <para/>The created <see cref="ObservableProgressData"/> instance is using '1' as <see cref="ObservableProgressData.MaxValue"/> 
    /// and has <see cref="string.Empty"/> as value for the <see cref="ObservableProgressData.OperationTitle"/>.</remarks>
    public ObservableProgressReporter()
    {
        // Cache the callback and state for use when reporting progress.
        _invokeReportProgress = ReportProgress;

        // Capture the current synchronization context.
        // If there is no current context, we use a default instance targeting the ThreadPool.
        _synchronizationContext = SynchronizationContext.Current ?? new SynchronizationContext();

        _observableProgressData = new ObservableProgressData(0, 1, string.Empty, string.Empty);
        _reportAction = null;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ObservableProgressReporter"/> class using the specified <see cref="ObservableProgressData"/> instance.
    /// </summary>
    /// <remarks>This constructor prepares the progress reporter for use using the provided <paramref name="progressData"/>. 
    /// It captures the current <see cref="SynchronizationContext"/> and uses this to forward the changes 
    /// to ensure the <see cref="ProgressData"/> is always updated on the correct thread. 
    /// For UI context this thread should be the UI  thread (dispatcher thread). 
    /// <para/>After instantiation, progress updates can be reported and observed by subscribers of the <see cref="ProgressReported"/> event or 
    /// by binding to a reference of the <see cref="ObservableProgressData"/> returned by the <see cref="ProgressData"/> property.
    /// <para/>The created <see cref="ObservableProgressData"/> instance is using '1' as <see cref="ObservableProgressData.MaxValue"/> 
    /// and has <see cref="string.Empty"/> as value for the <see cref="ObservableProgressData.OperationTitle"/>.</remarks>
    /// <param name="progressData">The <see cref="ObservableProgressData"/> instance that will be used to track and report progress. Cannot be <see langword="null"/>.</param>
    public ObservableProgressReporter(ObservableProgressData progressData)
    {
        ArgumentNullExceptionAdvanced.ThrowIfNull(progressData);

        _observableProgressData = progressData;

        // Cache the callback and state for use when reporting progress.
        _invokeReportProgress = ReportProgress;

        // Capture the current synchronization context.
        // If there is no current context, we use a default instance targeting the ThreadPool.
        _synchronizationContext = SynchronizationContext.Current ?? new SynchronizationContext();

        _reportAction = null;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ObservableProgressReporter"> class with the specified reporting action and
    /// progress data.
    /// </summary>
    /// <remarks>The constructor captures the current synchronization context to ensure that the provided <paramref name="reportAction"/> callback 
    /// and the updating of the provided <paramref name="progressData"/> is invoked on the correct thread, which is especially important for UI applications. 
    /// <brt/>For UI context this thread should be the UI  thread (dispatcher thread). 
    /// <br/>This allows progress updates to be marshaled back to the UI thread, 
    /// ensuring that any UI-bound properties or controls that are updated in response to progress changes
    /// are reported on the appropriate thread. If no synchronization context is available, a default context targeting
    /// the <see cref="ThreadPool"/> is used.</remarks>
    /// <param name="reportAction">The action to invoke when progress is reported. This delegate receives the current progress data as its
    /// parameter and cannot be <see langword="null"/>.</param>
    /// <param name="progressData">The <see cref="ObservableProgressData"/> instance that holds the progress information to be reported. This parameter cannot be
    /// <see langword="null"/>.</param>
    public ObservableProgressReporter(Action<ObservableProgressData> reportAction, ObservableProgressData progressData)
    {
        ArgumentNullExceptionAdvanced.ThrowIfNull(reportAction);
        ArgumentNullExceptionAdvanced.ThrowIfNull(progressData);

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
        _reportAction?.Invoke(_observableProgressData);
        OnProgressReported(oldProgress, _observableProgressData.Progress, _observableProgressData.Message);
    }

    protected virtual void OnReport(ProgressData value) => _synchronizationContext.Post(_invokeReportProgress, value);

    void IProgress<ProgressData>.Report(ProgressData value) => OnReport(value);

    protected virtual void OnProgressReported(double oldProgress, double newProgress, string message) => ProgressReported?.Invoke(this, new ObservableProgressChangedEventArgs(oldProgress, _observableProgressData));
    protected virtual void OnCompleted() => Completed?.Invoke(this, EventArgs.Empty);
    public event EventHandler<ObservableProgressChangedEventArgs>? ProgressReported;
    public event EventHandler? Completed;
    public ObservableProgressData ProgressData => _observableProgressData;
}
