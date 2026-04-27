namespace BionicAthlete.Training.Application.Persistence;
/// <summary>
/// Describes a local activity-history query.
/// </summary>
public sealed class ActivityHistoryQuery
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ActivityHistoryQuery"/> class.
    /// </summary>
    public ActivityHistoryQuery(
        DateTimeOffset? importedAfterUtc = null,
        DateTimeOffset? importedBeforeUtc = null,
        DateTimeOffset? activityStartedAfterUtc = null,
        DateTimeOffset? activityStartedBeforeUtc = null,
        int? maximumCount = null)
    {
        if (maximumCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumCount), maximumCount, "Maximum count must be zero or greater.");
        }

        ImportedAfterUtc = importedAfterUtc;
        ImportedBeforeUtc = importedBeforeUtc;
        ActivityStartedAfterUtc = activityStartedAfterUtc;
        ActivityStartedBeforeUtc = activityStartedBeforeUtc;
        MaximumCount = maximumCount;
    }

    /// <summary>
    /// Gets the inclusive lower bound for import time filtering.
    /// </summary>
    public DateTimeOffset? ImportedAfterUtc { get; }

    /// <summary>
    /// Gets the inclusive upper bound for import time filtering.
    /// </summary>
    public DateTimeOffset? ImportedBeforeUtc { get; }

    /// <summary>
    /// Gets the inclusive lower bound for activity start filtering.
    /// </summary>
    public DateTimeOffset? ActivityStartedAfterUtc { get; }

    /// <summary>
    /// Gets the inclusive upper bound for activity start filtering.
    /// </summary>
    public DateTimeOffset? ActivityStartedBeforeUtc { get; }

    /// <summary>
    /// Gets the maximum number of results to return, or <see langword="null"/> when the query is unbounded.
    /// </summary>
    public int? MaximumCount { get; }
}
