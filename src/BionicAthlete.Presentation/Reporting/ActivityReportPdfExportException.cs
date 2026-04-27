namespace BionicAthlete.Presentation.Reporting;

/// <summary>
/// Represents a failure during UI-bound report PDF generation.
/// </summary>
public class ActivityReportPdfExportException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ActivityReportPdfExportException"/> class.
    /// </summary>
    public ActivityReportPdfExportException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ActivityReportPdfExportException"/> class.
    /// </summary>
    /// <param name="message">Error message.</param>
    public ActivityReportPdfExportException(string? message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ActivityReportPdfExportException"/> class.
    /// </summary>
    /// <param name="message">Error message.</param>
    /// <param name="innerException">Inner exception that caused the failure.</param>
    public ActivityReportPdfExportException(string? message, Exception? innerException)
        : base(message, innerException)
    {
    }
}
