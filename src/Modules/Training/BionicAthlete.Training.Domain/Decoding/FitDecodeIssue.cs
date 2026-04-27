namespace BionicAthlete.Training.Domain.Decoding;

public sealed record FitDecodeIssue(FitDecodeIssueSeverity Severity, string Message);
