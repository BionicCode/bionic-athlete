namespace BionicAthlete.Training.Test.Persistence;

using System.Collections.Immutable;
using BionicAthlete.Training.Domain.Persistence;
using BionicAthlete.Training.Test.Fixtures;

public sealed class ActivityHistoryStoreContractTests
{
    [Fact]
    public async Task ShouldReturnExistingIdentifierWhenSameFingerprintIsSavedTwice()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        InMemoryActivityHistoryStoreFake store = new();
        ImportedActivityEnvelope importedActivity = new(
            FitActivityModelFactory.CreateActivityForExport(),
            new ActivityFingerprint("sha256:activity-1"),
            new DateTimeOffset(2024, 07, 14, 12, 00, 00, TimeSpan.Zero));

        ActivityPersistenceResult firstSaveResult = await store.SaveImportedActivityAsync(importedActivity, cancellationToken);
        ActivityPersistenceResult secondSaveResult = await store.SaveImportedActivityAsync(importedActivity, cancellationToken);

        Assert.Equal(firstSaveResult.ActivityId, secondSaveResult.ActivityId);
        Assert.False(secondSaveResult.WasCreated);
    }

    [Fact]
    public async Task ShouldProjectStoredSummaryDataWhenQueryingSavedActivities()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        InMemoryActivityHistoryStoreFake store = new();
        ImportedActivityEnvelope importedActivity = new(
            FitActivityModelFactory.CreateActivityForExport(),
            new ActivityFingerprint("sha256:activity-2"),
            new DateTimeOffset(2024, 07, 14, 13, 00, 00, TimeSpan.Zero));

        _ = await store.SaveImportedActivityAsync(importedActivity, cancellationToken);
        ImmutableArray<StoredActivitySummary> summaries = await store.QueryActivitiesAsync(new ActivityHistoryQuery(), cancellationToken);

        StoredActivitySummary summary = Assert.Single(summaries);
        Assert.Equal(importedActivity.Activity.Source.DisplayName, summary.SourceDisplayName);
    }

    private sealed class InMemoryActivityHistoryStoreFake : IActivityHistoryStore
    {
        private readonly Dictionary<Guid, StoredActivityRecord> _activityById = [];
        private readonly Dictionary<ActivityFingerprint, Guid> _activityIdByFingerprint = [];

        public Task<ActivityPersistenceResult> SaveImportedActivityAsync(ImportedActivityEnvelope importedActivity, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(importedActivity);

            cancellationToken.ThrowIfCancellationRequested();

            if (_activityIdByFingerprint.TryGetValue(importedActivity.Fingerprint, out Guid existingActivityId))
            {
                StoredActivityRecord existingRecord = _activityById[existingActivityId];
                return Task.FromResult(new ActivityPersistenceResult(existingRecord.ActivityId, existingRecord.Fingerprint, wasCreated: false, existingRecord.IsPendingSync));
            }

            Guid activityId = Guid.NewGuid();
            StoredActivityRecord storedRecord = new(
                activityId,
                importedActivity.Fingerprint,
                importedActivity.Activity,
                importedActivity.ImportedAtUtc,
                isPendingSync: true);
            _activityById.Add(activityId, storedRecord);
            _activityIdByFingerprint.Add(importedActivity.Fingerprint, activityId);

            return Task.FromResult(new ActivityPersistenceResult(activityId, importedActivity.Fingerprint, wasCreated: true, isPendingSync: true));
        }

        public Task<StoredActivityRecord?> TryGetActivityAsync(Guid activityId, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(_activityById.TryGetValue(activityId, out StoredActivityRecord? activity) ? activity : null);
        }

        public Task<StoredActivityRecord?> TryGetByFingerprintAsync(ActivityFingerprint fingerprint, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!_activityIdByFingerprint.TryGetValue(fingerprint, out Guid activityId))
            {
                return Task.FromResult<StoredActivityRecord?>(null);
            }

            return Task.FromResult<StoredActivityRecord?>(_activityById[activityId]);
        }

        public Task<ImmutableArray<StoredActivitySummary>> QueryActivitiesAsync(ActivityHistoryQuery query, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(query);

            cancellationToken.ThrowIfCancellationRequested();

            IEnumerable<StoredActivityRecord> records = _activityById.Values;
            if (query.ImportedAfterUtc is DateTimeOffset importedAfterUtc)
            {
                records = records.Where(record => record.ImportedAtUtc >= importedAfterUtc);
            }

            if (query.ImportedBeforeUtc is DateTimeOffset importedBeforeUtc)
            {
                records = records.Where(record => record.ImportedAtUtc <= importedBeforeUtc);
            }

            if (query.ActivityStartedAfterUtc is DateTimeOffset activityStartedAfterUtc)
            {
                records = records.Where(record => record.Activity.CanonicalStartTimeUtc >= activityStartedAfterUtc);
            }

            if (query.ActivityStartedBeforeUtc is DateTimeOffset activityStartedBeforeUtc)
            {
                records = records.Where(record => record.Activity.CanonicalStartTimeUtc <= activityStartedBeforeUtc);
            }

            if (query.MaximumCount is int maximumCount)
            {
                records = records.Take(maximumCount);
            }

            ImmutableArray<StoredActivitySummary> summaries = records
                .Select(record => new StoredActivitySummary(
                    record.ActivityId,
                    record.Fingerprint,
                    record.Activity.Source.DisplayName,
                    record.ImportedAtUtc,
                    record.Activity.CanonicalStartTimeUtc,
                    record.Activity.Sessions.Length,
                    record.IsPendingSync))
                .ToImmutableArray();

            return Task.FromResult(summaries);
        }
    }
}
