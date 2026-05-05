namespace BionicAthlete.Application.Reporting;

/// <summary>
/// A file written as part of a report package.
/// </summary>
/// <param name="ArtifactKind">The <see cref="ArtifactKind"/> kind, such as <see cref="ArtifactKind.HtmlReport"/> or <see cref="ArtifactKind.PdfReport"/>.</param>
/// <param name="RelativePath">Path relative to the report folder.</param>
public readonly record struct ReportManifestArtifact(ArtifactKind ArtifactKind, string RelativePath);
