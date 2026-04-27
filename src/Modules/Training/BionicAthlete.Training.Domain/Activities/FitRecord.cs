namespace BionicAthlete.Training.Domain.Activities;

using System.Collections.Immutable;
using BionicAthlete.Training.Domain.Fields;

public sealed class FitRecord : FitNode
{
    public FitRecord(FitNodeSnapshot original, ImmutableArray<FitField> fields)
        : base(original, fields)
    {
    }
}
