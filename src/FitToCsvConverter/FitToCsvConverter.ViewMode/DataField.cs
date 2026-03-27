namespace FitToCsvConverter.ViewModel;

using BionicCode.Utilities.Net;
using FitToCsvConverter.Data.Fields;

public class DataField : ViewModel
{
    private readonly SetValueOptions _setValueOptions;
    private bool _isSelected;

    public DataField(FitField fitField)
    {
        ArgumentNullExceptionAdvanced.ThrowIfNull(fitField);

        FitField = fitField;

        _isSelected = true;
        _setValueOptions = SetValueOptions.Default with
        {
            IsRejectInvalidValueEnabled = true,
            IsThrowExceptionOnValidationErrorEnabled = true,
            IsRejectEqualValuesEnabled = true
        };
    }

    internal FitField FitField { get; }
    public string Name => FitField.State.DisplayName;
    public FitFieldKey Id => FitField.Original.Key;

    public bool IsSelected
    {
        get => _isSelected;
        set => TrySetValue(value, ref _isSelected, _setValueOptions);
    }
}
