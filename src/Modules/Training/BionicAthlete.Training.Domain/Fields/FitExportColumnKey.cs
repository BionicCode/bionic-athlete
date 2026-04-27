namespace BionicAthlete.Training.Domain.Fields;

using BionicAthlete.Training.Domain.Activities;

public readonly record struct FitExportColumnKey(FitNodeType NodeType, string Token)
{
    public static FitExportColumnKey FromField(FitFieldKey fieldKey) => new(fieldKey.NodeType, $"field:{fieldKey}");

    public static FitExportColumnKey FromModeledProperty(FitNodeType nodeType, string propertyName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(propertyName);
        return new FitExportColumnKey(nodeType, $"property:{propertyName}");
    }

    public override string ToString() => $"{NodeType}:{Token}";
}
