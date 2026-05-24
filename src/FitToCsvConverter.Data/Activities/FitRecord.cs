namespace FitToCsvConverter.Data.Activities;

using System.Collections.Immutable;
using FitToCsvConverter.Data.Fields;

public sealed class FitRecord : FitNode
{
    public FitRecord(FitNodeSnapshot original, ImmutableArray<FitField> fields)
        : base(original, fields)
    {
    }
}
