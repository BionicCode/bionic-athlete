namespace BionicAthlete.Application.Exporting;

public enum ArtifactKind
{
    Undefined = 0,
    PdfReport,
    HtmlReport,
    ReportManifest,
    UserMarkdownReport,
    UserPlainTextReport,
    CsvReport,
    ImagePng,
    ImageSvg,
    ImageJpg,
    ZipArchive,
}