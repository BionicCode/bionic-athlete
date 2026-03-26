namespace FitToCsvConverter.Test.Model;

using System.Collections.Immutable;
using FitToCsvConverter.Data.Activities;
using FitToCsvConverter.Data.Fields;

public sealed class FitFieldTests
{
    [Fact]
    public void EditedValuesResetBackToOriginalDecodedValues()
    {
        FitField field = new(
            new FitFieldSnapshot(
                new FitFieldKey(FitNodeType.Record, FitFieldKind.Standard, 20, 3),
                FitExportColumnKey.FromField(new FitFieldKey(FitNodeType.Record, FitFieldKind.Standard, 20, 3)),
                "heart_rate",
                "record",
                FitFieldKind.Standard,
                baseType: 2,
                baseTypeName: "uint8",
                profileTypeName: "HeartRate",
                units: "bpm",
                scale: 1,
                offset: 0,
                isAccumulated: false,
                isExpandedField: false,
                developerApplicationIdBytes: ImmutableArray<byte>.Empty,
                developerApplicationVersion: null,
                nativeOverrideFieldNumber: null,
                nativeOverrideMessageNumber: null,
                isArray: true,
                originalValues: ImmutableArray.Create(
                    new FitFieldValue((byte)140, (byte)140),
                    new FitFieldValue((byte)141, (byte)141))));

        field.SetEditedDecodedValues([150, 151]);

        Assert.Equal([(object?)150, (object?)151], field.GetEffectiveDecodedValues());
        Assert.Equal((byte)140, field.Original.OriginalValues[0].DecodedValue);
        Assert.Equal((byte)141, field.Original.OriginalValues[1].DecodedValue);

        field.ResetEditedDecodedValues();

        Assert.Equal([(object?)(byte)140, (object?)(byte)141], field.GetEffectiveDecodedValues());
        Assert.False(field.State.HasEditedDecodedValues);
    }
}
