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

    /// <summary>
    /// Immutable source metadata and original values from the decoded FIT file.
    /// </summary>
    public FitFieldSnapshot Original { get; }

    /// <summary>
    /// Mutable presentation/export state layered over the immutable source data.
    /// </summary>
    public FitFieldState State { get; }

    /// <summary>
    /// Gets the values that should currently be shown to the user or sent to export:
    /// edited values when present, otherwise the original decoded values from the FIT file.
    /// </summary>
    public ImmutableArray<object?> GetEffectiveDecodedValues()
        => State.HasEditedDecodedValues
            ? State.EditedDecodedValues
            : Original.OriginalValues.Select(value => value.DecodedValue).ToImmutableArray();

    public void SetDisplayName(string? displayName)
        => State.DisplayName = string.IsNullOrWhiteSpace(displayName) ? Original.OriginalName : displayName.Trim();

    public void SetColumnName(string? columnName)
        => State.ColumnName = string.IsNullOrWhiteSpace(columnName) ? Original.OriginalName : columnName.Trim();

    public void SetExportInclusion(bool isIncludedInExport) => State.IsIncludedInExport = isIncludedInExport;

    /// <summary>
    /// Replaces the current effective decoded values while preserving the immutable original values.
    /// The edited value count must match the original field shape.
    /// </summary>
    public void SetEditedDecodedValues(IEnumerable<object?> editedDecodedValues)
    {
        ArgumentNullException.ThrowIfNull(editedDecodedValues);

        var editedValues = editedDecodedValues.ToImmutableArray();
        if (editedValues.Length != Original.OriginalValues.Length)
        {
            throw new InvalidOperationException(
                $"Edited value count {editedValues.Length} does not match original value count {Original.OriginalValues.Length} for field '{Original.OriginalName}'.");
        }

        State.EditedDecodedValues = editedValues;
    }

    /// <summary>
    /// Clears the edited values so presentation falls back to the original decoded values.
    /// </summary>
    public void ResetEditedDecodedValues() => State.EditedDecodedValues = default;
}
