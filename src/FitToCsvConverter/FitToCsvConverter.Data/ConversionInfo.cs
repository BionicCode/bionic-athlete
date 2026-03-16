namespace FitToCsvConverter.Data;

using System.Text;
using BionicCode.Utilities.Net;

public readonly struct ConversionInfo
{
    public string SourceFilePath { get; }
    public string DestinationFilePath { get; }
    public Encoding Encoding { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConversionInfo"/> class using the specified source and destination file paths,
    /// with UTF-8 encoding as the default.
    /// </summary>
    /// <remarks>This constructor defaults the encoding to UTF-8. Use the overload with an explicit encoding
    /// parameter if a different encoding is required.</remarks>
    /// <param name="sourceFilePath">The full path to the source file to be converted. Cannot be <see langword="null"/> or empty.</param>
    /// <param name="destinationFilePath">The full path to the destination file where the converted content will be saved (including the file name). Cannot be <see langword="null"/> or empty.</param>
    public ConversionInfo(string sourceFilePath, string destinationFilePath) : this(sourceFilePath, destinationFilePath, Encoding.UTF8)
    {
    }

    public ConversionInfo(string sourceFilePath, string destinationFilePath, Encoding encoding)
    {
        ArgumentExceptionAdvanced.ThrowIfNullOrEmpty(sourceFilePath);
        ArgumentExceptionAdvanced.ThrowIfNullOrEmpty(destinationFilePath);
        ArgumentNullExceptionAdvanced.ThrowIfNull(encoding);

        SourceFilePath = sourceFilePath;
        DestinationFilePath = destinationFilePath;
        Encoding = encoding;
    }
}
