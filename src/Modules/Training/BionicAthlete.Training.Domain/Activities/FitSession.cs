namespace BionicAthlete.Training.Domain.Activities;

using System.Collections.Immutable;
using BionicAthlete.Training.Domain.Fields;

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

    /// <summary>
    /// Laps that belong to this session.
    /// </summary>
    public ImmutableArray<FitLap> Laps { get; }

    /// <summary>
    /// Record messages that belong to this session.
    /// GPS position, heart rate, cadence, speed, and similar time-series values stay on the record fields.
    /// </summary>
    public ImmutableArray<FitRecord> Records { get; }

    /// <summary>
    /// Canonical session date/time for presentation.
    /// </summary>
    public DateTimeOffset? CanonicalStartTimeUtc => Original.CanonicalTimestampUtc;
}
