public readonly record struct UniformToolBarLayoutResult(
    Size UniformSize,
    int VisibleCount,
    int OverflowCount)
{
    public bool HasOverflowItems => OverflowCount > 0;
}