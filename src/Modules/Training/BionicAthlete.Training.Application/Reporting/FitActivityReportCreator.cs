namespace BionicAthlete.Training.Application.Reporting;

using System.Collections.Immutable;
using System.Text;
using BionicAthlete.Application;
using BionicAthlete.Application.Exporting;
using BionicAthlete.Application.Reporting;
using BionicAthlete.Application.Reporting.Html;
using BionicAthlete.Training.Domain.Activities;
using BionicCode.Utilities.Net;

public class FitActivityReportCreator
{
    private readonly IActivityReportProjector _activityReportProjector;
    private readonly IReportHtmlRenderer _activityReportHtmlRenderer;
    private readonly IHtmlExporterArgsFactory _htmlExporterArgsFactory;
    private readonly IHtmlExporter _htmlExporter;

    public FitActivityReportCreator(
        IActivityReportProjector activityReportProjector,
        IReportHtmlRenderer htmlRenderer,
        IHtmlExporterArgsFactory htmlExporterArgsFactory,
        IHtmlExporter htmlExporter)
    {
        _activityReportProjector = activityReportProjector;
        _activityReportHtmlRenderer = htmlRenderer;
        _htmlExporterArgsFactory = htmlExporterArgsFactory;
        _htmlExporter = htmlExporter;
    }

    public async Task<HtmlReportPackage> CreateHtmlReportAsync(
        FitFileExportData exportData,
        ReportOutputTarget outputTarget,
        bool isOverWriteExistingAllowed,
        CancellationToken cancellationToken)
    {
        // TODO::Add progress reporting and cancellation support

        ArgumentNullExceptionAdvanced.ThrowIfNull(exportData);

        Report report = await _activityReportProjector
            .ProjectAsync(exportData.Activity, exportData.ExportOptions, cancellationToken)
            .ConfigureAwait(true);
        HtmlDocument htmlDocument = await RenderHtmlAsync(exportData, report, cancellationToken);

        FileDescriptor htmlFilePath = default;
        ReportManifestBuilder? manifestBuilder = null;

        // If ReportOutputTarget.PdfFromGeneratedHtml then the  HTML must not be persisted to the file system,
        // because it is only used as an intermediate step for PDF generation.
        // In all other cases, the generated HTML must be persisted to the file system.
        if (outputTarget is not ReportOutputTarget.PdfFromGeneratedHtml)
        {
            string fileNameWithoutExtension = exportData.FitFileDescriptor.NameWithoutExtension;
            string fileName = $"{fileNameWithoutExtension}{FileExtensions.Html}";
            htmlFilePath = new FileDescriptor(Path.Combine(exportData.OutputDirectoryPath.FullPath, fileName));
            var exportUri = new Uri(htmlFilePath.FullPath);
            HtmlExporterArgs htmlExporterArgs = _htmlExporterArgsFactory.Create(
                htmlDocument,
                exportUri,
                isOverWriteExistingAllowed,
                Encoding.UTF8);
            await _htmlExporter.ExportAsync(htmlExporterArgs, cancellationToken);

            var reportDescriptor = ReportDescriptor.Create(htmlDocument, report, outputTarget);
            manifestBuilder = await ReportManifestBuilder.CreateAsync(reportDescriptor, exportData.OutputDirectoryPath, cancellationToken).ConfigureAwait(false);
            manifestBuilder.AddArtifact(ArtifactKind.HtmlReport, fileName);
            _ = await manifestBuilder.BuildAsync(cancellationToken).ConfigureAwait(false);
        }

        ImmutableArray<ReportDiagnostic> diagnostics = report.Diagnostics.IsDefault
            ? ImmutableArray<ReportDiagnostic>.Empty
            : report.Diagnostics;

        return new HtmlReportPackage(
            exportData.OutputDirectoryPath,
            htmlFilePath,
            manifestBuilder,
            outputTarget,
            exportData.ExportOptions.PageSettings,
            diagnostics);
    }

    internal async Task<HtmlDocument> RenderHtmlAsync(FitFileExportData exportData, Report report, CancellationToken cancellationToken)
    {
        ArgumentNullExceptionAdvanced.ThrowIfNull(exportData);

        return await _activityReportHtmlRenderer
        .RenderAsync(report, exportData.ExportOptions, cancellationToken)
        .ConfigureAwait(true);
    }
}

public sealed class FitFileExportData
{
    public FitFileExportData(FileDescriptor fitFileDescriptor, FitActivity activity, DirectoryDescriptor outputDirectoryPath, ReportExportOptions exportOptions)
    {
        FitFileDescriptor = fitFileDescriptor;
        Activity = activity;
        OutputDirectoryPath = outputDirectoryPath;
        ExportOptions = exportOptions;
    }

    public FileDescriptor FitFileDescriptor { get; init; }
    public FitActivity Activity { get; init; }
    public DirectoryDescriptor OutputDirectoryPath { get; init; }
    public ReportExportOptions ExportOptions { get; init; }
}