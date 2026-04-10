namespace FitToCsvConverter.Controls;

/// <summary>
/// Dependency properties of type bool are boxed when stored in the property system. 
/// This class provides boxed values for <see langword="true"/> and <see langword="false"/> to avoid unnecessary allocations.
/// </summary>
internal static class BooleanBoxes
{
    internal static readonly object TrueBox = true;
    internal static readonly object FalseBox = false;

    internal static object Box(bool value) => value ? TrueBox : FalseBox;

    internal static object? Box(bool? value) => value switch
    {
        true => TrueBox,
        false => FalseBox,
        null => null,
    };
}
