namespace BionicCode.Utilities.Net;

using System;

/// <summary>
/// Event args for the <see cref="ObservableProgressReporter.ProgressReported"/> event.
/// </summary>
public class ObservableProgressChangedEventArgs : EventArgs
{
    /// <summary>
    /// MemberConstructor.
    /// </summary>
    public ObservableProgressChangedEventArgs() : this(-1, -1, string.Empty, new ObservableProgressData(0.0, 0.0, string.Empty, string.Empty))
    {
    }

    /// <summary>
    /// MemberConstructor.
    /// </summary>
    /// <param name="oldValue">The old progress value before the change.</param>
    /// <param name="newValue">The new progress value after the change.</param>
    public ObservableProgressChangedEventArgs(double oldValue, double newValue, ObservableProgressData progressData) : this(oldValue, newValue, string.Empty, progressData)
    {
    }

    /// <summary>
    /// MemberConstructor.
    /// </summary>
    /// <param name="oldValue">The old progress value before the change.</param>
    /// <param name="newValue">The new progress value after the change.</param>
    /// <param name="progressText">A text message to summarize the progress.</param>
    public ObservableProgressChangedEventArgs(double oldValue, double newValue, string progressText, ObservableProgressData progressData)
    {
        OldValue = oldValue;
        NewValue = newValue;
        ProgressText = progressText;
        ProgressData = progressData;
    }

    /// <summary>
    /// The old progress value before the change.
    /// </summary>
    public double OldValue { get; }
    /// <summary>
    /// The new progress value after the change.
    /// </summary>
    public double NewValue { get; }
    /// <summary>
    /// A text message to summarize the progress.
    /// </summary>
    public string ProgressText { get; }
    /// <summary>
    /// Indicates that the progress is indeterminate what would characterize the progress values of <see cref="OldValue"/> and <see cref="NewValue"/> just random progress e.g. bytes transferred instead of an abslote value of a fixed value range.
    /// </summary>
    public bool IsIndeterminate { get; }

    public ObservableProgressData ProgressData { get; }
}
