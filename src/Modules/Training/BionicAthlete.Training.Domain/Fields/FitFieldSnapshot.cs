namespace BionicAthlete.Training.Domain.Fields;

using System.Collections.Immutable;

public sealed class FitFieldSnapshot
{
    public FitFieldSnapshot(
        FitFieldKey key,
        FitExportColumnKey exportColumnKey,
        string originalName,
        string messageName,
        FitFieldKind kind,
        byte baseType,
        string baseTypeName,
        string profileTypeName,
        string? units,
        double scale,
        double offset,
        bool isAccumulated,
        bool isExpandedField,
        ImmutableArray<byte> developerApplicationIdBytes,
        uint? developerApplicationVersion,
        byte? nativeOverrideFieldNumber,
        ushort? nativeOverrideMessageNumber,
        bool isArray,
        ImmutableArray<FitFieldValue> originalValues)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(originalName);
        ArgumentException.ThrowIfNullOrWhiteSpace(messageName);
        ArgumentException.ThrowIfNullOrWhiteSpace(baseTypeName);
        ArgumentException.ThrowIfNullOrWhiteSpace(profileTypeName);

        Key = key;
        ExportColumnKey = exportColumnKey;
        OriginalName = originalName;
        MessageName = messageName;
        Kind = kind;
        BaseType = baseType;
        BaseTypeName = baseTypeName;
        ProfileTypeName = profileTypeName;
        Units = units;
        Scale = scale;
        Offset = offset;
        IsAccumulated = isAccumulated;
        IsExpandedField = isExpandedField;
        DeveloperApplicationIdBytes = developerApplicationIdBytes.IsDefault ? ImmutableArray<byte>.Empty : developerApplicationIdBytes;
        DeveloperApplicationVersion = developerApplicationVersion;
        NativeOverrideFieldNumber = nativeOverrideFieldNumber;
        NativeOverrideMessageNumber = nativeOverrideMessageNumber;
        OriginalValues = originalValues.IsDefault ? ImmutableArray<FitFieldValue>.Empty : originalValues;
        IsArray = isArray || OriginalValues.Length > 1;
    }

    /// <summary>
    /// Stable identity for this field within the decoded model.
    /// </summary>
    public FitFieldKey Key { get; }

    /// <summary>
    /// Stable export key that remains usable even when the presentation column name changes.
    /// </summary>
    public FitExportColumnKey ExportColumnKey { get; }

    /// <summary>
    /// Source FIT field name as decoded from the file or Garmin metadata.
    /// </summary>
    public string OriginalName { get; }

    public string MessageName { get; }

    public FitFieldKind Kind { get; }

    public byte BaseType { get; }

    public string BaseTypeName { get; }

    public string ProfileTypeName { get; }

    public string? Units { get; }

    public double Scale { get; }

    public double Offset { get; }

    public bool IsAccumulated { get; }

    public bool IsExpandedField { get; }

    public ImmutableArray<byte> DeveloperApplicationIdBytes { get; }

    public uint? DeveloperApplicationVersion { get; }

    public byte? NativeOverrideFieldNumber { get; }

    public ushort? NativeOverrideMessageNumber { get; }

    /// <summary>
    /// True when the FIT field is logically an array/multi-value field, even if the SDK surfaced it as a single CLR array object.
    /// </summary>
    public bool IsArray { get; }

    /// <summary>
    /// Immutable raw/decoded value pairs in source order.
    /// Use <see cref="FitField.GetEffectiveDecodedValues"/> for presentation and <see cref="OriginalValues"/> when you need immutable source values.
    /// </summary>
    public ImmutableArray<FitFieldValue> OriginalValues { get; }
}
