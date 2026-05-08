namespace BionicAthlete.Application.Reporting;

using System.Collections.Immutable;
using System.Collections.ObjectModel;
using BionicAthlete.Application.Exporting;
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

    public ReadOnlySet<ReportManifestArtifact> Artifacts { get; }
    public int ReportSchemaVersion { get; }
    public string RendererVersion { get; }
    public string ReportId { get; }
    public string SourceFilePath { get; }
    public DateTimeOffset ExportTimestampUtc { get; }
    public ReportOutputTarget OutputTarget { get; }
    public PagePreset PagePreset { get; }
    public ImmutableArray<string> IncludedSections { get; }
    public ImmutableArray<ReportDiagnostic> Diagnostics { get; }
    public bool IsDirty { get; private set; }

    internal ReportManifest(
        int reportSchemaVersion,
        string rendererVersion,
        string reportId,
        string sourceFilePath,
        DateTimeOffset exportTimestampUtc,
        ReportOutputTarget outputTarget,
        PagePreset pagePreset,
        ImmutableArray<string> includedSections,
        ImmutableArray<ReportDiagnostic> diagnostics)
    {
        ReportSchemaVersion = reportSchemaVersion;
        RendererVersion = rendererVersion;
        ReportId = reportId;
        SourceFilePath = sourceFilePath;
        ExportTimestampUtc = exportTimestampUtc;
        OutputTarget = outputTarget;
        PagePreset = pagePreset;
        _artifacts = [];
        Artifacts = new ReadOnlySet<ReportManifestArtifact>(_artifacts);
        IncludedSections = includedSections;
        Diagnostics = diagnostics;
    }

    public static Task<ReportManifestBuilder> CreateBuilderAsync(ReportDescriptor reportDescriptor, DirectoryDescriptor outputFolder, CancellationToken cancellationToken) => ReportManifestBuilder.CreateAsync(reportDescriptor, outputFolder, cancellationToken);

    public static ReportManifestBuilder Create(ReportManifest manifest, DirectoryDescriptor outputFolder) => ReportManifestBuilder.Create(manifest, outputFolder);

    /// <inheritdoc />
    public void AddArtifact(ArtifactKind artifactKind, FileDescriptor relativeArtifactFilePath)
    {
        ArgumentNullExceptionAdvanced.ThrowIfDefault(relativeArtifactFilePath);
        ArgumentExceptionAdvanced.ThrowIfFalse(
            relativeArtifactFilePath.IsRelative,
            $"The argument '{nameof(relativeArtifactFilePath)}' must be a relative file path, but an absolute path was provided: '{relativeArtifactFilePath.FullPath}'.");
        ArgumentExceptionAdvanced.ThrowIfEnumIsNotDefined<ArtifactKind>(artifactKind);
        ArgumentExceptionAdvanced.ThrowIfEnumEqualsAny(artifactKind, [ArtifactKind.Undefined]);

        var reportManifestArtifact = ReportManifestArtifact.Create(artifactKind, relativeArtifactFilePath);
        if (!_artifacts.Add(reportManifestArtifact))
        {
            throw new InvalidOperationException($"The artifact for '{reportManifestArtifact.RelativePath}' already exists in the manifest.");
        }

        IsDirty = true;
    }

    public void UpdateManifest(ReportManifestArtifact reportManifestArtifact)
    {
        ArgumentNullExceptionAdvanced.ThrowIfDefault(reportManifestArtifact);

        _ = _artifacts.RemoveWhere(artifact => artifact.Equals(reportManifestArtifact));
        IsDirty = _artifacts.Add(reportManifestArtifact);
    }

    public void SetIsCommitted() => IsDirty = false;
};

