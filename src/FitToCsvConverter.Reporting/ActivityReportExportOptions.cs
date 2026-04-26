namespace FitToCsvConverter.Reporting;

using System.Globalization;

/// <summary>
/// Options that make View C report projection and HTML generation deterministic.
/// </summary>
public sealed class ActivityReportExportOptions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ActivityReportExportOptions"/> class.
    /// </summary>
    /// <param name="outputDirectoryPath">The directory under which the <c>report</c> folder will be created.</param>
    /// <param name="outputTarget">The requested View C output target.</param>
    /// <param name="culture">The culture used for deterministic number and date formatting.</param>
    /// <param name="localTimeZone">The time zone used for local-time presentation.</param>
    /// <param name="exportTimestampUtc">The export timestamp to write into the report and manifest.</param>
    /// <param name="pageSettings">The neutral page settings used by HTML print CSS and the UI PDF exporter.</param>
    /// <param name="includeProvenanceNotes">Whether data-quality and provenance notes should be included.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="outputDirectoryPath"/> is <see langword="null"/> or whitespace.</exception>
    /// <exception cref="ArgumentNullException">Thrown when a required reference argument is <see langword="null"/>.</exception>
    public ActivityReportExportOptions(
        string outputDirectoryPath,
        ActivityReportOutputTarget outputTarget,
        CultureInfo culture,
        TimeZoneInfo localTimeZone,
        DateTimeOffset exportTimestampUtc,
        ActivityReportPageSettings pageSettings,
        bool includeProvenanceNotes = true)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectoryPath);
        ArgumentNullException.ThrowIfNull(culture);
        ArgumentNullException.ThrowIfNull(localTimeZone);
        ArgumentNullException.ThrowIfNull(pageSettings);

        OutputDirectoryPath = outputDirectoryPath;
        OutputTarget = outputTarget;
        Culture = culture;
        LocalTimeZone = localTimeZone;
        ExportTimestampUtc = exportTimestampUtc.ToUniversalTime();
        PageSettings = pageSettings;
        IncludeProvenanceNotes = includeProvenanceNotes;
    }

    /// <summary>
    /// Gets the directory under which the <c>report</c> folder will be created.
    /// </summary>
    public string OutputDirectoryPath { get; }

    /// <summary>
    /// Gets the requested output target.
    /// </summary>
    public ActivityReportOutputTarget OutputTarget { get; }

    /// <summary>
    /// Gets the culture used for deterministic report formatting.
    /// </summary>
    public CultureInfo Culture { get; }

    /// <summary>
    /// Gets the local time zone used when presenting source timestamps.
    /// </summary>
    public TimeZoneInfo LocalTimeZone { get; }

    /// <summary>
    /// Gets the timestamp that should be written into the report and manifest.
    /// </summary>
    public DateTimeOffset ExportTimestampUtc { get; }

    /// <summary>
    /// Gets the print/page settings for the report package.
    /// </summary>
    public ActivityReportPageSettings PageSettings { get; }

    /// <summary>
    /// Gets a value indicating whether provenance and data-quality notes should be emitted.
    /// </summary>
    public bool IncludeProvenanceNotes { get; }
}
