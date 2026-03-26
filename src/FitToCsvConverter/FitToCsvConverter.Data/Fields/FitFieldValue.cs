namespace FitToCsvConverter.Data.Fields;

/// <summary>
/// Immutable raw/decoded representation of a single field element.
/// RawValue preserves the source SDK value shape, while DecodedValue is the presentation-oriented normalized value.
/// </summary>
public sealed record FitFieldValue(object? RawValue, object? DecodedValue);
