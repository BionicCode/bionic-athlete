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

    public FitNodeSnapshot Original { get; }

    public ImmutableArray<FitFieldSnapshot> Fields { get; }
}
