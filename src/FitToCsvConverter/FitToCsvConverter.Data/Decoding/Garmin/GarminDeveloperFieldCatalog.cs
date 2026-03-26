namespace FitToCsvConverter.Data.Decoding.Garmin;

using System.Collections.Immutable;
using Dynastream.Fit;

internal sealed class GarminDeveloperFieldCatalog
{
    private readonly ImmutableDictionary<byte, GarminDeveloperDataIdentity> _developerDataByIndex;
    private readonly ImmutableDictionary<GarminDeveloperFieldCatalogKey, GarminDeveloperFieldDescriptionMetadata> _fieldDescriptionsByKey;

    private GarminDeveloperFieldCatalog(
        ImmutableDictionary<byte, GarminDeveloperDataIdentity> developerDataByIndex,
        ImmutableDictionary<GarminDeveloperFieldCatalogKey, GarminDeveloperFieldDescriptionMetadata> fieldDescriptionsByKey)
    {
        _developerDataByIndex = developerDataByIndex;
        _fieldDescriptionsByKey = fieldDescriptionsByKey;
    }

    public static GarminDeveloperFieldCatalog Create(FitMessages fitMessages)
    {
        ArgumentNullException.ThrowIfNull(fitMessages);

        Dictionary<byte, GarminDeveloperDataIdentity> developerDataByIndex = [];
        foreach (DeveloperDataIdMesg developerDataIdMessage in fitMessages.DeveloperDataIdMesgs)
        {
            byte? developerDataIndex = developerDataIdMessage.GetDeveloperDataIndex();
            if (developerDataIndex is not byte index)
            {
                continue;
            }

            developerDataByIndex[index] = new GarminDeveloperDataIdentity(
                ReadApplicationId(developerDataIdMessage),
                developerDataIdMessage.GetApplicationVersion());
        }

        Dictionary<GarminDeveloperFieldCatalogKey, GarminDeveloperFieldDescriptionMetadata> fieldDescriptionsByKey = [];
        foreach (FieldDescriptionMesg fieldDescriptionMessage in fitMessages.FieldDescriptionMesgs)
        {
            byte? developerDataIndex = fieldDescriptionMessage.GetDeveloperDataIndex();
            byte? fieldDefinitionNumber = fieldDescriptionMessage.GetFieldDefinitionNumber();
            if (developerDataIndex is not byte index || fieldDefinitionNumber is not byte fieldNumber)
            {
                continue;
            }

            _ = developerDataByIndex.TryGetValue(index, out GarminDeveloperDataIdentity? developerDataIdentity);
            fieldDescriptionsByKey[new GarminDeveloperFieldCatalogKey(index, fieldNumber)] =
                new GarminDeveloperFieldDescriptionMetadata(
                    fieldDescriptionMessage.GetFieldNameAsString(0),
                    fieldDescriptionMessage.GetUnitsAsString(0),
                    fieldDescriptionMessage.GetFitBaseTypeId() ?? Fit.UInt8,
                    fieldDescriptionMessage.GetScale() ?? 1,
                    fieldDescriptionMessage.GetOffset() ?? 0,
                    fieldDescriptionMessage.GetNativeFieldNum(),
                    fieldDescriptionMessage.GetNativeMesgNum(),
                    fieldDescriptionMessage.GetArray() is byte arrayFlag && arrayFlag != 0,
                    developerDataIdentity);
        }

        return new GarminDeveloperFieldCatalog(
            developerDataByIndex.ToImmutableDictionary(),
            fieldDescriptionsByKey.ToImmutableDictionary());
    }

    public GarminDeveloperDataIdentity? GetDeveloperDataIdentity(byte developerDataIndex)
        => _developerDataByIndex.GetValueOrDefault(developerDataIndex);

    public GarminDeveloperFieldDescriptionMetadata? GetFieldDescription(byte developerDataIndex, byte fieldDefinitionNumber)
        => _fieldDescriptionsByKey.GetValueOrDefault(new GarminDeveloperFieldCatalogKey(developerDataIndex, fieldDefinitionNumber));

    private static ImmutableArray<byte> ReadApplicationId(DeveloperDataIdMesg developerDataIdMessage)
    {
        ImmutableArray<byte>.Builder builder = ImmutableArray.CreateBuilder<byte>(developerDataIdMessage.GetNumApplicationId());
        for (int index = 0; index < developerDataIdMessage.GetNumApplicationId(); index++)
        {
            if (developerDataIdMessage.GetApplicationId(index) is byte applicationIdByte)
            {
                builder.Add(applicationIdByte);
            }
        }

        return builder.ToImmutable();
    }
}

internal sealed record GarminDeveloperDataIdentity(
    ImmutableArray<byte> ApplicationIdBytes,
    uint? ApplicationVersion);

internal sealed record GarminDeveloperFieldDescriptionMetadata(
    string? Name,
    string? Units,
    byte BaseType,
    double Scale,
    double Offset,
    byte? NativeFieldNumber,
    ushort? NativeMessageNumber,
    bool IsArray,
    GarminDeveloperDataIdentity? DeveloperDataIdentity);

internal readonly record struct GarminDeveloperFieldCatalogKey(byte DeveloperDataIndex, byte FieldDefinitionNumber);
