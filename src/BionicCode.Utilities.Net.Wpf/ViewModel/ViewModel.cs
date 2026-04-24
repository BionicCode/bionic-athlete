namespace BionicCode.Utilities.Net;

using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
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
    private readonly ObservableCollection<ObservableProgressData> _progressDataCollectionInternal;
    private ObservableProgressData? _selectedProgress;
    private Index _selectedProgressIndex;

    protected ViewModel()
    {
        _progressDataCollectionInternal = [];
        Application.Current.Dispatcher.Invoke(() => BindingOperations.EnableCollectionSynchronization(_progressDataCollectionInternal, _progressDataCollectionSyncRoot), DispatcherPriority.Send);
        ProgressDataCollection = new ReadOnlyObservableCollection<ObservableProgressData>(_progressDataCollectionInternal);
        _progressDataCollectionInternal.CollectionChanged += OnProgressDataCollectionChanged;
        _selectedProgressIndex = 0;
    }

    protected virtual void OnProgressDataCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(HasProgressDataCollectionItems));
        OnPropertyChanged(nameof(HasProgressData));
        OnPropertyChanged(nameof(IsProgressDataCollectionEmpty));
    }

    #region IProgressReporter
    public RelayCommand ClearAllProgressCommand => new(RemoveAllCompletedObservableProgressData, () => HasProgressDataCollectionItems);
    public ReadOnlyObservableCollection<ObservableProgressData> ProgressDataCollection { get; }
    public bool HasProgressDataCollectionItems => ProgressDataCollection.Count > 0;
    public bool IsProgressDataCollectionEmpty => ProgressDataCollection.Count == 0;
    public bool HasProgressData => HasProgressDataCollectionItems || SelectedProgress is not null;

    public virtual ObservableProgressData? SelectedProgress
    {
        get => _selectedProgress;
        private set
        {
            if (TrySetValue(value, ref _selectedProgress))
            {
                OnPropertyChanged(nameof(HasProgressData));
            }
        }
    }

    /// <summary>
    /// Sets the selected <see cref="ObservableProgressData"/> item in the <see cref="ProgressDataCollection"/> by the specified index and assigns it to the <see cref="SelectedProgress"/> property."/>
    /// </summary>
    /// <remarks>Setting this property will update the <see cref="SelectedProgress"/> property and raise the <see cref="HasProgressData"/> property changed notification.
    /// <para/>The index specifies the default position that is mapped to the <see cref="SelectedProgress"/>. For example, if an items gets removed from or added to the <see cref="ProgressDataCollection"/> at the current <see cref="SelectedProgressIndex"/> index, the new item will be selected automatically based on that index.
    /// <para/>The value can be out of range and describes general behavior based on the current <see cref="ProgressDataCollection"/> size.
    /// <br/>If <see cref="Index"/> is &lt; zero then <see cref="SelectedProgress"/> will be set to <see langword="null" />. If the <see cref="Index"/> is greater than or equal to the number of items in the <see cref="ProgressDataCollection"/> then the last item will be selected. Otherwise, the item at the specified index will be selected.
    /// <para/>Use <c>new Index(1, true)</c> or <c>SelectedProgressIndex = ^1</c> to specify an index from the end of the collection i.e. to always select the last progress item from <see cref="ProgressDataCollection"/>.</remarks>
    /// <value>The <see cref="Index"/> of the selected progress data item in the <see cref="ProgressDataCollection"/>. A value of -1 indicates that no item is selected and <see langword="null" /> is assigned to the <see cref="SelectedProgress"/> property. The default value is 0 resulting in always the first item being selected if available.</value>
    public virtual Index SelectedProgressIndex
    {
        get => _selectedProgressIndex;
        protected set
        {
            if (TrySetValue(value, ref _selectedProgressIndex))
            {
                SetSelectedProgress(value);
            }
        }
    }

    protected void SetSelectedProgress() => SetSelectedProgress(SelectedProgressIndex);

    protected void SetSelectedProgress(Index progressDataIndex)
    {
        if (IsSelectedProgressIndexValid(progressDataIndex))
        {
            // If provided index is greater than collection count try to project on current collection.
            // If value is from end then the caller is obviously not interested in the last element so we provide the element
            // that is farthest away from the end, which in this specific case is always the first item.
            // If value is from start then we provide the last item, which is the farthest away from the start.
            if (progressDataIndex.Value > _progressDataCollectionInternal.Count)
            {
                progressDataIndex = progressDataIndex.IsFromEnd
                    ? 0
                    : ^1;
            }

            SelectedProgress = _progressDataCollectionInternal[progressDataIndex];
        }
        else
        {
            SelectedProgress = null;
        }

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

    private void UpdateSelectedProgressData() => SetSelectedProgress();

    /// <summary>
    /// Checks whether the current <see cref="SelectedProgressIndex"/> is valid based on the current count of items in the <see cref="ProgressDataCollection"/>.
    /// </summary>
    /// <remarks>Valid if the index is greater than '-1' and the <see cref="ProgressDataCollection"/> is not empty. Validity in this context means that the <see cref="SelectedProgressIndex"/> is projectible to an actual <see cref="ObservableProgressData"/> within the <see cref="ProgressDataCollection"/>.</remarks>
    /// <value><see langword="true"/> if the <see cref="SelectedProgressIndex"/> is valid, which is when the index is greater than '-1' and the <see cref="ProgressDataCollection"/> is not empty; otherwise, <see langword="false"/>.</value>
    protected bool IsSelectedProgressIndexValid(Index progressDataIndex) => _progressDataCollectionInternal.Any()
        && progressDataIndex.GetOffset(_progressDataCollectionInternal.Count) >= 0;

    public void RemoveAllCompletedObservableProgressData()
    {
        foreach (ObservableProgressData? progressData in _progressDataCollectionInternal
            .Where(progressData => progressData.IsCompleted).ToList())
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

        UpdateSelectedProgressData();
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
