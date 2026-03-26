namespace FitToCsvConverter.Data.Activities;

public sealed class FitNodeSnapshot
{
    public FitNodeSnapshot(
        FitNodeIdentity identity,
        ushort messageNumber,
        string messageName,
        byte? localMessageNumber,
        DateTimeOffset? timestampUtc,
        DateTimeOffset? startTimeUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(messageName);

        Identity = identity;
        MessageNumber = messageNumber;
        MessageName = messageName;
        LocalMessageNumber = localMessageNumber;
        TimestampUtc = timestampUtc;
        StartTimeUtc = startTimeUtc;
    }

    public FitNodeIdentity Identity { get; }

    public ushort MessageNumber { get; }

    public string MessageName { get; }

    public byte? LocalMessageNumber { get; }

    public DateTimeOffset? TimestampUtc { get; }

    public DateTimeOffset? StartTimeUtc { get; }

    public DateTimeOffset? CanonicalTimestampUtc => StartTimeUtc ?? TimestampUtc;
}
