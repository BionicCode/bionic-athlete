namespace BionicAthlete.Training.Domain.Fields;

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

    /// <summary>
    /// User-facing label for presentation. Defaults to <see cref="FitFieldSnapshot.OriginalName"/>.
    /// </summary>
    public string DisplayName { get; internal set; }

    /// <summary>
    /// Export column name. Defaults to <see cref="FitFieldSnapshot.OriginalName"/>.
    /// </summary>
    public string ColumnName { get; internal set; }

    public bool IsIncludedInExport { get; internal set; }

    /// <summary>
    /// Edited decoded values that override the original decoded values for presentation/export.
    /// </summary>
    public ImmutableArray<object?> EditedDecodedValues { get; internal set; }

    public bool HasEditedDecodedValues => !EditedDecodedValues.IsDefault;
}
