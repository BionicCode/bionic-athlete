namespace FitToCsvConverter.Main;

using System.Windows;
using System.Windows.Threading;
using FitToCsvConverter.Controls;
using FitToCsvConverter.Data;
using FitToCsvConverter.ViewModel;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private ITemporaryFileManager? _temporaryFileManager;
    private ServiceProvider? _serviceProvider;

    protected override void OnStartup(StartupEventArgs e)
    {
        RecoveryCleanup();
        base.OnStartup(e);
        DispatcherUnhandledException += OnUnhandledDispatcherException;
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledDomainException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
        var services = new ServiceCollection();
        _ = services.AddSingleton<ITemporaryFileManager, TemporaryFileManager>()
            .AddSingleton<IZipArchiveManager, ZipArchiveManager>()
            .AddSingleton<IGarminFitCsvToolConverter, GarminFitCsvToolConverter>()
            .AddSingleton<MainViewModel>()
            .AddSingleton<MainWindow>();

        _serviceProvider = services.BuildServiceProvider();
        _temporaryFileManager = _serviceProvider.GetRequiredService<ITemporaryFileManager>();
        MainWindow mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }

    private void RecoveryCleanup()
    {
        // TODO::Implement recovery cleanup on next application startup to handle cases where the application may have been terminated unexpectedly,
        // leaving temporary files behind. This could involve checking for and deleting any temporary files
        // that were registered in the previous session before starting the main application logic.
        //
        // For this purpose, consider implementing a mechanism to persist the list of registered temporary files across sessions,
        // such as writing them to a file in the application's data directory,
        // and then cleaning them up on the next startup before initializing the main application components.
    }

    // TODO::Add logging of unhandled exceptions to a file in the application data folder.
    // Consider using Serilog or a similar logging library for this purpose.
    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e) => Cleanp();
    private void OnUnhandledDomainException(object sender, UnhandledExceptionEventArgs e) => Cleanp();
    private void OnUnhandledDispatcherException(object sender, DispatcherUnhandledExceptionEventArgs e) => Cleanp();

    protected override void OnExit(ExitEventArgs e)
    {
        base.OnExit(e);
        Cleanp();
    }

    private void Cleanp()
    {
        _serviceProvider?.Dispose();
        _temporaryFileManager?.CleanUpTemporaryFiles();
    }
}

