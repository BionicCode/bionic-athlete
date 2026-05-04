using System.Runtime.CompilerServices;

[assembly: RuntimeCompatibilityAttribute(WrapNonExceptionThrows = true)]

namespace BionicAthlete.Desktop;

using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Threading;
using BionicAthlete.Application.Reporting;
using BionicAthlete.FileSystem.Abstractions;
using BionicAthlete.Infrastructure.FileSystem;
using BionicAthlete.Presentation;
using BionicAthlete.Presentation.Reporting;
using BionicAthlete.Shared.Logging;
using BionicAthlete.Training.Application.Caching;
using BionicAthlete.Training.Application.Decoding;
using BionicAthlete.Training.Application.Reporting;
using BionicAthlete.Training.Exporting;
using BionicAthlete.Training.Infrastructure.GarminFit.Caching;
using BionicAthlete.Training.Infrastructure.GarminFit.Decoding;
using BionicAthlete.Training.Presentation.ViewModel;
using BionicAthlete.Training.Reporting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting.Json;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private ITemporaryFileManager? _temporaryFileManager;
    private IHost? _host;
    private IApplicationLogger<App> _logger = NullApplicationLogger<App>.Instance;
    private bool _isCleaningUp;
    private const string CoreDecoderServiceKey = "coreDecoder";

    static App() => FrameworkElement.LanguageProperty.OverrideMetadata(
          typeof(FrameworkElement),
          new FrameworkPropertyMetadata(
            defaultValue: XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.IetfLanguageTag)));

    protected override void OnStartup(StartupEventArgs e)
    {
        DispatcherUnhandledException += OnUnhandledDispatcherException;
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledDomainException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        bool hasRemovedLeftovers = RecoveryCleanup();

        HostApplicationBuilder hostBuilder = Host.CreateApplicationBuilder();
        _ = hostBuilder.Services.Configure<SeqOptions>(
    hostBuilder.Configuration.GetSection(SeqOptions.SectionName));
        _ = hostBuilder.Services.Configure<EmailOptions>(
            hostBuilder.Configuration.GetSection(EmailOptions.SectionName));

        string seqUrl = hostBuilder.Configuration["Seq:ServerUrl"] ?? "http://localhost:5341";
        string? seqApiKey = hostBuilder.Configuration["Seq:ApiKey"];

        // Add logging with Serilog
        string debugOutputTemplate = @$"[{{{LoggerProperties.Timestamp}:yyyy/MM/dd HH:mm:ss.fff zzz}}] [{{{LoggerProperties.Level}}}] [{{{LoggerProperties.SourceContext}:s}}] [{{{LoggerProperties.CallerMemberName}}}] [{{{LoggerProperties.CallerLineNumber}}}] {{{LoggerProperties.Message}:lj}} {{{LoggerProperties.NewLine}}} {{{LoggerProperties.Exception}}}";
        LoggerConfiguration loggerConfiguration = new LoggerConfiguration()
            .Enrich.WithThreadName()
            .Enrich.WithThreadId()
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", "FitToCsvConverter")
            .Enrich.WithProperty("ApplicationVersion", GetType().Assembly.GetName().Version?.ToString() ?? "Unknown")

            .WriteTo.Async(action => action.Seq(seqUrl,
            apiKey: seqApiKey,
            restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Information,
            formatProvider: CultureInfo.CurrentCulture));

        string appDataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FitToCsvConverter");
        string logDir = Path.Combine(appDataDir, "Logs");
        _ = Directory.CreateDirectory(logDir);

        string errorLogFilePath = Path.Combine(logDir, "application_log_error.log");
        loggerConfiguration = loggerConfiguration.WriteTo.Async(action => action.File(errorLogFilePath,
            formatProvider: CultureInfo.CurrentCulture,
            restrictedToMinimumLevel: LogEventLevel.Warning,
            encoding: Encoding.UTF8,
            outputTemplate: debugOutputTemplate,
            rollOnFileSizeLimit: true,
            rollingInterval: RollingInterval.Day,
            fileSizeLimitBytes: 10 * 1024 * 1024, // 10MB
            retainedFileCountLimit: 14,
            buffered: true,
            flushToDiskInterval: TimeSpan.FromSeconds(1)));

        string errorLogJsonPath = Path.Combine(logDir, "application_log_error.json");
        loggerConfiguration = loggerConfiguration.WriteTo.Async(action => action.File(
            new JsonFormatter(renderMessage: true),
            errorLogJsonPath,
            restrictedToMinimumLevel: LogEventLevel.Warning,
            encoding: Encoding.UTF8,
            rollOnFileSizeLimit: true,
            rollingInterval: RollingInterval.Day,
            fileSizeLimitBytes: 10 * 1024 * 1024, // 10MB
            retainedFileCountLimit: 14,
            buffered: true,
            flushToDiskInterval: TimeSpan.FromSeconds(1)));

        string debugLogFilePath = Path.Combine(logDir, "application_log_debug.log");
        loggerConfiguration = loggerConfiguration.WriteTo.Async(action => action.File(debugLogFilePath,
            formatProvider: CultureInfo.CurrentCulture,
            restrictedToMinimumLevel: LogEventLevel.Debug,
            encoding: Encoding.UTF8,
            outputTemplate: debugOutputTemplate,
            rollOnFileSizeLimit: true,
            rollingInterval: RollingInterval.Day,
            fileSizeLimitBytes: 10 * 1024 * 1024, // 10MB
            retainedFileCountLimit: 14,
            buffered: true,
            flushToDiskInterval: TimeSpan.FromSeconds(1)));

        string debugLogJsonPath = Path.Combine(logDir, "application_log_debug.json");
        loggerConfiguration = loggerConfiguration.WriteTo.Async(action => action.File(
            new JsonFormatter(renderMessage: true),
            debugLogJsonPath,
            restrictedToMinimumLevel: LogEventLevel.Debug,
            encoding: Encoding.UTF8,
            rollOnFileSizeLimit: true,
            rollingInterval: RollingInterval.Day,
            fileSizeLimitBytes: 10 * 1024 * 1024, // 10MB
            retainedFileCountLimit: 14,
            buffered: true,
            flushToDiskInterval: TimeSpan.FromSeconds(1)));

        string verboseLogFilePath = Path.Combine(logDir, "application_log_verbose.log");
        Logger logger = loggerConfiguration.WriteTo.Async(action => action.File(verboseLogFilePath,
            formatProvider: CultureInfo.CurrentCulture,
            restrictedToMinimumLevel: LogEventLevel.Verbose,
            encoding: Encoding.UTF8,
            outputTemplate: debugOutputTemplate,
            rollOnFileSizeLimit: true,
            rollingInterval: RollingInterval.Day,
            fileSizeLimitBytes: 10 * 1024 * 1024, // 10MB
            retainedFileCountLimit: 14,
            buffered: true,
            flushToDiskInterval: TimeSpan.FromSeconds(1)))

            .CreateLogger();

        // TODO::Dispatch logs or batches of logs to web server via HTTPS for relaying via email or other channels to avoid issues with credential storage.

        _ = hostBuilder.Services.AddLogging(loggingBuilder =>
            {
                _ = loggingBuilder.AddSerilog(logger, dispose: true);
            });

        _ = hostBuilder.Services.AddSingleton(typeof(IApplicationLogger<>), typeof(ApplicationLogger<>))
            .AddSingleton<ITemporaryFileManager, TemporaryFileManager>()
            .AddSingleton<IZipArchiveManager, ZipArchiveManager>()
            .AddSingleton<ICsvActivityExporter, CsvActivityExporter>()
            .AddSingleton<IReportChartRenderer, InlineSvgReportChartRenderer>()
            .AddSingleton<IActivityReportProjector, ActivityReportProjector>()
            .AddSingleton<IReportHtmlRenderer, ReportHtmlRenderer>()
            .AddSingleton<IReportPdfExporter, WebView2PdfExporter>()
            .AddKeyedSingleton<IFitActivityDecoder, GarminFitActivityDecoder>(CoreDecoderServiceKey)
            .AddSingleton<IFitActivityCache, InMemoryFitActivityCache>()
            .AddSingleton<IFitActivityDecoder>(serviceProvider =>
                {
                    return new CachingFitActivityDecoder(
                        serviceProvider.GetRequiredKeyedService<IFitActivityDecoder>(CoreDecoderServiceKey),
                        serviceProvider.GetRequiredService<IFitActivityCache>());
                })
            .AddKeyedSingleton<IFitActivityReportManifestHandler, FitActivityReportManifestHandler>()
            .AddKeyedSingleton<IFileManager<string>, TextFileManager>()
            .AddSingleton<MainViewModel>()
            .AddSingleton<MainWindow>()
            .AddSingleton<IHtmlExporterArgsFactory, HtmlFileExporterArgsFactory>()
            .AddFactory<IFitActivityDecoder>(ServiceLifetime.Singleton);

        _host = hostBuilder.Build();
        _host.Start();
        _logger = _host.Services.GetRequiredService<IApplicationLogger<App>>();
        _temporaryFileManager = _host.Services.GetRequiredService<ITemporaryFileManager>();

        _logger.LogInformationMessage("##### Starting application. #####");
        _logger.LogInformationMessage($"{(hasRemovedLeftovers ? "Removed leftover temporary files." : "No leftover temporary files found.")}");

        base.OnStartup(e);

        MainWindow mainWindow = _host.Services.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }

    private static bool RecoveryCleanup()
    {
        bool hasRemovedLeftovers = false;

        // TODO::Implement recovery cleanup on next application startup to handle cases where the application may have been terminated unexpectedly,
        return hasRemovedLeftovers;
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        _logger.LogException(e.Exception);
        e.SetObserved();
    }

    private void OnUnhandledDomainException(object sender, UnhandledExceptionEventArgs e)
    {
        // The 'RuntimeCompatibilityAttribute' ensures this will always succeed
        // by wrapping non-Exception throws in a 'RuntimeWrappedException', but we check just in case.
        if (e.ExceptionObject is Exception ex)
        {
            _logger.LogException(ex);
        }

        if (!e.IsTerminating)
        {
            Shutdown();
        }
    }

    private void OnUnhandledDispatcherException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        _logger.LogException(e.Exception);
        e.Handled = true;
        Shutdown();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _logger.LogInformationMessage("----- Exiting application. -----");
        base.OnExit(e);
        Cleanup();
    }

    private void Cleanup()
    {
        if (_isCleaningUp)
        {
            return;
        }

        _isCleaningUp = true;
        _temporaryFileManager?.CleanUpTemporaryFiles();
        _host?.Dispose();
    }
}

