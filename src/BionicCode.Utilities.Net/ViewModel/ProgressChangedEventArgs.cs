namespace BionicCode.Utilities.Net;

using System;
using System.ComponentModel;

/// <summary>
/// Event args for the <see cref="ProgressChangedEventHandler"/>.
/// </summary>
[Obsolete("This class is deprecated. Use the 'ObservableProgressChangedEventArgs' class instead to provide more detailed progress information including percentage and indeterminate state.", error: false)]
[EditorBrowsable(EditorBrowsableState.Never)]
public class ProgressChangedEventArgs : EventArgs
{
    /// <summary>
    /// MemberConstructor.
    /// </summary>
    [Obsolete("This class is deprecated. Use the 'ObservableProgressChangedEventArgs' class instead to provide more detailed progress information including percentage and indeterminate state.", error: false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public ProgressChangedEventArgs() : this(-1, -1, string.Empty)
    {
    }

    /// <summary>
    /// MemberConstructor.
    /// </summary>
    /// <param name="oldValue">The old progress value before the change.</param>
    /// <param name="newValue">The new progress value after the change.</param>
    [Obsolete("This class is deprecated. Use the 'ObservableProgressChangedEventArgs' class instead to provide more detailed progress information including percentage and indeterminate state.", error: false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public ProgressChangedEventArgs(double oldValue, double newValue) : this(oldValue, newValue, string.Empty)
    {
    }

    /// <summary>
    /// MemberConstructor.
    /// </summary>
    /// <param name="oldValue">The old progress value before the change.</param>
    /// <param name="newValue">The new progress value after the change.</param>
    /// <param name="progressText">A text message to summarize the progress.</param>
    [Obsolete("This class is deprecated. Use the 'ObservableProgressChangedEventArgs' class instead to provide more detailed progress information including percentage and indeterminate state.", error: false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public ProgressChangedEventArgs(double oldValue, double newValue, string progressText)
    {
        OldValue = oldValue;
        NewValue = newValue;
        ProgressText = progressText;
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
}