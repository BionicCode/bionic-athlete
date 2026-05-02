namespace BionicAthlete.Application.Reporting;

using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// Updates View C report manifests after UI-bound PDF generation succeeds.
/// </summary>
public sealed class ReportManifestManager : IReportManifestManager
{
    private static readonly JsonSerializerOptions s_manifestJsonOptions = CreateManifestJsonOptions();
    internal static JsonSerializerOptions ManifestJsonOptions => s_manifestJsonOptions;

    public static async Task WriteManifestAsync(
        string manifestFilePath,
        ReportManifest manifest,
        CancellationToken cancellationToken)
    {
        string json = JsonSerializer.Serialize(manifest, ManifestJsonOptions);
        await File.WriteAllTextAsync(manifestFilePath, json, Encoding.UTF8, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public static async Task AddPdfArtifactAsync(
        string manifestFilePath,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(manifestFilePath);

        string json = await File.ReadAllTextAsync(manifestFilePath, cancellationToken).ConfigureAwait(false);
        ReportManifest manifest = JsonSerializer.Deserialize<ReportManifest>(
            json,
            ManifestJsonOptions)
            ?? throw new InvalidOperationException("The report manifest could not be deserialized.");

        ReportManifest updatedManifest = ReportManifestCreator.CreateManifestForUpdate(manifestFilePath, manifest);
        string updatedJson = JsonSerializer.Serialize(updatedManifest, ManifestJsonOptions);
        await File.WriteAllTextAsync(manifestFilePath, updatedJson, Encoding.UTF8, cancellationToken).ConfigureAwait(false);
    }

    private static JsonSerializerOptions CreateManifestJsonOptions()
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
