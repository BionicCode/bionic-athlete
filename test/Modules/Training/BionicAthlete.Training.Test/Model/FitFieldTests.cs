namespace BionicAthlete.Training.Test.Model;

using System.Collections.Immutable;
using BionicAthlete.Training.Domain.Activities;
using BionicAthlete.Training.Domain.Fields;

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

        ImmutableArray<object?> effectiveEditedValues = field.GetEffectiveDecodedValues();
        Assert.Collection(
            effectiveEditedValues,
            value => Assert.Equal(150, Assert.IsType<int>(value)),
            value => Assert.Equal(151, Assert.IsType<int>(value)));
        Assert.Equal((byte)140, field.Original.OriginalValues[0].DecodedValue);
        Assert.Equal((byte)141, field.Original.OriginalValues[1].DecodedValue);

        field.ResetEditedDecodedValues();

        ImmutableArray<object?> effectiveOriginalValues = field.GetEffectiveDecodedValues();
        Assert.Collection(
            effectiveOriginalValues,
            value => Assert.Equal((byte)140, Assert.IsType<byte>(value)),
            value => Assert.Equal((byte)141, Assert.IsType<byte>(value)));
        Assert.False(field.State.HasEditedDecodedValues);
    }
}