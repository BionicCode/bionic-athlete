using System.Runtime.CompilerServices;

[assembly: RuntimeCompatibilityAttribute(WrapNonExceptionThrows = true)]

namespace FitToCsvConverter.Main;

using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Threading;
using FitToCsvConverter.Controls;
using FitToCsvConverter.Data;
using FitToCsvConverter.Data.Caching;
using FitToCsvConverter.Data.Decoding;
using FitToCsvConverter.Data.Decoding.Garmin;
using FitToCsvConverter.Shared.Logging;
using FitToCsvConverter.ViewModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Core;

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

        string appDataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FitToCsvConverter");

        string logDir = Path.Combine(appDataDir, "Logs");
        if (!Directory.Exists(logDir))
        {
            _ = Directory.CreateDirectory(logDir);
        }

        string seqUrl = hostBuilder.Configuration["Seq:ServerUrl"] ?? "http://localhost:5341";
        string? seqApiKey = hostBuilder.Configuration["Seq:ApiKey"];

        // Add logging with Serilog
        string debugOutputTemplate = @$"[{{{LoggerProperties.Timestamp}:yyyy/MM/dd HH:mm:ss.fff zzz}}] [{{{LoggerProperties.Level}}}] [{{{LoggerProperties.SourceContext}:s}}] [{{{LoggerProperties.CallerMemberName}}}] [{{{LoggerProperties.CallerLineNumber}}}] [{{{LoggerProperties.Message}}}] [{{{LoggerProperties.NewLine}}}] [{{{LoggerProperties.Exception}}}]";
        Logger logger = new LoggerConfiguration()
            .Enrich.WithThreadName()
            .Enrich.WithThreadId()
            .Enrich.FromLogContext()

            .WriteTo.Async(action => action.Seq(seqUrl,
            apiKey: seqApiKey,
            restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Information,
            formatProvider: CultureInfo.CurrentCulture))

            .WriteTo.Async(action => action.File("application_log_default.log",
            formatProvider: CultureInfo.CurrentCulture,
            restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Warning,
            encoding: Encoding.UTF8,
            outputTemplate: debugOutputTemplate,
            rollOnFileSizeLimit: true,
            rollingInterval: RollingInterval.Day,
            fileSizeLimitBytes: 10 * 1024 * 1024,
            retainedFileCountLimit: 14,
            buffered: true,
            flushToDiskInterval: TimeSpan.FromSeconds(1))) // 10MB

            .CreateLogger();

        _ = hostBuilder.Services.AddLogging(loggingBuilder =>
            {
                _ = loggingBuilder.AddSerilog(logger, dispose: true);
            });

        _ = hostBuilder.Services.AddSingleton(typeof(IApplicationLogger<>), typeof(ApplicationLogger<>))
            .AddSingleton<ITemporaryFileManager, TemporaryFileManager>()
            .AddSingleton<IZipArchiveManager, ZipArchiveManager>()
            .AddSingleton<IGarminFitCsvToolConverter, GarminFitCsvToolConverter>()
            .AddKeyedSingleton<IFitActivityDecoder, GarminFitActivityDecoder>(CoreDecoderServiceKey)
            .AddSingleton<IFitActivityCache, InMemoryFitActivityCache>()
            .AddSingleton<IFitActivityDecoder>(serviceProvider =>
                {
                    return new CachingFitActivityDecoder(
                        serviceProvider.GetRequiredKeyedService<IFitActivityDecoder>(CoreDecoderServiceKey),
                        serviceProvider.GetRequiredService<IFitActivityCache>());
                })
            .AddFactory<IFitActivityDecoder>(ServiceLifetime.Singleton)
            .AddSingleton<MainViewModel>()
            .AddSingleton<MainWindow>();

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

