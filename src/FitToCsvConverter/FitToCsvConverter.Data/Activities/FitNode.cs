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

    /// <summary>
    /// Immutable FIT message metadata for this node.
    /// </summary>
    public FitNodeSnapshot Original { get; }

    /// <summary>
    /// Exportable fields that belong directly to this node.
    /// Use <see cref="FitField.Original"/> for immutable source data and <see cref="FitField.State"/> for mutable presentation/export state.
    /// </summary>
    public ImmutableArray<FitField> Fields { get; }
}
