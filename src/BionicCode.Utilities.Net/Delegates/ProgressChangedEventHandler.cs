namespace BionicCode.Utilities.Net;

using System.ComponentModel;

/// <summary>
/// Eventhandler for the <see cref="IProgressReporterCommon.ProgressChanged"/> event.
/// </summary>
/// <param name="sender">the event source.</param>
/// <param name="e">The event data.</param>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1003:Use generic event handler instances", Justification = "<Pending>")]
[Obsolete("This delegate is obsolete. Use ObservableProgressChangedEventHandler instead.", error: false)]
[EditorBrowsable(EditorBrowsableState.Never)]
public delegate void ProgressChangedEventHandler(object sender, ProgressChangedEventArgs e);