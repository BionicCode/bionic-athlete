namespace BionicAthlete.Application.Reporting;

using BionicAthlete.Application.Exporting;
using BionicAthlete.Infrastructure.FileSystem;
using BionicCode.Utilities.Net;

/// <summary>
/// A file written as part of a report package.
/// </summary>
/// <param name="ArtifactKind">The <see cref="ArtifactKind"/> kind, such as <see cref="ArtifactKind.HtmlReport"/> or <see cref="ArtifactKind.PdfReport"/>.</param>
/// <param name="RelativePath">Path relative to the report folder.</param>
public readonly record struct ReportManifestArtifact
{
    internal ReportManifestArtifact(ArtifactKind artifactKind, string relativePath, string mediaType)
    {
        ArtifactKind = artifactKind;
        RelativePath = relativePath;
        MediaType = mediaType;
    }

    /// <summary>
    /// Factory method that ensures integrity of the created <see cref="ReportManifestArtifact"/> instances, 
    /// such as ensuring that the <see cref="ArtifactKind"/> is valid and that the <see cref="RelativePath"/> is well-formed.
    /// </summary>
    /// <param name="artifactKind">The kind of artifact, such as <see cref="ArtifactKind.HtmlReport"/> or <see cref="ArtifactKind.PdfReport"/>.</param>
    /// <param name="relativeArtifactFilePath">The path relative to the report folder.</param>
    /// <returns>A new instance of <see cref="ReportManifestArtifact"/>.</returns>
    public static ReportManifestArtifact Create(ArtifactKind artifactKind, string relativeArtifactFilePath) => ReportManifestArtifactFactory.Create(artifactKind, relativeArtifactFilePath);

    public ArtifactKind ArtifactKind { get; }
    public string RelativePath { get; }
    public string MediaType { get; }

    #region ReportManifestArtifactFactory
    private static class ReportManifestArtifactFactory
    {
        private static readonly AspNetCoreMimeMediaTypeMapProvider s_mediaTypeProvider = new ();

        public static ReportManifestArtifact Create(ArtifactKind artifactKind, string relativeArtifactFilePath)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(relativeArtifactFilePath);
            ArgumentExceptionAdvanced.ThrowIfEnumIsNotDefined<ArtifactKind>(artifactKind);
            ArgumentExceptionAdvanced.ThrowIfEnumEqualsAny(artifactKind, [ArtifactKind.Undefined]);

            if (!ExportHelpers.ArtifactKindToFileExtensionTable.TryGetValue(artifactKind, out FileExtension requiredFileExtension))
            {
                throw new NotImplementedException($"The file extension for artifact kind '{artifactKind}' is not defined in the artifact kind to file extension mapping.");
            }

            var providedArtifactExtension = FileExtension.FromFilePath(relativeArtifactFilePath);
            ArgumentExceptionAdvanced.ThrowIfFalse(
                providedArtifactExtension == requiredFileExtension,
                $"The argument '{relativeArtifactFilePath}' has an invalid file extension for artifact kind '{artifactKind}'. Expected '{requiredFileExtension}', but got '{providedArtifactExtension}'.");

            return s_mediaTypeProvider.TryGetMediaTypeForExtension(providedArtifactExtension, out string? mediaType)
                ? new ReportManifestArtifact(artifactKind, relativeArtifactFilePath, mediaType)
                : new ReportManifestArtifact(artifactKind, relativeArtifactFilePath, s_mediaTypeProvider.DefaultMediaType);
        }
    }
    #endregion ReportManifestArtifactFactory
}
