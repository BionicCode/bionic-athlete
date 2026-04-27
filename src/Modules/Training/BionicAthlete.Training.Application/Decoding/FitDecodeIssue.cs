namespace BionicAthlete.Training.Application.Decoding;

public sealed record FitDecodeIssue(FitDecodeIssueSeverity Severity, string Message);
