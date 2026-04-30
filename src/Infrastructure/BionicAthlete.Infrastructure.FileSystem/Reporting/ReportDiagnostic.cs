namespace BionicAthlete.Infrastructure.FileSystem.Reporting;

/// <summary>
/// A warning, caveat, or informational diagnostic emitted while creating a report.
/// </summary>
/// <param name="Code">Stable diagnostic code.</param>
/// <param name="Message">Human-readable diagnostic message.</param>
public sealed record ReportDiagnostic(string Code, string Message);
