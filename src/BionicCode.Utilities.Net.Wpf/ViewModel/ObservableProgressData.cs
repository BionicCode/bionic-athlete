namespace BionicCode.Utilities.Net;

public class ObservableProgressData : ViewModel
{
    private string _message;
    private double _progress;
    private string _operationTitle;
    private readonly SetValueOptions _setValueOptions;
    private double _maxValue;
    private bool _isIndeterminate;

    public ObservableProgressData(double progress, double maxValue, string message, string operationTitle)
    {
        _message = message ?? string.Empty;
        _progress = progress;
        _setValueOptions = SetValueOptions.Default with { IsRejectInvalidValueEnabled = true, IsThrowExceptionOnValidationErrorEnabled = true, IsRejectEqualValuesEnabled = true };
        _operationTitle = operationTitle ?? string.Empty;
        _maxValue = maxValue;
        Id = Guid.NewGuid();
    }

    public static ObservableProgressData Empty { get; } = new ObservableProgressData(0, -1, string.Empty, string.Empty);

    public void Update(ProgressData value)
    {
        Progress = value.Progress;
        MaxValue = value.MaxValue;
        Message = value.Message;
        IsIndeterminate = value.IsIndeterminate;
    }

    /// <summary>
    /// The progress message text.
    /// </summary>
    public string Message
    {
        get => _message;
        internal set => _ = TrySetValue(value, ref _message, _setValueOptions);
    }
    /// <summary>
    /// The progress value.
    /// </summary>
    public double Progress
    {
        get => _progress;
        internal set
        {
            _ = TrySetValue(value, ref _progress, _setValueOptions);
            OnPropertyChanged(nameof(ProgressPercentage));
        }
    }

    public double MaxValue
    {
        get => _maxValue;
        internal set => _ = TrySetValue(value, ref _maxValue, _setValueOptions);
    }
    /// <summary>
    /// The progress value as percentage.
    /// </summary>
    public double ProgressPercentage => (_maxValue > 0)
        ? _progress / _maxValue * 100.0
        : 0.0;

    /// <summary>
    /// Indicates that the progress reporting is indeterminate.
    /// </summary>
    public new bool IsIndeterminate
    {
        get => _isIndeterminate;
        internal set => _ = TrySetValue(value, ref _isIndeterminate, _setValueOptions);
    }

    public string OperationTitle
    {
        get => _operationTitle;
        internal set => _ = TrySetValue(value, ref _operationTitle, _setValueOptions);
    }

    public Guid Id { get; }

    public bool IsCompleted => Progress >= 1.0;
}
