namespace FitToCsvConverter.Data.Decoding;

public sealed record FitDecodeIssue(FitDecodeIssueSeverity Severity, string Message);
