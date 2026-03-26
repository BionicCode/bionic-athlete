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

    public ImmutableArray<FitSession> Sessions { get; }

    public FitFileSource Source { get; }

    public FitActivityAncillaryData AncillaryData { get; }

    public DateTimeOffset? CanonicalStartTimeUtc
    {
        get
        {
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
