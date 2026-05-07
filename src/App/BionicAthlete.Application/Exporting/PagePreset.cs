namespace BionicAthlete.Application.Exporting;

/// <summary>
/// Identifies the print-layout preset used by the HTML report and WebView2 PDF settings.
/// </summary>
public enum PagePreset
{
    Undefined = 0,
    /// <summary>
    /// A custom page size supplied by <see cref="PageSettings"/>.
    /// </summary>
    Custom = 1,

    /// <summary>
    /// A4 portrait page preset.
    /// </summary>
    A4Portrait = 2,

    /// <summary>
    /// US Letter portrait page preset.
    /// </summary>
    UsLetterPortrait = 3
}
