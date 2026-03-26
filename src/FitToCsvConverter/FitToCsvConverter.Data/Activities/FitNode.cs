namespace FitToCsvConverter.Data.Activities;

using System.Collections.Immutable;
using FitToCsvConverter.Data.Fields;

public abstract class FitNode
{
    protected FitNode(FitNodeSnapshot original, ImmutableArray<FitField> fields)
    {
        ArgumentNullException.ThrowIfNull(original);

        Original = original;
        Fields = fields.IsDefault ? ImmutableArray<FitField>.Empty : fields;
    }

    public FitNodeSnapshot Original { get; }

    public ImmutableArray<FitField> Fields { get; }
}
