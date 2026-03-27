namespace FitToCsvConverter.Shared.Logging;

using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

public partial class ApplicationLogger<TService> : ILogger<TService>, IApplicationLogger<TService>
{
    private readonly ILogger<TService> _logger;

    public ApplicationLogger(ILogger<TService> logger) => _logger = logger;

    public void LogInformationMessage(string message, [CallerMemberName] string? callerMemberName = null, [CallerLineNumber] int callerLineNumber = -1)
    {
        using IDisposable? scope = BeginCallerScope(callerMemberName, callerLineNumber);
        LogInformation(_logger, message);
    }

    public void LogErrorMessage(string message, [CallerMemberName] string? callerMemberName = null, [CallerLineNumber] int callerLineNumber = -1)
    {
        using IDisposable? scope = BeginCallerScope(callerMemberName, callerLineNumber);
        LogError(_logger, message);
    }

    public void LogException(Exception exception, [CallerMemberName] string? callerMemberName = null, [CallerLineNumber] int callerLineNumber = -1)
    {
        using IDisposable? scope = BeginCallerScope(callerMemberName, callerLineNumber);
        LogUnhandledException(_logger, exception);
    }

    private IDisposable? BeginCallerScope(
    [CallerMemberName] string? callerMemberName = null,
    [CallerLineNumber] int callerLineNumber = -1)
    => _logger.BeginScope(new Dictionary<string, object?>
    {
        [LoggerProperties.CallerMemberName] = callerMemberName,
        [LoggerProperties.CallerLineNumber] = callerLineNumber
    });

    [LoggerMessage(
        EventId = 0,
        Level = LogLevel.Critical,
        Message = "An unhandled exception occurred.")]
    private static partial void LogUnhandledException(ILogger<TService> logger, Exception exception);

    [LoggerMessage(
        EventId = 0,
        Level = LogLevel.Error,
        Message = "{Message}")]
    private static partial void LogError(ILogger<TService> logger, string message);

    [LoggerMessage(
        EventId = 0,
        Level = LogLevel.Information,
        Message = "{Message}")]
    private static partial void LogInformation(ILogger<TService> logger, string message);

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => _logger.BeginScope(state);
    public bool IsEnabled(LogLevel logLevel) => _logger.IsEnabled(logLevel);
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) => _logger.Log(logLevel, eventId, state, exception, formatter);
}

