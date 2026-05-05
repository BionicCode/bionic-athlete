namespace BionicAthlete.Application.Reporting;

using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using BionicCode.Utilities.Net;

/// <summary>
/// Machine-readable manifest for a View C report folder.
/// </summary>
/// <param name="ReportSchemaVersion">Report manifest schema version.</param>
/// <param name="RendererVersion">Renderer version.</param>
/// <param name="ReportId">Stable report identifier.</param>
/// <param name="SourceFilePath">Source FIT file path when known.</param>
/// <param name="ExportTimestampUtc">Export timestamp supplied through <see cref="ReportExportOptions"/>.</param>
/// <param name="OutputTarget">Requested output target.</param>
/// <param name="PagePreset">Page preset used by the package.</param>
/// <param name="Artifacts">Generated artifacts relative to the report folder.</param>
/// <param name="IncludedSections">Section identifiers included in the HTML report.</param>
/// <param name="Diagnostics">Warnings or caveats emitted by projection/rendering.</param>
public sealed record ReportManifest
{
    private readonly HashSet<ReportManifestArtifact> _artifacts;
    private readonly IMimeMediaTypeMapProvider _mimeMediaTypeMapProvider;
    public static FrozenDictionary<ArtifactKind, FileExtension> ArtifactKindToFileExtensionTable { get; }
        = new Dictionary<ArtifactKind, FileExtension>
        {
            [ArtifactKind.PdfReport] = FileExtensions.Pdf,
            [ArtifactKind.HtmlReport] = FileExtensions.Html,
            [ArtifactKind.ReportManifest] = FileExtensions.Json
        }.ToFrozenDictionary();

    public ReadOnlySet<ReportManifestArtifact> Artifacts { get; }
    public int ReportSchemaVersion { get; }
    public string RendererVersion { get; }
    public string ReportId { get; }
    public string SourceFilePath { get; }
    public DateTimeOffset ExportTimestampUtc { get; }
    public ReportOutputTarget OutputTarget { get; }
    public ReportPagePreset PagePreset { get; }
    public ImmutableArray<string> IncludedSections { get; }
    public ImmutableArray<ReportDiagnostic> Diagnostics { get; }

    public ReportManifest(
        int reportSchemaVersion,
        string rendererVersion,
        string reportId,
        string sourceFilePath,
        DateTimeOffset exportTimestampUtc,
        ReportOutputTarget outputTarget,
        ReportPagePreset pagePreset,
        IEnumerable<ReportManifestArtifact> artifacts,
        ImmutableArray<string> includedSections,
        ImmutableArray<ReportDiagnostic> diagnostics,
        IMimeMediaTypeMapProvider mimeMediaTypeMapProvider)
    {
        ReportSchemaVersion = reportSchemaVersion;
        RendererVersion = rendererVersion;
        ReportId = reportId;
        SourceFilePath = sourceFilePath;
        ExportTimestampUtc = exportTimestampUtc;
        OutputTarget = outputTarget;
        PagePreset = pagePreset;
        _artifacts = [.. artifacts];
        Artifacts = new ReadOnlySet<ReportManifestArtifact>(_artifacts);
        IncludedSections = includedSections;
        Diagnostics = diagnostics;
        _mimeMediaTypeMapProvider = mimeMediaTypeMapProvider;
    }

    /// <inheritdoc />
    public void AddArtifactToManifest(
        string relativeArtifactFilePath,
        ArtifactKind artifactKind)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(relativeArtifactFilePath);
        ArgumentExceptionAdvanced.ThrowIfEnumIsNotDefined<ArtifactKind>(artifactKind);
        ArgumentExceptionAdvanced.ThrowIfEnumEqualsAny(artifactKind, [ArtifactKind.Undefined]);

        if (!ArtifactKindToFileExtensionTable.TryGetValue(artifactKind, out FileExtension fileExtension))
        {
            throw new NotImplementedException($"The file extension for artifact kind '{artifactKind}' is not defined in the artifact kind to file extension mapping.");
        }

        if

        var reportManifestArtifact = new ReportManifestArtifact(artifactKind, relativeArtifactFilePath);
        if (_artifacts.Contains(reportManifestArtifact))
        {
            throw new InvalidOperationException($"The artifact for '{reportManifestArtifact.RelativePath}' already exists in the manifest.");
        }

        UpdateManifest(reportManifestArtifact);
    }

    public void UpdateManifest(ReportManifestArtifact reportManifestArtifact)
    {
        ArgumentNullExceptionAdvanced.ThrowIfDefault(reportManifestArtifact);

        if (ArtifactKindToFileExtensionTable.TryGetValue(reportManifestArtifact.ArtifactKind, out var fileExtension))
        {
            // Do something with fileExtension if needed
        }

        _ = _artifacts.RemoveAll(artifact => artifact.ArtifactKind.Equals(reportManifestArtifact.ArtifactKind, StringComparison.OrdinalIgnoreCase));
        _ = _artifacts.Add(reportManifestArtifact);
    }
};
