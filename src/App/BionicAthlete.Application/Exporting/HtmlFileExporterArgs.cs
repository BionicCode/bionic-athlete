namespace BionicAthlete.Application.Exporting;

using System.Text;
using BionicAthlete.Application;
using BionicCode.Utilities.Net;

public class HtmlFileExporterArgs : HtmlExporterArgs
{
    public HtmlFileExporterArgs(HtmlDocument document,
        Uri destinationUri,
        bool isOverWriteExistingAllowed,
        Encoding? encoding) : base(document, destinationUri)
    {
        ArgumentNullExceptionAdvanced.ThrowIfNull(destinationUri);
        ArgumentExceptionAdvanced.ThrowIfFalse(destinationUri.IsFile, $"Invalid argument '{destinationUri}'. The URI '{destinationUri.AbsolutePath}' is not a file.");

        OutputDirectory = Path.GetDirectoryName(destinationUri.LocalPath) ?? throw new InvalidOperationException($"Unable to determine the directory for '{destinationUri.LocalPath}'.");
        OutputFileName = Path.GetFileName(destinationUri.LocalPath) is string fileName && !string.IsNullOrWhiteSpace(fileName)
            ? fileName
            : throw new InvalidOperationException($"Unable to determine the file name for '{destinationUri.LocalPath}'.");
        IsOverWriteExistingAllowed = isOverWriteExistingAllowed;
        Encoding = encoding ?? Encoding.UTF8;
    }

    public string OutputDirectory { get; init; }
    public string OutputFileName { get; init; }
    public bool IsOverWriteExistingAllowed { get; }
    public Encoding Encoding { get; }
}
