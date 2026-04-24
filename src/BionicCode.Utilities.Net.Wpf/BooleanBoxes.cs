namespace BionicCode.Utilities.Net;

/// <summary>
/// Dependency properties of type bool are boxed when stored in the property system. 
/// This class provides boxed values for <see langword="true"/> and <see langword="false"/> to avoid unnecessary allocations.
/// </summary>
public static class BooleanBoxes
{
    public static readonly object TrueBox = true;
    public static readonly object FalseBox = false;

    public static object Box(bool value) => value ? TrueBox : FalseBox;

    public static object? Box(bool? value) => value switch
    {
        true => TrueBox,
        false => FalseBox,
        null => null,
    };
}
