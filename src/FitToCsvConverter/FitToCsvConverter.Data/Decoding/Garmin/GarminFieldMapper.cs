namespace FitToCsvConverter.Data.Decoding.Garmin;

using System.Collections.Immutable;
using System.Reflection;
using System.Text;
using Dynastream.Fit;
using FitToCsvConverter.Data.Activities;
using FitToCsvConverter.Data.Fields;

internal sealed class GarminFieldMapper
{
    private static readonly Assembly FitAssembly = typeof(Mesg).Assembly;
    private readonly GarminDeveloperFieldCatalog developerFieldCatalog;

    public GarminFieldMapper(GarminDeveloperFieldCatalog developerFieldCatalog)
    {
        ArgumentNullException.ThrowIfNull(developerFieldCatalog);
        this.developerFieldCatalog = developerFieldCatalog;
    }

    public ImmutableArray<FitField> MapFields(Mesg mesg, FitNodeType nodeType)
        => MapFieldSnapshots(mesg, nodeType).Select(snapshot => new FitField(snapshot)).ToImmutableArray();

    public ImmutableArray<FitFieldSnapshot> MapFieldSnapshots(Mesg mesg, FitNodeType nodeType)
    {
        ArgumentNullException.ThrowIfNull(mesg);

        ImmutableArray<FitFieldSnapshot>.Builder builder =
            ImmutableArray.CreateBuilder<FitFieldSnapshot>(mesg.GetNumFields() + mesg.DeveloperFields.Count());

        foreach (Field field in mesg.Fields)
        {
            builder.Add(CreateStandardFieldSnapshot(mesg, nodeType, field));
        }

        foreach (DeveloperField developerField in mesg.DeveloperFields)
        {
            builder.Add(CreateDeveloperFieldSnapshot(mesg, nodeType, developerField));
        }

        return builder.MoveToImmutable();
    }

    private FitFieldSnapshot CreateStandardFieldSnapshot(Mesg mesg, FitNodeType nodeType, Field field)
    {
        string messageName = GetMessageName(mesg);
        FitFieldKind kind = field.ProfileType == Profile.Type.NumTypes || string.Equals(field.Name, "unknown", StringComparison.OrdinalIgnoreCase)
            ? FitFieldKind.Unknown
            : FitFieldKind.Standard;

        FitFieldKey fieldKey = new(nodeType, kind, mesg.Num, field.Num);
        return new FitFieldSnapshot(
            fieldKey,
            FitExportColumnKey.FromField(fieldKey),
            GetStandardFieldName(field),
            messageName,
            kind,
            field.Type,
            GetBaseTypeName(field.Type),
            field.ProfileType.ToString(),
            field.Units,
            field.Scale,
            field.Offset,
            field.IsAccumulated,
            field.IsExpandedField,
            ImmutableArray<byte>.Empty,
            null,
            null,
            null,
            isArray: field.GetNumValues() > 1 || HasSingleArrayEncodedValue(field, field.Type),
            CreateFieldValues(field, field.Type, field.ProfileType.ToString(), preserveSingleArrayValueAsCollection: true));
    }

    private FitFieldSnapshot CreateDeveloperFieldSnapshot(Mesg mesg, FitNodeType nodeType, DeveloperField developerField)
    {
        GarminDeveloperFieldDescriptionMetadata? fieldDescription =
            developerFieldCatalog.GetFieldDescription(developerField.DeveloperDataIndex, developerField.Num);

        GarminDeveloperDataIdentity? developerDataIdentity =
            fieldDescription?.DeveloperDataIdentity
            ?? developerFieldCatalog.GetDeveloperDataIdentity(developerField.DeveloperDataIndex);

        FitFieldKey fieldKey = new(nodeType, FitFieldKind.Developer, mesg.Num, developerField.Num, developerField.DeveloperDataIndex);

        byte? nativeOverrideFieldNumber = developerField.NativeOverride == Fit.FieldNumInvalid
            ? fieldDescription?.NativeFieldNumber
            : developerField.NativeOverride;

        ImmutableArray<byte> applicationIdBytes = developerDataIdentity?.ApplicationIdBytes
            ?? (developerField.AppId is { Length: > 0 } appId ? ImmutableArray.Create(appId) : ImmutableArray<byte>.Empty);

        uint? applicationVersion = developerDataIdentity?.ApplicationVersion
            ?? (developerField.AppVersion == 0 ? null : developerField.AppVersion);

        return new FitFieldSnapshot(
            fieldKey,
            FitExportColumnKey.FromField(fieldKey),
            GetDeveloperFieldName(developerField, fieldDescription),
            GetMessageName(mesg),
            FitFieldKind.Developer,
            developerField.Type,
            GetBaseTypeName(developerField.Type),
            "DeveloperField",
            developerField.Units ?? fieldDescription?.Units,
            developerField.Scale,
            developerField.Offset,
            isAccumulated: false,
            isExpandedField: false,
            applicationIdBytes,
            applicationVersion,
            nativeOverrideFieldNumber,
            fieldDescription?.NativeMessageNumber,
            isArray: fieldDescription?.IsArray == true
                || developerField.GetNumValues() > 1
                || HasSingleArrayEncodedValue(developerField, developerField.Type),
            CreateFieldValues(developerField, developerField.Type, "DeveloperField", preserveSingleArrayValueAsCollection: true));
    }

    private static ImmutableArray<FitFieldValue> CreateFieldValues(
        FieldBase fieldBase,
        byte baseType,
        string profileTypeName,
        bool preserveSingleArrayValueAsCollection)
    {
        int valueCount = fieldBase.GetNumValues();
        if (preserveSingleArrayValueAsCollection
            && valueCount == 1
            && TryExpandArrayValue(fieldBase.GetRawValue(0), baseType, out ImmutableArray<object?> rawValues))
        {
            ImmutableArray<object?> decodedValues = TryExpandArrayValue(fieldBase.GetValue(), baseType, out ImmutableArray<object?> expandedDecodedValues)
                && expandedDecodedValues.Length == rawValues.Length
                    ? expandedDecodedValues
                    : rawValues;

            ImmutableArray<FitFieldValue>.Builder expandedBuilder = ImmutableArray.CreateBuilder<FitFieldValue>(rawValues.Length);
            for (int index = 0; index < rawValues.Length; index++)
            {
                object? rawValue = NormalizeRawValue(rawValues[index]);
                object? decodedValue = NormalizeDecodedValue(baseType, profileTypeName, decodedValues[index], rawValue);
                expandedBuilder.Add(new FitFieldValue(rawValue, decodedValue));
            }

            return expandedBuilder.MoveToImmutable();
        }

        ImmutableArray<FitFieldValue>.Builder builder = ImmutableArray.CreateBuilder<FitFieldValue>(valueCount);
        for (int index = 0; index < valueCount; index++)
        {
            object? rawValue = NormalizeRawValue(fieldBase.GetRawValue(index));
            object? decodedValue = NormalizeDecodedValue(baseType, profileTypeName, fieldBase.GetValue(index), rawValue);
            builder.Add(new FitFieldValue(rawValue, decodedValue));
        }

        return builder.MoveToImmutable();
    }

    private static bool HasSingleArrayEncodedValue(FieldBase fieldBase, byte baseType)
        => fieldBase.GetNumValues() == 1
            && (TryExpandArrayValue(fieldBase.GetRawValue(0), baseType, out _)
                || TryExpandArrayValue(fieldBase.GetValue(), baseType, out _));

    private static bool TryExpandArrayValue(object? value, byte baseType, out ImmutableArray<object?> values)
    {
        values = default;
        if ((baseType & Fit.BaseTypeNumMask) == Fit.String || value is not Array arrayValue || arrayValue.Rank != 1)
        {
            return false;
        }

        ImmutableArray<object?>.Builder builder = ImmutableArray.CreateBuilder<object?>(arrayValue.Length);
        foreach (object? item in arrayValue)
        {
            builder.Add(item);
        }

        values = builder.MoveToImmutable();
        return true;
    }

    private static object? NormalizeRawValue(object? rawValue)
        => rawValue switch
        {
            null => null,
            byte[] bytes => ImmutableArray.Create(bytes),
            _ => rawValue
        };

    private static object? NormalizeDecodedValue(byte baseType, string profileTypeName, object? decodedValue, object? normalizedRawValue)
    {
        if (decodedValue is null)
        {
            return null;
        }

        if ((baseType & Fit.BaseTypeNumMask) == Fit.String)
        {
            byte[] bytes = normalizedRawValue switch
            {
                ImmutableArray<byte> immutableBytes => immutableBytes.ToArray(),
                byte[] rawBytes => rawBytes,
                _ => decodedValue as byte[] ?? []
            };

            return DecodeUtf8String(bytes);
        }

        if (TryConvertFitDateTime(profileTypeName, normalizedRawValue, out DateTimeOffset? dateTimeOffset))
        {
            return dateTimeOffset;
        }

        if (TryConvertProfileEnum(profileTypeName, normalizedRawValue, out string? enumName))
        {
            return enumName;
        }

        return decodedValue switch
        {
            byte[] bytes => DecodeUtf8String(bytes),
            Dynastream.Fit.DateTime fitDateTime => new DateTimeOffset(fitDateTime.GetDateTime()),
            _ => decodedValue
        };
    }

    private static bool TryConvertFitDateTime(string profileTypeName, object? normalizedRawValue, out DateTimeOffset? dateTimeOffset)
    {
        dateTimeOffset = null;
        if (!string.Equals(profileTypeName, nameof(Dynastream.Fit.DateTime), StringComparison.Ordinal)
            && !string.Equals(profileTypeName, "LocalDateTime", StringComparison.Ordinal))
        {
            return false;
        }

        if (!TryConvertToUInt32(normalizedRawValue, out uint timestamp) || timestamp < 0x10000000)
        {
            return false;
        }

        dateTimeOffset = new DateTimeOffset(new Dynastream.Fit.DateTime(timestamp).GetDateTime());
        return true;
    }

    private static bool TryConvertProfileEnum(string profileTypeName, object? normalizedRawValue, out string? enumName)
    {
        enumName = null;
        if (string.IsNullOrWhiteSpace(profileTypeName)
            || string.Equals(profileTypeName, Profile.Type.NumTypes.ToString(), StringComparison.Ordinal)
            || string.Equals(profileTypeName, nameof(Dynastream.Fit.DateTime), StringComparison.Ordinal)
            || string.Equals(profileTypeName, "LocalDateTime", StringComparison.Ordinal))
        {
            return false;
        }

        Type? enumType = FitAssembly.GetType($"Dynastream.Fit.{profileTypeName}", throwOnError: false, ignoreCase: false);
        if (enumType is null || !enumType.IsEnum)
        {
            return false;
        }

        if (!TryConvertToInt64(normalizedRawValue, out long numericValue))
        {
            return false;
        }

        object underlyingValue;
        try
        {
            underlyingValue = Convert.ChangeType(numericValue, Enum.GetUnderlyingType(enumType));
        }
        catch
        {
            return false;
        }

        if (!Enum.IsDefined(enumType, underlyingValue))
        {
            return false;
        }

        enumName = Enum.GetName(enumType, underlyingValue);
        return enumName is not null;
    }

    private static bool TryConvertToUInt32(object? value, out uint result)
    {
        try
        {
            result = value switch
            {
                byte byteValue => byteValue,
                sbyte sbyteValue when sbyteValue >= 0 => (uint)sbyteValue,
                short shortValue when shortValue >= 0 => (uint)shortValue,
                ushort ushortValue => ushortValue,
                int intValue when intValue >= 0 => (uint)intValue,
                uint uintValue => uintValue,
                long longValue when longValue >= 0 && longValue <= uint.MaxValue => (uint)longValue,
                ulong ulongValue when ulongValue <= uint.MaxValue => (uint)ulongValue,
                _ => Convert.ToUInt32(value)
            };

            return true;
        }
        catch
        {
            result = default;
            return false;
        }
    }

    private static bool TryConvertToInt64(object? value, out long result)
    {
        try
        {
            result = value switch
            {
                byte byteValue => byteValue,
                sbyte sbyteValue => sbyteValue,
                short shortValue => shortValue,
                ushort ushortValue => ushortValue,
                int intValue => intValue,
                uint uintValue => uintValue,
                long longValue => longValue,
                ulong ulongValue when ulongValue <= long.MaxValue => (long)ulongValue,
                _ => Convert.ToInt64(value)
            };

            return true;
        }
        catch
        {
            result = default;
            return false;
        }
    }

    private static string DecodeUtf8String(byte[] bytes)
    {
        int effectiveLength = bytes.Length;
        while (effectiveLength > 0 && bytes[effectiveLength - 1] == 0)
        {
            effectiveLength--;
        }

        return Encoding.UTF8.GetString(bytes, 0, effectiveLength);
    }

    private static string GetMessageName(Mesg mesg)
        => string.IsNullOrWhiteSpace(mesg.Name) ? $"mesg_{mesg.Num}" : mesg.Name;

    private static string GetStandardFieldName(Field field)
    {
        if (string.IsNullOrWhiteSpace(field.Name))
        {
            return $"field_{field.Num}";
        }

        return string.Equals(field.Name, "unknown", StringComparison.OrdinalIgnoreCase)
            ? $"unknown_{field.Num}"
            : field.Name;
    }

    private static string GetDeveloperFieldName(
        DeveloperField developerField,
        GarminDeveloperFieldDescriptionMetadata? fieldDescription)
    {
        if (!string.IsNullOrWhiteSpace(developerField.Name))
        {
            return developerField.Name;
        }

        if (!string.IsNullOrWhiteSpace(fieldDescription?.Name))
        {
            return fieldDescription.Name!;
        }

        return $"developer_{developerField.DeveloperDataIndex}_{developerField.Num}";
    }

    private static string GetBaseTypeName(byte baseType)
        => Fit.BaseType[baseType & Fit.BaseTypeNumMask].typeName;
}
