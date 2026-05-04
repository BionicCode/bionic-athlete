namespace BionicAthlete.FileSystem.Abstractions;

using System.Text;
using BionicAthlete.Application.Exporting;

using BionicAthlete.Application.Reporting.Html;
using BionicCode.Utilities.Net;

public class HtmlFileExporterArgs : HtmlExporterArgs
{
    public HtmlFileExporterArgs(HtmlDocument document,
        string outputDirectory,
        string outputFileName,
        bool isOverWriteExistingAllowed,
        Encoding? encoding) : base(document)
    {
        ArgumentExceptionAdvanced.ThrowIfNullOrWhiteSpace(outputDirectory);
        ArgumentExceptionAdvanced.ThrowIfNullOrWhiteSpace(outputFileName);

        OutputDirectory = outputDirectory;
        OutputFileName = outputFileName;
        IsOverWriteExistingAllowed = isOverWriteExistingAllowed;
        Encoding = encoding ?? Encoding.UTF8;
    }

    public string OutputDirectory { get; init; }
    public string OutputFileName { get; init; }
    public bool IsOverWriteExistingAllowed { get; }
    public Encoding Encoding { get; }
}