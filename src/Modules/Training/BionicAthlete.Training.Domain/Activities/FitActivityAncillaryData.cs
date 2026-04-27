namespace BionicAthlete.Training.Domain.Activities;

using System.Collections.Immutable;

public sealed class FitActivityAncillaryData
{
    public static FitActivityAncillaryData Empty { get; } = new(ImmutableArray<FitAncillaryMessage>.Empty);

    public FitActivityAncillaryData(ImmutableArray<FitAncillaryMessage> messages)
    {
        Messages = messages.IsDefault ? ImmutableArray<FitAncillaryMessage>.Empty : messages;
    }

    /// <summary>
    /// Preserved non-tree FIT messages that were decoded alongside the activity tree.
    /// </summary>
    public ImmutableArray<FitAncillaryMessage> Messages { get; }
}
