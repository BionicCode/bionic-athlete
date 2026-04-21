namespace FitToCsvConverter.Data.Exporting;

using System.Collections.Immutable;
using System.Text;
using FitToCsvConverter.Data.Activities;

/// <summary>
/// Describes a decoded FIT activity export to one or more CSV files.
/// </summary>
/// <remarks>
/// The request uses the imported <see cref="FitActivity"/> as source data and keeps CSV-specific settings separate
/// from persistence and sync concerns.
/// </remarks>
public sealed class CsvExportRequest
{
    private static readonly Encoding s_defaultEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    /// <summary>
    /// Initializes a new instance of the <see cref="CsvExportRequest"/> class.
    /// </summary>
    /// <param name="sourceActivity">The decoded activity that acts as the export source.</param>
    /// <param name="nodeRequests">The node-specific CSV outputs to generate.</param>
    /// <param name="encoding">
    /// The text encoding to use when writing the CSV files.
    /// When <see langword="null"/>, UTF-8 without BOM is used.
    /// </param>
    /// <param name="delimiter">The CSV delimiter to use when writing files.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="sourceActivity"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="delimiter"/> is not a valid CSV delimiter.
    /// </exception>
    public CsvExportRequest(
        FitActivity sourceActivity,
        ImmutableArray<CsvNodeExportRequest> nodeRequests,
        Encoding? encoding = null,
        char delimiter = ',')
    {
        ArgumentNullException.ThrowIfNull(sourceActivity);
        ValidateDelimiter(delimiter);

        SourceActivity = sourceActivity;
        NodeRequests = nodeRequests.IsDefault ? ImmutableArray<CsvNodeExportRequest>.Empty : nodeRequests;
        Encoding = encoding ?? s_defaultEncoding;
        Delimiter = delimiter;
    }

    /// <summary>
    /// Gets the decoded activity that acts as the export source.
    /// </summary>
    public FitActivity SourceActivity { get; }

    /// <summary>
    /// Gets the node-specific CSV outputs to generate.
    /// </summary>
    public ImmutableArray<CsvNodeExportRequest> NodeRequests { get; }

    /// <summary>
    /// Gets the text encoding used for generated CSV files.
    /// </summary>
    public Encoding Encoding { get; }

    /// <summary>
    /// Gets the delimiter used for generated CSV files.
    /// </summary>
    public char Delimiter { get; }

    private static void ValidateDelimiter(char delimiter)
    {
        if (delimiter is '\0' or '\r' or '\n' or '"')
        {
            throw new ArgumentOutOfRangeException(nameof(delimiter), delimiter, "The CSV delimiter must be a printable non-quote character.");
        }
    }
}
