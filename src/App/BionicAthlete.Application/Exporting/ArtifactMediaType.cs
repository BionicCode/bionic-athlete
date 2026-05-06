namespace BionicAthlete.Application.Exporting;

using System.Collections.Frozen;

/// <summary>
/// Represents a common media type as defined for an artifact generated as part of a report package, such as the HTML report or PDF report.
/// </summary>
public class ArtifactMediaType
{
    public static FrozenSet<string>
    public const string Pdf = "application/pdf";
    public const string Html = "text/html";
    public const string Json = "application/json";

    public MediaTypeKind MediaTypeKind { get; init; }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1065:Do not raise exceptions in unexpected locations", Justification = "Required to improve robustness by avoiding missing cases. It's an implementation level exception that should never be thrown in production code.")]
    public override string ToString() => MediaTypeKind switch
    {
        MediaTypeKind.Pdf => Pdf,
        MediaTypeKind.Html => Html,
        MediaTypeKind.Json => Json,
        _ => throw new NotImplementedException($"Unsupported media type kind: {MediaTypeKind}")
    };
}

public enum MediaTypeKind
{
    Undefined = 0,
    Pdf = 1,
    Html = 2,
    Json = 3
}