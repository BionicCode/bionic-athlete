namespace FitToCsvConverter.Data.Decoding;

using System.Collections.Immutable;
using System.Linq;
using FitToCsvConverter.Data.Activities;

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

    public FitActivity? Activity { get; }

    public FitFileSource Source { get; }

    public ImmutableArray<FitDecodeIssue> Issues { get; }

    public bool IsFromCache { get; }

    public bool IsSuccess => Activity is not null && Issues.All(issue => issue.Severity != FitDecodeIssueSeverity.Error);
}
