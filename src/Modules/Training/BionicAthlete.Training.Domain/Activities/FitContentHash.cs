namespace BionicAthlete.Training.Domain.Activities;

public readonly record struct FitContentHash
{
    public FitContentHash(string hexValue)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(hexValue);
        HexValue = hexValue;
    }

    public string HexValue { get; }

    public static FitContentHash FromHashBytes(ReadOnlySpan<byte> hashBytes)
        => new(Convert.ToHexString(hashBytes));

    public override string ToString() => HexValue;
}
