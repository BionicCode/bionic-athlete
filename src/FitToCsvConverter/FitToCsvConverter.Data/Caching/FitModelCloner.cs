namespace FitToCsvConverter.Data.Caching;

using System.Collections.Immutable;
using FitToCsvConverter.Data.Activities;
using FitToCsvConverter.Data.Decoding;
using FitToCsvConverter.Data.Fields;

internal static class FitModelCloner
{
    public static FitActivityDecodeResult CloneResult(FitActivityDecodeResult result, FitFileSource source, bool isFromCache)
        => new(
            result.Activity is null ? null : CloneActivity(result.Activity, source),
            source,
            result.Issues,
            isFromCache);

    private static FitActivity CloneActivity(FitActivity activity, FitFileSource source)
        => new(
            activity.Original,
            CloneFields(activity.Fields),
            activity.Sessions.Select(CloneSession).ToImmutableArray(),
            source,
            CloneAncillaryData(activity.AncillaryData));

    private static FitSession CloneSession(FitSession session)
        => new(
            session.Original,
            CloneFields(session.Fields),
            session.Laps.Select(CloneLap).ToImmutableArray(),
            session.Records.Select(CloneRecord).ToImmutableArray());

    private static FitLap CloneLap(FitLap lap)
        => new(
            lap.Original,
            CloneFields(lap.Fields));

    private static FitRecord CloneRecord(FitRecord record)
        => new(
            record.Original,
            CloneFields(record.Fields));

    private static FitActivityAncillaryData CloneAncillaryData(FitActivityAncillaryData ancillaryData)
        => ancillaryData.Messages.IsDefaultOrEmpty
            ? FitActivityAncillaryData.Empty
            : new FitActivityAncillaryData(
                ancillaryData.Messages
                    .Select(message => new FitAncillaryMessage(message.Original, message.Fields))
                    .ToImmutableArray());

    private static ImmutableArray<FitField> CloneFields(ImmutableArray<FitField> fields)
        => fields.Select(CloneField).ToImmutableArray();

    private static FitField CloneField(FitField field)
    {
        FitField clone = new(field.Original);
        clone.SetDisplayName(field.State.DisplayName);
        clone.SetColumnName(field.State.ColumnName);
        clone.SetExportInclusion(field.State.IsIncludedInExport);

        if (field.State.HasEditedDecodedValues)
        {
            clone.SetEditedDecodedValues(field.State.EditedDecodedValues);
        }

        return clone;
    }
}
