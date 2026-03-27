namespace FitToCsvConverter.Shared.Logging;

using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

/// <summary>
/// Minimalistic logger that does nothing.
/// </summary>
public class NullApplicationLogger<T> : IApplicationLogger<T>
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1063:Implement IDisposable Correctly", Justification = "Dummy implementation.")]
    private class NullScope : IDisposable
    {
        public static NullScope Instance { get; } = new NullScope();

        private NullScope()
        {
        }

        /// <inheritdoc />
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1063:Implement IDisposable Correctly", Justification = "Dummy implementation.")]
        public void Dispose()
        {
        }
    }

    /// <summary>
    /// Returns an instance of <see cref="NullLogger{T}"/>.
    /// </summary>
    /// <returns>An instance of <see cref="NullApplicationLogger{T}"/>.</returns>
    public static readonly NullApplicationLogger<T> Instance = new();

    /// <inheritdoc />
    public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

    /// <inheritdoc />
    /// <remarks>
    /// This method ignores the parameters and does nothing.
    /// </remarks>
    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
    }

    /// <inheritdoc />
    public bool IsEnabled(LogLevel logLevel) => false;

    public void LogErrorMessage(string message, [CallerMemberName] string? callerMemberName = null, [CallerLineNumber] int callerLineNumber = -1) { }
    public void LogException(Exception exception, [CallerMemberName] string? callerMemberName = null, [CallerLineNumber] int callerLineNumber = -1) { }
    public void LogInformationMessage(string message, [CallerMemberName] string? callerMemberName = null, [CallerLineNumber] int callerLineNumber = -1) { }
}

