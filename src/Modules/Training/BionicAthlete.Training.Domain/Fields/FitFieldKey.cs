namespace BionicAthlete.Training.Domain.Fields;

using BionicAthlete.Training.Domain.Activities;

public readonly record struct FitFieldKey(
    FitNodeType NodeType,
    FitFieldKind Kind,
    ushort MessageNumber,
    byte FieldNumber,
    byte? DeveloperDataIndex = null)
{
    public override string ToString()
        => DeveloperDataIndex is byte developerDataIndex
            ? $"{NodeType}:{Kind}:{MessageNumber}:{FieldNumber}:{developerDataIndex}"
            : $"{NodeType}:{Kind}:{MessageNumber}:{FieldNumber}";
}
