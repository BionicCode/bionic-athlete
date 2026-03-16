namespace BionicCode.Utilities.Net;
/// <summary>
/// Eventhandler for the <see cref="IProgressReporterCommon.ProgressChanged"/> event.
/// </summary>
/// <param name="sender">the event source.</param>
/// <param name="e">The event data.</param>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1003:Use generic event handler instances", Justification = "<Pending>")]
public delegate void ProgressChangedEventHandler(object sender, ProgressChangedEventArgs e);
