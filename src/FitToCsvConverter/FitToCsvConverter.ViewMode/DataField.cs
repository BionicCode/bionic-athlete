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

        Name = fitField.Original.OriginalName;
        Id = fitField.Original.Key;
        _isSelected = true;
        _setValueOptions = SetValueOptions.Default with
        {
            IsRejectInvalidValueEnabled = true,
            IsThrowExceptionOnValidationErrorEnabled = true,
            IsRejectEqualValuesEnabled = true
        };
    }

    public string Name { get; }
    public FitFieldKey Id { get; }

    public bool IsSelected
    {
        get => _isSelected;
        set => TrySetValue(value, ref _isSelected, _setValueOptions);
    }
}
