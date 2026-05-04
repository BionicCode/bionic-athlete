namespace BionicAthlete.Application.Reporting.Html;

using System.Net;

public static class HtmlText
{
    public static string Encode(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);
}
