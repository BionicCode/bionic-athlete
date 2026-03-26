namespace FitToCsvConverter.Data.Fields;

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
    }

    public FitFieldKey Key { get; }

    public FitExportColumnKey ExportColumnKey { get; }

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

    public ImmutableArray<FitFieldValue> OriginalValues { get; }

    public bool IsArray => OriginalValues.Length > 1;
}
