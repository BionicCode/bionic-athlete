using System.Windows;

public readonly record struct UniformToolBarLayoutResult(
    Size UniformSize,
    int VisibleCount,
    int OverflowCount)
{
    public static UniformToolBarLayoutResult Empty { get; } = new(Size.Empty, 0, 0);
    public bool HasOverflowItems => OverflowCount > 0;
}