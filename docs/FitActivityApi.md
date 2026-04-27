# Fit Activity API

This document describes the decoded model exposed by `BionicAthlete.Training.Domain` and the parts of the API that are typically useful for presentation.

## Decode entry points

Use `IFitActivityDecoder` to decode a file or stream.

```csharp
IFitActivityDecoder decoder = new GarminFitActivityDecoder();
FitActivityDecodeResult result = await decoder.DecodeFileAsync(path, cancellationToken);

if (!result.IsSuccess || result.Activity is null)
{
    // Inspect result.Issues for errors and warnings.
    return;
}

FitActivity activity = result.Activity;
```

## Activity tree

The decoded hierarchy is:

- `FitActivity`
- `FitSession`
- `FitLap`
- `FitRecord`

Access pattern:

```csharp
FitActivity activity = result.Activity!;
ImmutableArray<FitSession> sessions = activity.Sessions;
ImmutableArray<FitLap> laps = sessions[0].Laps;
ImmutableArray<FitRecord> records = sessions[0].Records;
```

## Date and time

For presentation, prefer the canonical properties:

- Activity date/time: `activity.CanonicalStartTimeUtc`
- Session date/time: `session.CanonicalStartTimeUtc`
- Any node snapshot: `node.Original.CanonicalTimestampUtc`

Important distinction:

- `Original.TimestampUtc` is the original FIT `timestamp`.
- `Original.StartTimeUtc` is the original FIT `start_time` when available.
- `Original.CanonicalTimestampUtc` prefers `start_time` over `timestamp`.

Example:

```csharp
DateTimeOffset? activityDate = activity.CanonicalStartTimeUtc;
DateTimeOffset? sessionDate = activity.Sessions[0].CanonicalStartTimeUtc;
DateTimeOffset? recordTimestamp = activity.Sessions[0].Records[0].Original.CanonicalTimestampUtc;
```

## Available fields on activity, session, lap, and record

Every node inherits `FitNode.Fields`.

- Activity-level fields: `activity.Fields`
- Session-level fields: `session.Fields`
- Lap-level fields: `lap.Fields`
- Record-level fields: `record.Fields`

Example:

```csharp
foreach (FitField field in record.Fields)
{
    string sourceName = field.Original.OriginalName;
    string displayName = field.State.DisplayName;
    ImmutableArray<object?> values = field.GetEffectiveDecodedValues();
}
```

Record-level data such as GPS position remains on record fields. For example, latitude and longitude are fields on `FitRecord.Fields`, not a separate child object.

## Which field name to show

You usually have two names available:

- `field.Original.OriginalName`: immutable source FIT field name
- `field.State.DisplayName`: current presentation name, editable later

Use:

- `OriginalName` when you need the exact source identity
- `DisplayName` when you are rendering UI

## Which values to show

There are three relevant layers:

- `field.Original.OriginalValues`: immutable raw/decoded source values
- `field.State.EditedDecodedValues`: optional edited presentation values
- `field.GetEffectiveDecodedValues()`: the values the UI or exporter should currently use

Example:

```csharp
FitField field = record.Fields.First(f => f.Original.OriginalName == "heart_rate");

object? firstDisplayedValue = field.GetEffectiveDecodedValues().FirstOrDefault();
object? firstOriginalDecodedValue = field.Original.OriginalValues.FirstOrDefault().DecodedValue;
object? firstOriginalRawValue = field.Original.OriginalValues.FirstOrDefault().RawValue;
```

If `field.Original.IsArray` is `false`, the field is typically scalar and `GetEffectiveDecodedValues()` will usually contain one element.

## Filtering developer and unknown fields

Use `field.Original.Kind`:

- `FitFieldKind.Standard`
- `FitFieldKind.Developer`
- `FitFieldKind.Unknown`

Example:

```csharp
IEnumerable<FitField> developerFields = session.Fields
    .Where(field => field.Original.Kind == FitFieldKind.Developer);
```

## Ancillary messages

`FitActivity.AncillaryData.Messages` contains preserved non-tree FIT messages such as:

- `Event`
- `DeveloperDataId`
- `FieldDescription`

These are useful for diagnostics and advanced workflows, but the main presentation tree is `Activity -> Session -> Lap / Record`.

## Practical presentation patterns

### Show activity summary row

```csharp
FitActivity activity = result.Activity!;

string title = activity.Source.DisplayName;
DateTimeOffset? activityDate = activity.CanonicalStartTimeUtc;
IEnumerable<FitField> activityFields = activity.Fields;
```

### Show record columns dynamically

```csharp
FitRecord firstRecord = activity.Sessions[0].Records[0];

foreach (FitField field in firstRecord.Fields)
{
    string columnName = field.State.DisplayName;
    ImmutableArray<object?> values = field.GetEffectiveDecodedValues();
}
```

### Read a specific field by source name

```csharp
FitField? distanceField = record.Fields
    .FirstOrDefault(field => field.Original.OriginalName == "distance");

object? distanceValue = distanceField?.GetEffectiveDecodedValues().FirstOrDefault();
```
