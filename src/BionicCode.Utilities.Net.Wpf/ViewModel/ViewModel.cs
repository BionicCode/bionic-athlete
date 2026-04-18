namespace BionicCode.Utilities.Net;

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using System.Windows.Threading;

/// <summary>
/// Base class recommended to use for view models across the application. Encapsulates implementations of <see cref="INotifyPropertyChanged"/> and <see cref="INotifyDataErrorInfo"/>.
/// </summary>
public abstract class ViewModel : ViewModelCommon, IViewModel
{
    private readonly object _progressDataCollectionSyncRoot = new();
    protected ViewModel()
    {
        _progressDataCollectionInternal = [];
        Application.Current.Dispatcher.Invoke(() => BindingOperations.EnableCollectionSynchronization(_progressDataCollectionInternal, _progressDataCollectionSyncRoot), DispatcherPriority.Send);
        ProgressDataCollection = new ReadOnlyObservableCollection<ObservableProgressData>(_progressDataCollectionInternal);
        _selectedProgressIndex = 0;
    }

    #region IProgressReporter
    public ReadOnlyObservableCollection<ObservableProgressData> ProgressDataCollection { get; }
    public bool HasProgressDataCollectionItems => ProgressDataCollection.Count > 0;
    public bool IsProgressDataCollectionEmpty => ProgressDataCollection.Count == 0;
    public bool HasProgressData => HasProgressDataCollectionItems || SelectedProgress is not null;
    private readonly ObservableCollection<ObservableProgressData> _progressDataCollectionInternal;
    private ObservableProgressData? _selectedProgress;
    private int _selectedProgressIndex;

    public virtual ObservableProgressData? SelectedProgress
    {
        get => _selectedProgress;
        private set => TrySetValue(value, ref _selectedProgress);
    }

    /// <summary>
    /// Sets the selected <see cref="ObservableProgressData"/> item in the <see cref="ProgressDataCollection"/> by the specified index and assigns it to the <see cref="SelectedProgress"/> property."/>
    /// </summary>
    /// <remarks>Setting this property will update the <see cref="SelectedProgress"/> property and raise the <see cref="HasProgressData"/> property changed notification.
    /// <para/>The index specifies the default position that is mapped to the <see cref="SelectedProgress"/>. For example, if an items gets removed from or added to the <see cref="ProgressDataCollection"/> at the current <see cref="SelectedProgressIndex"/> index, the new item will be selected automatically based on that index.</remarks>
    /// <value>The index of the selected progress data item in the <see cref="ProgressDataCollection"/>. A value of -1 indicates that no item is selected and <see langword="null" /> is assigned to the <see cref="SelectedProgress"/> property. The default value is 0 resulting in always the first item being selected if available.</value>
    public virtual int SelectedProgressIndex
    {
        get => _selectedProgressIndex;
        private set
        {
            if (TrySetValue(value, ref _selectedProgressIndex))
            {
                SetSelectedProgress(value);
            }
        }
    }

    private void SetSelectedProgress(int progressDataIndex)
    {
        SelectedProgress = progressDataIndex >= 0 && progressDataIndex < _progressDataCollectionInternal.Count
            ? _progressDataCollectionInternal[progressDataIndex]
            : null;
        OnPropertyChanged(nameof(HasProgressData));
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
    /// <param name="isCapturingUiThread">Indicates whether the progress operation is capturing the current UI thread.</param>
    /// <returns>An <see cref="ObservableProgressReporter"/> instance casted to <see cref="IProgress{ProgressData}"/> instance that reports progress to the UI thread.</returns>
    public IProgress<ProgressData> StartNewObservableProgressReporting(string initialMessage = "", string operationTitle = "", double maxValue = 1, bool isIndeterminate = false, bool isCapturingUiThread = true)
    {
        var progressData = new ObservableProgressData(0, maxValue, initialMessage, operationTitle) { IsIndeterminate = isIndeterminate };
        Application.Current.Dispatcher.Invoke(() => _progressDataCollectionInternal.Add(progressData));
        UpdateSelectedProgressData();

        ObservableProgressReporter reporter = isCapturingUiThread
            ? Application.Current.Dispatcher.Invoke(() => new ObservableProgressReporter(OnProgress, progressData))
            : new ObservableProgressReporter(OnProgress, progressData);
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
    /// <param name="isCapturingUiThread">Indicates whether the progress operation is capturing the current UI thread.</param>  
    /// <returns>An <see cref="ObservableProgressReporter"/> instance casted to <see cref="IProgress{ProgressData}"/> instance that reports progress to the UI thread.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the <paramref name="onProgress"/> argument is <see langword="null"/>.</exception>"
    public IProgress<ProgressData> StartNewObservableProgressReporting(Action<ObservableProgressData> onProgress, string initialMessage = "", string operationTitle = "", double maxValue = 100, bool isIndeterminate = false, bool isCapturingUiThread = true)
    {
        ArgumentNullExceptionAdvanced.ThrowIfNull(onProgress);

        var progressData = new ObservableProgressData(0, maxValue, initialMessage, operationTitle) { IsIndeterminate = isIndeterminate };
        Application.Current.Dispatcher.Invoke(() => _progressDataCollectionInternal.Add(progressData));
        UpdateSelectedProgressData();

        Action<ObservableProgressData> reportAction = onProgress + OnProgress;

        ObservableProgressReporter reporter = isCapturingUiThread
            ? Application.Current.Dispatcher.Invoke(() => new ObservableProgressReporter(reportAction, progressData))
            : new ObservableProgressReporter(reportAction, progressData);
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
        ArgumentOutOfRangeExceptionAdvanced.ThrowIfIndexOutOfRange(index, _progressDataCollectionInternal);

        ObservableProgressData progressData = _progressDataCollectionInternal[index];
        _progressDataCollectionInternal.RemoveAt(index);
        UpdateSelectedProgressData();

        return progressData;
    }

    private void UpdateSelectedProgressData()
    {
        if (_progressDataCollectionInternal.Count > SelectedProgressIndex)
        {
            SetSelectedProgress(SelectedProgressIndex);
        }
        else
        {
            SetSelectedProgress(-1);
        }
    }

    public void RemoveAllCompletedObservableProgressData()
    {
        foreach (ObservableProgressData? progressData in _progressDataCollectionInternal
            .Where(progressData => progressData.Progress >= 1.0).ToList())
        {
            _ = _progressDataCollectionInternal.Remove(progressData);
        }

        UpdateSelectedProgressData();
    }

    public void RemoveAllObservableProgressData()
    {
        _progressDataCollectionInternal.Clear();
        UpdateSelectedProgressData();
    }

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
    public new event EventHandler<ObservableProgressChangedEventArgs>? ProgressChanged;

    /// <summary>
    /// Indicates ongoing progress reporting 
    /// </summary>
    /// <remarks>Raises <see cref="INotifyPropertyChanged.PropertyChanged"/> event.</remarks>
    private new bool IsReportingProgress => SelectedProgress is not null && !SelectedProgress.IsCompleted;

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
