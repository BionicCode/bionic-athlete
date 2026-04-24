namespace FitToCsvConverter.Data.Activities;

using System.Collections.Immutable;
using FitToCsvConverter.Data.Decoding;
using FitToCsvConverter.Data.Fields;

public sealed class FitActivity : FitNode
{
    public FitActivity(
        FitNodeSnapshot original,
        ImmutableArray<FitField> fields,
        ImmutableArray<FitSession> sessions,
        FitFileSource source,
        FitActivityAncillaryData ancillaryData)
        : base(original, fields)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(ancillaryData);

        Sessions = sessions.IsDefault ? ImmutableArray<FitSession>.Empty : sessions;
        Source = source;
        AncillaryData = ancillaryData;
    }

    /// <summary>
    /// Sessions that make up the decoded activity tree.
    /// </summary>
    public ImmutableArray<FitSession> Sessions { get; }

    public FitFileSource Source { get; }

    /// <summary>
    /// Non-tree FIT messages that were preserved during decode, such as Event, DeveloperDataId, and FieldDescription.
    /// </summary>
    public FitActivityAncillaryData AncillaryData { get; }

    /// <summary>
    /// Canonical activity date/time for presentation.
    /// This is the first session start when available, otherwise the activity message timestamp.
    /// </summary>
    public DateTimeOffset? CanonicalStartTimeUtc
    {
        get
        {
            // Activity messages are usually summary messages written after the session content,
            // so the first session start is the most useful "activity date" for presentation.
            foreach (FitSession session in Sessions)
            {
                if (session.CanonicalStartTimeUtc is DateTimeOffset sessionStartTime)
                {
                    return sessionStartTime;
                }
            }

            return Original.CanonicalTimestampUtc;
        }
    }
}
