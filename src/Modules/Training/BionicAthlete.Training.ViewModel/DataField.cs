namespace BionicAthlete.Training.Presentation;

using BionicAthlete.Training.Domain.Fields;
using BionicCode.Utilities.Net;

public class DataField : ViewModel
{
    private readonly SetValueOptions _setValueOptions;
    private bool _isSelected;

    public DataField(FitField fitField, int displayOrder)
    {
        ArgumentNullExceptionAdvanced.ThrowIfNull(fitField);
        ArgumentOutOfRangeExceptionAdvanced.ThrowIfNegative(displayOrder);

        FitField = fitField;
        DisplayOrder = displayOrder;

        _isSelected = true;
        _setValueOptions = SetValueOptions.Default with
        {
            IsRejectInvalidValueEnabled = true,
            IsThrowExceptionOnValidationErrorEnabled = true,
            IsRejectEqualValuesEnabled = true
        };
    }

    internal FitField FitField { get; }
    internal int DisplayOrder { get; }
    public string Name => FitField.State.DisplayName;
    public FitFieldKey Id => FitField.Original.Key;

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (TrySetValue(value, ref _isSelected, _setValueOptions))
            {
                FitField.SetExportInclusion(value);
            }
        }
    }
}
