namespace BionicAthlete.Training.Application.Decoding;

using System.Collections.Immutable;
using System.Linq;
using BionicAthlete.Training.Domain;
using BionicAthlete.Training.Domain.Activities;

public sealed class FitActivityDecodeResult
{
    public FitActivityDecodeResult(
        FitActivity? activity,
        FitFileSource source,
        ImmutableArray<FitDecodeIssue> issues,
        bool isFromCache = false)
    {
        ArgumentNullException.ThrowIfNull(source);

        Activity = activity;
        Source = source;
        Issues = issues.IsDefault ? ImmutableArray<FitDecodeIssue>.Empty : issues;
        IsFromCache = isFromCache;
    }

    /// <summary>
    /// Decoded activity tree, or null when decoding failed.
    /// </summary>
    public FitActivity? Activity { get; }

    public FitFileSource Source { get; }

    public ImmutableArray<FitDecodeIssue> Issues { get; }

    public bool IsFromCache { get; }

    /// <summary>
    /// True when an activity was produced and no error-severity issues were recorded.
    /// </summary>
    public bool IsSuccess => Activity is not null && Issues.All(issue => issue.Severity != FitDecodeIssueSeverity.Error);
}
