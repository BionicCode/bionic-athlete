namespace FitToCsvConverter.Data;

using BionicCode.Utilities.Net;

public readonly struct ConversionInfo
{
    public string SourceFilePath { get; }
    public string DestinationFilePath { get; }

    public ConversionInfo(string sourceFilePath, string destinationFilePath)
    {
        ArgumentExceptionAdvanced.ThrowIfNullOrEmpty(sourceFilePath);
        ArgumentExceptionAdvanced.ThrowIfNullOrEmpty(destinationFilePath);

        SourceFilePath = sourceFilePath;
        DestinationFilePath = destinationFilePath;
    }
}
