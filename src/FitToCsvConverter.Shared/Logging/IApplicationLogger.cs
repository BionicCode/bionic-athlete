namespace FitToCsvConverter.Shared.Logging;

using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

public interface IApplicationLogger<TService> : ILogger<TService>
{
    void LogErrorMessage(string message, [CallerMemberName] string? callerMemberName = null, [CallerLineNumber] int callerLineNumber = -1);
    void LogException(Exception exception, [CallerMemberName] string? callerMemberName = null, [CallerLineNumber] int callerLineNumber = -1);
    void LogInformationMessage(string message, [CallerMemberName] string? callerMemberName = null, [CallerLineNumber] int callerLineNumber = -1);
    void LogInformationObject(object obj, [CallerMemberName] string? callerMemberName = null, [CallerLineNumber] int callerLineNumber = -1);
    void LogErrorObject(object obj, [CallerMemberName] string? callerMemberName = null, [CallerLineNumber] int callerLineNumber = -1);
}