namespace BionicAthlete.Infrastructure.FileSystem;

using System;
using BionicAthlete.Application.Exporting;
using BionicAthlete.FileSystem.Abstractions;
using BionicCode.Utilities.Net;

public sealed class HtmlFileExporter : IHtmlFileExporter
{
    private readonly IFileManager<string> _textFileManager;

    public HtmlFileExporter(IFileManager<string> textFileManager) => _textFileManager = textFileManager;

    public void Export(HtmlFileExporterArgs args)
    {
        ArgumentNullExceptionAdvanced.ThrowIfNull(args, nameof(args));

        if (!Directory.Exists(args.OutputDirectory))
        {
            _ = Directory.CreateDirectory(args.OutputDirectory);
        }

        string destinationFilePath = Path.Combine(args.OutputDirectory, args.OutputFileName);
        _textFileManager.Write(args.Document.Content, args.Encoding, destinationFilePath, args.IsOverWriteExistingAllowed);
    }

    public async Task ExportAsync(HtmlFileExporterArgs args, CancellationToken cancellationToken)
    {
        ArgumentNullExceptionAdvanced.ThrowIfNull(args, nameof(args));

        if (!Directory.Exists(args.OutputDirectory))
        {
            _ = Directory.CreateDirectory(args.OutputDirectory);
        }

        string destinationFilePath = Path.Combine(args.OutputDirectory, args.OutputFileName);
        await _textFileManager.WriteAsync(args.Document.Content, args.Encoding, destinationFilePath, args.IsOverWriteExistingAllowed, cancellationToken);
    }

    void IHtmlExporter.Export(HtmlExporterArgs args)
    {
        if (args is HtmlFileExporterArgs fileArgs)
        {
            Export(fileArgs);
        }
        else
        {
            throw new ArgumentException($"Invalid argument type. Expected a type convertible to '{typeof(HtmlFileExporterArgs).FullName}'.", nameof(args));
        }
    }

    Task IHtmlExporter.ExportAsync(HtmlExporterArgs args, CancellationToken cancellationToken) => args is HtmlFileExporterArgs fileArgs
        ? ExportAsync(fileArgs, cancellationToken)
        : throw new ArgumentException("Invalid argument type", nameof(args));
}
