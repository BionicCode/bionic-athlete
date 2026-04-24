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

    /// <summary>
    /// Stable identity within the decoded activity tree.
    /// </summary>
    public FitNodeIdentity Identity { get; }

    public ushort MessageNumber { get; }

    public string MessageName { get; }

    public byte? LocalMessageNumber { get; }

    /// <summary>
    /// Original FIT timestamp value for the source message, if the message exposes one.
    /// </summary>
    public DateTimeOffset? TimestampUtc { get; }

    /// <summary>
    /// Original FIT start_time value for the source message, if the message exposes one.
    /// </summary>
    public DateTimeOffset? StartTimeUtc { get; }

    /// <summary>
    /// Preferred presentation timestamp.
    /// For nodes that have both start_time and timestamp, start_time is treated as the canonical date/time.
    /// </summary>
    public DateTimeOffset? CanonicalTimestampUtc => StartTimeUtc ?? TimestampUtc;
}
