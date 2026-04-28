namespace BionicAthlete.Shared.Logging;

public sealed class EmailOptions
{
    public const string SectionName = "Email";

    public string From { get; init; } = "application.logging@bioniccode.net";

    public string To { get; init; } = "application.logging@bioniccode.net";

    public string SmtpServer { get; init; } = "localhost";
}