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
    #endregion IProgressReporter

    /// <summary>
    /// Creates a <see cref="IProgress{T}"/> instance that is associated with the caller's thread.
    /// The registered progress callback is the virtual <see cref="ViewModelCommon.OnProgress(ProgressData)"/> member.
    /// </summary>
    /// <remarks>The returned <see cref="IProgress{T}"/> instance is associated with the application's primary dispatcher thread. Progress is always reported to the UI thread that is associated with the <c>Dispatcher</c> returned by <c>Application.Current.Dispatcher</c>.</remarks>
    /// <returns>A <see cref="IProgress{ProgressData}"/> instance that always posts progress to the UI thread.</returns>
    public IProgress<ProgressData> CreateProgressReporterFromUiThread() => Application.Current.Dispatcher.Invoke(() => new Progress<ProgressData>(OnProgress));

    protected IProgress<ProgressData> StartNewObservableProgressReporting(string initialMessage = "", string operationTitle = "")
    {
        var progressData = new ObservableProgressData { Message = initialMessage, OperationTitle = operationTitle };
        _progressDataCollectionInternal.Add(progressData);
        return new ObservableProgressReporter(OnProgress, progressData);
    }

    protected IProgress<ProgressData> StartNewObservableProgressReporting(Action<ProgressData> onProgress, string initialMessage = "", string operationTitle = "")
    {
        ArgumentNullExceptionAdvanced.ThrowIfNull(onProgress);

        var progressData = new ObservableProgressData { Message = initialMessage, OperationTitle = operationTitle };
        _progressDataCollectionInternal.Add(progressData);
        Action<ProgressData> reportAction = onProgress + OnProgress;

        return new ObservableProgressReporter(reportAction, progressData);
    }

    protected ObservableProgressData RemoveObservableProgressData(int index)
    {
        ObservableProgressData progressData = _progressDataCollectionInternal[index];
        _progressDataCollectionInternal.RemoveAt(index);
        return progressData;
    }

    protected void RemoveAllCompletedObservableProgressData()
    {
        foreach (ObservableProgressData? progressData in _progressDataCollectionInternal
            .Where(progressData => progressData.Progress >= 1.0).ToList())
        {
            _ = _progressDataCollectionInternal.Remove(progressData);
        }
    }

    protected void RemoveAllObservableProgressData() => _progressDataCollectionInternal.Clear();

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
    protected virtual void OnProgress(ObservableProgressData progress)
    {
        ArgumentNullExceptionAdvanced.ThrowIfNull(progress);

        ProgressText = progress.Message;
        IsIndeterminate = progress.Progress == ViewModelCommon.EnableIndeterminateMode || IsIndeterminate;
        ProgressValue = progress.Progress;
    }
}
