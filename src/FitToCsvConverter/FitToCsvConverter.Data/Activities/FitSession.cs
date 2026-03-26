namespace FitToCsvConverter.Data.Activities;

using System.Collections.Immutable;
using FitToCsvConverter.Data.Fields;

public sealed class FitSession : FitNode
{
    public FitSession(
        FitNodeSnapshot original,
        ImmutableArray<FitField> fields,
        ImmutableArray<FitLap> laps,
        ImmutableArray<FitRecord> records)
        : base(original, fields)
    {
        Laps = laps.IsDefault ? ImmutableArray<FitLap>.Empty : laps;
        Records = records.IsDefault ? ImmutableArray<FitRecord>.Empty : records;
    }

    public ImmutableArray<FitLap> Laps { get; }

    public ImmutableArray<FitRecord> Records { get; }

    public DateTimeOffset? CanonicalStartTimeUtc => Original.CanonicalTimestampUtc;
}
