namespace BionicAthlete.Presentation.Reporting;

/// <summary>
/// Represents a failure during UI-bound report PDF generation.
/// </summary>
public class PdfExportException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PdfExportException"/> class.
    /// </summary>
    public PdfExportException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PdfExportException"/> class.
    /// </summary>
    /// <param name="message">Error message.</param>
    public PdfExportException(string? message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PdfExportException"/> class.
    /// </summary>
    /// <param name="message">Error message.</param>
    /// <param name="innerException">Inner exception that caused the failure.</param>
    public PdfExportException(string? message, Exception? innerException)
        : base(message, innerException)
    {
    }
}
