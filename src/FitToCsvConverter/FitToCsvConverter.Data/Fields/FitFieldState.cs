namespace FitToCsvConverter.Data.Fields;

using System.Collections.Immutable;

public sealed class FitFieldState
{
    internal FitFieldState(string originalName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(originalName);

        DisplayName = originalName;
        ColumnName = originalName;
        IsIncludedInExport = true;
        EditedDecodedValues = default;
    }

    public string DisplayName { get; internal set; }

    public string ColumnName { get; internal set; }

    public bool IsIncludedInExport { get; internal set; }

    public ImmutableArray<object?> EditedDecodedValues { get; internal set; }

    public bool HasEditedDecodedValues => !EditedDecodedValues.IsDefault;
}
