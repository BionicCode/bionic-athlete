namespace FitToCsvConverter.Data.Fields;

using System.Collections.Immutable;
using System.Linq;

public sealed class FitField
{
    public FitField(FitFieldSnapshot original)
    {
        ArgumentNullException.ThrowIfNull(original);

        Original = original;
        State = new FitFieldState(original.OriginalName);
    }

    public FitFieldSnapshot Original { get; }

    public FitFieldState State { get; }

    public ImmutableArray<object?> GetEffectiveDecodedValues()
        => State.HasEditedDecodedValues
            ? State.EditedDecodedValues
            : Original.OriginalValues.Select(value => value.DecodedValue).ToImmutableArray();

    public void SetDisplayName(string? displayName)
        => State.DisplayName = string.IsNullOrWhiteSpace(displayName) ? Original.OriginalName : displayName.Trim();

    public void SetColumnName(string? columnName)
        => State.ColumnName = string.IsNullOrWhiteSpace(columnName) ? Original.OriginalName : columnName.Trim();

    public void SetExportInclusion(bool isIncludedInExport) => State.IsIncludedInExport = isIncludedInExport;

    public void SetEditedDecodedValues(IEnumerable<object?> editedDecodedValues)
    {
        ArgumentNullException.ThrowIfNull(editedDecodedValues);

        ImmutableArray<object?> editedValues = editedDecodedValues.ToImmutableArray();
        if (editedValues.Length != Original.OriginalValues.Length)
        {
            throw new InvalidOperationException(
                $"Edited value count {editedValues.Length} does not match original value count {Original.OriginalValues.Length} for field '{Original.OriginalName}'.");
        }

        State.EditedDecodedValues = editedValues;
    }

    public void ResetEditedDecodedValues() => State.EditedDecodedValues = default;
}
