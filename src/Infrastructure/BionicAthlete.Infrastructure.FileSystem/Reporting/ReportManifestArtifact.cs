namespace BionicAthlete.Infrastructure.FileSystem.Reporting;

/// <summary>
/// A file written as part of a report package.
/// </summary>
/// <param name="ArtifactKind">Artifact kind, such as <c>HtmlReport</c> or <c>PdfReport</c>.</param>
/// <param name="RelativePath">Path relative to the report folder.</param>
/// <param name="MediaType">Media type for the artifact.</param>
public sealed record ReportManifestArtifact(string ArtifactKind, string RelativePath, string MediaType);
