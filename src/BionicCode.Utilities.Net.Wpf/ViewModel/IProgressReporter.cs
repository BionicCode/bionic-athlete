namespace BionicCode.Utilities.Net;

#region Info
// //  
// WpfTestRange.Main
#endregion
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;

/// <summary>
/// Interface to provide progress properties to be exposed by a view model for data binding a progress reporter GUI control.
/// </summary>
public interface IProgressReporter : INotifyPropertyChanged
{
    /// <summary>
    /// Creates a <see cref="IProgress{T}"/> instance that is associated with the application's primary dispatcher thread.
    /// The registered progress callback is the virtual <c><see cref="ViewModel"/>.OnProgress(ProgressData)</c> member.
    /// </summary>
    /// <remarks>To create a <see cref="IProgress{T}"/> instance that is associated with the application's primary dispatcher thread, call </remarks>
    /// <returns>A <see cref="IProgress{ProgressData}"/> instance that posts progress to the thread <see cref="IProgressReporterCommon.CreateProgressReporterFromCurrentThread"/> was called from.</returns>
    [Obsolete("Use 'StartNewObservableProgressReporting()' instead.", error: true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    IProgress<ProgressData> CreateProgressReporterFromUiThread();

    ReadOnlyObservableCollection<ObservableProgressData> ProgressDataCollection { get; }

    ObservableProgressData SelectedProgress { get; }

    /// <summary>
    /// Raised when <see cref="SelectedProgress"/> has a changed value.
    /// </summary>
    event EventHandler<ObservableProgressChangedEventArgs>? ProgressChanged;

    /// <summary>
    /// Indicates ongoing progress reporting 
    /// </summary>
    /// <remarks>Raises <see cref="INotifyPropertyChanged.PropertyChanged"/> event.</remarks>
    bool IsReportingProgress { get; }

    /// <summary>
    /// Starts a new progress reporting operation by creating a new instance of <see cref="ObservableProgressData"/> with the specified parameters and adding it to the internal collection and assigns it to the <see cref="SelectedProgress"/> property.
    /// </summary>
    /// <remarks>The returned <see cref="IProgress{T}"/> instance is associated with the captured context. 
    /// If <see cref="StartNewObservableProgressReporting(string, string, double)"/> is called on the UI thread
    /// then the captured context is the <see cref="Dispatcher"/> thread and progress is always reported to that UI thread.
    /// <para/>The method actually returns a <see cref="ObservableProgressReporter"/> instance. 
    /// The instance is casted to the return type <see cref="IProgress{ProgressData}"/> to enforce MVVM compliance when the reporter is handed over to the model.
    /// <para/>Setting progress on the returned <see cref="ObservableProgressReporter"/> instance will 
    /// update the <see cref="SelectedProgress"/> property and notify any observers via the <see cref="ProgressChanged"/> event.</remarks>
    /// <param name="initialMessage">The initial message for the progress operation.</param>
    /// <param name="operationTitle">The title of the progress operation.</param>
    /// <param name="maxValue">The maximum value for the progress operation.</param>
    /// <param name="isIndeterminate">Indicates whether the progress operation is indeterminate.</param>
    /// <returns>An <see cref="ObservableProgressReporter"/> instance casted to <see cref="IProgress{ProgressData}"/> instance that reports progress to the UI thread.</returns>
    IProgress<ProgressData> StartNewObservableProgressReporting(string initialMessage = "", string operationTitle = "", double maxValue = 100, bool isIndeterminate = false);

    /// <summary>
    /// Starts a new progress reporting operation by creating a new instance of <see cref="ObservableProgressData"/> with the specified parameters and adding it to the internal collection and assigns it to the <see cref="SelectedProgress"/> property.
    /// </summary>
    /// <remarks>The returned <see cref="IProgress{T}"/> instance is associated with the captured context. 
    /// If <see cref="StartNewObservableProgressReporting(Action{ObservableProgressData}, string, string, double)"/> is called on the UI thread 
    /// then the captured context is the <see cref="Dispatcher"/> thread and progress is always reported to that UI thread.
    /// <para/>The method actually returns a <see cref="ObservableProgressReporter"/> instance. 
    /// The instance is casted to the return type <see cref="IProgress{ProgressData}"/> to enforce MVVM compliance when the reporter is handed over to the model.
    /// <para/>Setting progress on the returned <see cref="ObservableProgressReporter"/> instance will 
    /// update the <see cref="SelectedProgress"/> property and notify any observers via the <see cref="ProgressChanged"/> event.</remarks>
    /// <param name="initialMessage">The initial message for the progress operation.</param>
    /// <param name="operationTitle">The title of the progress operation.</param>
    /// <param name="maxValue">The maximum value for the progress operation.</param>
    /// <param name="onProgress">An additional progress callback that is invoked when progress is reported.</param>
    /// <param name="isIndeterminate">Indicates whether the progress operation is indeterminate.</param>
    /// <returns>An <see cref="ObservableProgressReporter"/> instance casted to <see cref="IProgress{ProgressData}"/> instance that reports progress to the UI thread.</returns>
    IProgress<ProgressData> StartNewObservableProgressReporting(Action<ObservableProgressData> onProgress, string initialMessage = "", string operationTitle = "", double maxValue = 100, bool isIndeterminate = false);

    ObservableProgressData RemoveObservableProgressData(int index);

    void RemoveAllCompletedObservableProgressData();

    void RemoveAllObservableProgressData();

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
    void OnProgress(ObservableProgressData progress);
}
