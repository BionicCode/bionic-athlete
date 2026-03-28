namespace FitToCsvConverter.Shared.Logging;

public sealed class SeqOptions
{
    public const string SectionName = "Seq";

    public string ServerUrl { get; init; } = "http://localhost:5341";

    public string? ApiKey { get; init; }
}