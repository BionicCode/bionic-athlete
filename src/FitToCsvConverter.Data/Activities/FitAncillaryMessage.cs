namespace FitToCsvConverter.Data.Activities;

using System.Collections.Immutable;
using FitToCsvConverter.Data.Fields;

public sealed class FitAncillaryMessage
{
    public FitAncillaryMessage(FitNodeSnapshot original, ImmutableArray<FitFieldSnapshot> fields)
    {
        ArgumentNullException.ThrowIfNull(original);

        Original = original;
        Fields = fields.IsDefault ? ImmutableArray<FitFieldSnapshot>.Empty : fields;
    }

    /// <summary>
    /// Immutable FIT message metadata for the ancillary message.
    /// </summary>
    public FitNodeSnapshot Original { get; }

    /// <summary>
    /// Immutable field snapshots for the ancillary message.
    /// </summary>
    public ImmutableArray<FitFieldSnapshot> Fields { get; }
}
