namespace FitToCsvConverter.Data.Activities;

using System.Collections.Immutable;

public sealed class FitActivityAncillaryData
{
    public static FitActivityAncillaryData Empty { get; } = new(ImmutableArray<FitAncillaryMessage>.Empty);

    public FitActivityAncillaryData(ImmutableArray<FitAncillaryMessage> messages)
    {
        Messages = messages.IsDefault ? ImmutableArray<FitAncillaryMessage>.Empty : messages;
    }

    public ImmutableArray<FitAncillaryMessage> Messages { get; }
}
