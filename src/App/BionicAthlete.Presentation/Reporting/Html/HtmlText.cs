namespace BionicAthlete.Presentation.Reporting.Html;

using System.Net;

internal static class HtmlText
{
    public static string Encode(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);
}
