namespace BionicCode.Utilities.Net;

public class ObservableProgressData : ViewModel
{
    private string _message;
    private double _progress;
    private string _operationTitle;
    private readonly SetValueOptions _setValueOptions;

    public ObservableProgressData(string message, double progress, string operationTitle)
    {
        Message = message;
        Progress = progress;
        _setValueOptions = SetValueOptions.Default with { IsRejectInvalidValueEnabled = true, IsThrowExceptionOnValidationErrorEnabled = true, IsRejectEqualValuesEnabled = true };
        _operationTitle = operationTitle;
        Id = Guid.NewGuid();
    }

    public ObservableProgressData() : this(string.Empty, 0.0, string.Empty)
    {
    }

    public void Update(ProgressData value)
    {
        Progress = value.Progress;
        Message = value.Message;
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
    /// <summary>
    /// The progress value as percentage.
    /// </summary>
    public double ProgressPercentage => _progress * 100.0;

    public string OperationTitle
    {
        get => _operationTitle;
        internal set => _ = TrySetValue(value, ref _operationTitle, _setValueOptions);
    }

    public Guid Id { get; }

    public bool IsCompleted => Progress >= 1.0;
}
