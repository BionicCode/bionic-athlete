namespace FitToCsvConverter.Data.Activities;

using System.Collections.Immutable;
using FitToCsvConverter.Data.Fields;

public sealed class FitLap : FitNode
{
    public FitLap(FitNodeSnapshot original, ImmutableArray<FitField> fields)
        : base(original, fields)
    {
    }
}
