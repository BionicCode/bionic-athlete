namespace FitToCsvConverter.Reporting;

using System.Text;
using System.Text.Json;

/// <summary>
/// Updates View C report manifests after UI-bound PDF generation succeeds.
/// </summary>
public sealed class ActivityReportManifestUpdater : IActivityReportManifestUpdater
{
    /// <inheritdoc />
    public async Task AddPdfArtifactAsync(
        HtmlReportPackage reportPackage,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(reportPackage);

        if (string.IsNullOrWhiteSpace(reportPackage.PdfFilePath))
        {
            return;
        }

        string json = await File.ReadAllTextAsync(reportPackage.ManifestFilePath, cancellationToken).ConfigureAwait(false);
        ActivityReportManifest manifest = JsonSerializer.Deserialize<ActivityReportManifest>(
            json,
            ActivityReportHtmlRenderer.ManifestJsonOptions)
            ?? throw new InvalidOperationException("The report manifest could not be deserialized.");

        ActivityReportManifest updatedManifest = ActivityReportHtmlRenderer.CreateManifestForUpdate(reportPackage, manifest);
        string updatedJson = JsonSerializer.Serialize(updatedManifest, ActivityReportHtmlRenderer.ManifestJsonOptions);
        await File.WriteAllTextAsync(reportPackage.ManifestFilePath, updatedJson, Encoding.UTF8, cancellationToken).ConfigureAwait(false);
    }
}
