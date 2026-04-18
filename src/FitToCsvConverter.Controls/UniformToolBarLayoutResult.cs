using System.Windows;

public readonly record struct UniformToolBarLayoutResult(
    Size UniformSize,
    int VisibleCount,
    int OverflowCount)
{
    public static UniformToolBarLayoutResult Empty { get; } = new(Size.Empty, 0, 0);
    public bool HasOverflowItems => OverflowCount > 0;
    public bool IsValid { get; private init; } = true;
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1024:Use properties where appropriate", Justification = "Must not participate in equality, hash code , ToString() etc. and also communicate tah a new instance (value type) is returned")]
    public UniformToolBarLayoutResult GetInvalidatedResult() => this with { IsValid = false };
}