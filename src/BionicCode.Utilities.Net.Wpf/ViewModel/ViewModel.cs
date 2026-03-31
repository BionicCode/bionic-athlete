namespace BionicCode.Utilities.Net;

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;

/// <summary>
/// Base class recommended to use for view models across the application. Encapsulates implementations of <see cref="INotifyPropertyChanged"/> and <see cref="INotifyDataErrorInfo"/>.
/// </summary>
public abstract class ViewModel : ViewModelCommon, IViewModel
{
    protected ViewModel()
    {
        _progressDataCollectionInternal = [];
        ProgressDataCollection = new ReadOnlyObservableCollection<ObservableProgressData>(_progressDataCollectionInternal);
    }

    #region IProgressReporter
    public ReadOnlyObservableCollection<ObservableProgressData> ProgressDataCollection { get; }
    private readonly ObservableCollection<ObservableProgressData> _progressDataCollectionInternal;
    private ObservableProgressData _selectedProgress;

    public virtual ObservableProgressData SelectedProgress
    {
        get => _selectedProgress;
        protected set => TrySetValue(value, ref _selectedProgress);
    }

    /// <summary>
    /// Creates a <see cref="IProgress{T}"/> instance that is associated with the caller's thread.
    /// The registered progress callback is the virtual <see cref="ViewModelCommon.OnProgress(ProgressData)"/> member.
    /// </summary>
    /// <remarks>The returned <see cref="IProgress{T}"/> instance is associated with the application's primary dispatcher thread. Progress is always reported to the UI thread that is associated with the <c>Dispatcher</c> returned by <c>Application.Current.Dispatcher</c>.</remarks>
    /// <returns>A <see cref="IProgress{ProgressData}"/> instance that always posts progress to the UI thread.</returns>
    [Obsolete("This method is deprecated. Use 'StartNewObservableProgressReporting' instead.", error: false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public IProgress<ProgressData> CreateProgressReporterFromUiThread() => Application.Current.Dispatcher.Invoke(() => new Progress<ProgressData>(OnProgress));

    /// <summary>
    /// Starts a new progress reporting operation by creating a new instance of <see cref="ObservableProgressData"/> with the specified parameters 
    /// and adding it to the internal collection and assigns it to the <see cref="SelectedProgress"/> property.
    /// </summary>
    /// <remarks>The returned <see cref="IProgress{T}"/> instance is associated with the captured context. 
    /// If <see cref="StartNewObservableProgressReporting(string, string, double)"/> is called 
    /// on the UI thread then the captured context is the <see cref="Dispatcher"/> thread and progress is always reported to that UI thread.
    /// <para/>The method actually returns a <see cref="ObservableProgressReporter"/> instance. 
    /// The instance is casted to the return type <see cref="IProgress{ProgressData}"/> to enforce MVVM compliance when the reporter is handed over to the model.
    /// <para/>Setting progress on the returned <see cref="ObservableProgressReporter"/> instance will update the <see cref="SelectedProgress"/> property 
    /// and notify any observers via the <see cref="ProgressChanged"/> event.</remarks>
    /// <param name="initialMessage">The initial message for the progress operation.</param>
    /// <param name="operationTitle">The title of the progress operation.</param>
    /// <param name="maxValue">The maximum value for the progress operation.</param>
    /// <param name="isIndeterminate">Indicates whether the progress operation is indeterminate.</param>
    /// <returns>An <see cref="ObservableProgressReporter"/> instance casted to <see cref="IProgress{ProgressData}"/> instance that reports progress to the UI thread.</returns>
    public IProgress<ProgressData> StartNewObservableProgressReporting(string initialMessage = "", string operationTitle = "", double maxValue = 100, bool isIndeterminate = false)
    {
        var progressData = new ObservableProgressData(0, maxValue, initialMessage, operationTitle) { IsIndeterminate = isIndeterminate };
        SelectedProgress = progressData;
        _progressDataCollectionInternal.Add(progressData);

        var reporter = new ObservableProgressReporter(OnProgress, progressData);
        reporter.ProgressReported += OnObservableProgressReporterProgressReported;
        reporter.Completed += OnObservableProgressReporterCompleted;

        return reporter;
    }

    /// <summary>
    /// Starts a new progress reporting operation by creating a new instance of <see cref="ObservableProgressData"/> with the specified parameters 
    /// and adding it to the internal collection and assigns it to the <see cref="SelectedProgress"/> property.
    /// </summary>
    /// <remarks>The returned <see cref="IProgress{T}"/> instance is associated with the captured context. 
    /// If <see cref="StartNewObservableProgressReporting(Action{ObservableProgressData}, string, string, double)"/> is called 
    /// on the UI thread then the captured context is the <see cref="Dispatcher"/> thread and progress is always reported to that UI thread.
    /// <para/>The method actually returns a <see cref="ObservableProgressReporter"/> instance. 
    /// The instance is casted to the return type <see cref="IProgress{ProgressData}"/> to enforce MVVM compliance when the reporter is handed over to the model.
    /// <para/>Setting progress on the returned <see cref="ObservableProgressReporter"/> instance will update the <see cref="SelectedProgress"/> property 
    /// and notify any observers via the <see cref="ProgressChanged"/> event.</remarks>
    /// <param name="initialMessage">The initial message for the progress operation.</param>
    /// <param name="operationTitle">The title of the progress operation.</param>
    /// <param name="maxValue">The maximum value for the progress operation.</param>
    /// <param name="onProgress">An additional progress callback that is invoked when progress is reported. The <see cref="ProgressChanged"/> event is also raised.</param>
    /// <param name="isIndeterminate">Indicates whether the progress operation is indeterminate.</param>
    /// <returns>An <see cref="ObservableProgressReporter"/> instance casted to <see cref="IProgress{ProgressData}"/> instance that reports progress to the UI thread.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the <paramref name="onProgress"/> argument is <see langword="null"/>.</exception>"
    public IProgress<ProgressData> StartNewObservableProgressReporting(Action<ObservableProgressData> onProgress, string initialMessage = "", string operationTitle = "", double maxValue = 100, bool isIndeterminate = false)
    {
        ArgumentNullExceptionAdvanced.ThrowIfNull(onProgress);

        var progressData = new ObservableProgressData(0, maxValue, initialMessage, operationTitle) { IsIndeterminate = isIndeterminate };
        SelectedProgress = progressData;
        _progressDataCollectionInternal.Add(progressData);

        Action<ObservableProgressData> reportAction = onProgress + OnProgress;

        var reporter = new ObservableProgressReporter(reportAction, progressData);
        reporter.ProgressReported += OnObservableProgressReporterProgressReported;
        reporter.Completed += OnObservableProgressReporterCompleted;

        return reporter;
    }

    private void OnObservableProgressReporterCompleted(object? sender, EventArgs e)
    {
        var reporter = (ObservableProgressReporter)sender!;
        reporter.ProgressReported -= OnObservableProgressReporterProgressReported;
        reporter.Completed -= OnObservableProgressReporterCompleted;
    }

    private void OnObservableProgressReporterProgressReported(object? sender, ObservableProgressChangedEventArgs e) => OnProgressChanged(e.OldValue, e.ProgressData);

    public ObservableProgressData RemoveObservableProgressData(int index)
    {
        ObservableProgressData progressData = _progressDataCollectionInternal[index];
        _progressDataCollectionInternal.RemoveAt(index);
        return progressData;
    }

    public void RemoveAllCompletedObservableProgressData()
    {
        foreach (ObservableProgressData? progressData in _progressDataCollectionInternal
            .Where(progressData => progressData.Progress >= 1.0).ToList())
        {
            _ = _progressDataCollectionInternal.Remove(progressData);
        }

        SelectedProgress = new ObservableProgressData(0, -1, string.Empty, string.Empty);
    }

    public void RemoveAllObservableProgressData() => _progressDataCollectionInternal.Clear();

    /// <summary>
    /// When overridden, handles the <see cref="IProgress{ObservableProgressData}.Report(ObservableProgressData)"/> 
    /// that is invoked by the <see cref="IProgress{ObservableProgressData}"/> instance 
    /// returned from <see cref="CreateProgressReporterFromCurrentThread"/>. 
    /// Can be used as progress delegate for any <see cref="IProgress{ObservableProgressData}"/>.
    /// </summary>
    /// <param name="progress">The progress argument.</param>
    /// <remarks>The default implementation provides the following logic: a value of <see cref="double.NegativeInfinity"/> 
    /// or <see cref="ViewModelCommon.DisableIndeterminateMode"/> will automatically 
    /// set the <see cref="ViewModelCommon.IsIndeterminate"/> property to <see langword="false"/>. 
    /// A value of <see cref="double.PositiveInfinity"/> or <see cref="ViewModelCommon.EnableIndeterminateMode"/> will automatically 
    /// set the <see cref="ViewModelCommon.IsIndeterminate"/> property to <see langword="true"/>.
    /// </remarks>
    public virtual void OnProgress(ObservableProgressData progress)
    {
        ArgumentNullExceptionAdvanced.ThrowIfNull(progress);

        if (SelectedProgress != progress)
        {
            SelectedProgress = progress;
        }
    }

    /// <inheritdoc/>
    public event EventHandler<ObservableProgressChangedEventArgs>? ProgressChanged;

    /// <summary>
    /// Indicates ongoing progress reporting 
    /// </summary>
    /// <remarks>Raises <see cref="INotifyPropertyChanged.PropertyChanged"/> event.</remarks>
    private bool IsReportingProgress => SelectedProgress is not null && !SelectedProgress.IsCompleted;

    #endregion IProgressReporter

    /// <summary>
    /// Raises the <see cref="IProgressReporterCommon.ProgressChanged"/> event.
    /// </summary>
    /// <param name="oldValue">The old progress value.</param>
    /// <param name="newValue">The new progress value.</param>
    /// <param name="maxValue">The maximum progress value.</param>
    /// <param name="progressText">The progress message.</param>
    protected virtual void OnProgressChanged(double oldValue, ObservableProgressData progressData) => ProgressChanged?.Invoke(this, new ObservableProgressChangedEventArgs(oldValue, progressData));
}
