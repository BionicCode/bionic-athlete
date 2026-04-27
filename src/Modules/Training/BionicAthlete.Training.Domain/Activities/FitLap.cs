namespace BionicAthlete.Training.Domain.Activities;

using System.Collections.Immutable;
using BionicAthlete.Training.Domain.Fields;

public sealed class FitLap : FitNode
{
    public FitLap(FitNodeSnapshot original, ImmutableArray<FitField> fields)
        : base(original, fields)
    {
    }
}
