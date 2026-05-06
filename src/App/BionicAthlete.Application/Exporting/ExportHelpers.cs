namespace BionicAthlete.Application.Exporting;

using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Text;
using BionicCode.Utilities.Net;

public static class ExportHelpers
{
    public static FrozenDictionary<ArtifactKind, FileExtension> ArtifactKindToFileExtensionTable { get; }
        = new Dictionary<ArtifactKind, FileExtension>
        {
            [ArtifactKind.PdfReport] = FileExtensions.Pdf,
            [ArtifactKind.HtmlReport] = FileExtensions.Html,
            [ArtifactKind.ReportManifest] = FileExtensions.Json,
            [ArtifactKind.UserMarkdownReport] = FileExtensions.Md,
            [ArtifactKind.UserPlainTextReport] = FileExtensions.Txt,
            [ArtifactKind.CsvReport] = FileExtensions.Csv,
            [ArtifactKind.ImagePng] = FileExtensions.Png,
            [ArtifactKind.ImageSvg] = FileExtensions.Svg,
            [ArtifactKind.ImageJpg] = FileExtensions.Jpg,
            [ArtifactKind.ZipArchive] = FileExtensions.Zip,
        }.ToFrozenDictionary();
}
