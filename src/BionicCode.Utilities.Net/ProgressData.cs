namespace BionicCode.Utilities.Net;

using System;
using System.ComponentModel;

public readonly struct ProgressData : IEquatable<ProgressData>
{
    /// <summary>
    /// Data model to report progress to a implementation of <see cref="IProgressReporterCommon"/>. When using the <see cref="IProgress{T}"/> returned from the <see cref="IProgressReporterCommon.CreateProgressReporterFromCurrentThread"/> method, the <see cref="ProgressData"/> serves as the argument.
    /// </summary>
    /// <param name="message">A progress message.</param>
    /// <param name="progress">The progress value.</param>
    /// <param name="isIndeterminate">Indicates whether the progress operation is indeterminate.</param>
    [Obsolete("This constructor is deprecated. Use the constructor that includes the 'maxValue' parameter to enable a calculated value for 'ProgressPercentage'.", error: false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public ProgressData(string message, double progress, bool isIndeterminate = false)
    {
        Message = message;
        Progress = progress;
        MaxValue = 1;
        IsIndeterminate = isIndeterminate;
    }
    /// <summary>
    /// Data model to report progress to a implementation of <see cref="IProgressReporterCommon"/>. When using the <see cref="IProgress{T}"/> returned from the <see cref="IProgressReporterCommon.CreateProgressReporterFromCurrentThread"/> method, the <see cref="ProgressData"/> serves as the argument.
    /// </summary>
    /// <param name="message">A progress message.</param>
    /// <param name="progress">The progress value.</param>
    /// <param name="maxValue">The maximum progress value that corresponds to 100% progress. This parameter is used to calculate the percentage value for the <see cref="ProgressPercentage"/> property.</param>
    /// <param name="isIndeterminate">Indicates whether the progress operation is indeterminate.</param>
    public ProgressData(double progress, double maxValue, string message, bool isIndeterminate = false)
    {
        Message = message;
        Progress = progress;
        MaxValue = maxValue;
        IsIndeterminate = isIndeterminate;
    }

    /// <summary>
    /// The progress message text.
    /// </summary>
    public string Message { get; init; }
    /// <summary>
    /// The progress value.
    /// </summary>
    public double Progress { get; init; }

    public bool IsIndeterminate { get; init; }

    /// <summary>
    /// Gets the current progress as a percentage of the maximum value defined by the <see cref="MaxValue"/> property.
    /// </summary>
    public double ProgressPercentage => (MaxValue > 0)
        ? Progress / MaxValue * 100.0
        : 0.0;

    /// <summary>
    /// The maximum progress value that corresponds to 100% progress. This property is used to calculate the percentage value when <see cref="IsPercentage"/> is set to <c>true</c>.
    /// </summary>
    public double MaxValue { get; init; }
    public override bool Equals(object? obj) => obj is ProgressData other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(Message, Progress, MaxValue, IsIndeterminate);
    public static bool operator ==(ProgressData left, ProgressData right) => left.Equals(right);

    public static bool operator !=(ProgressData left, ProgressData right) => !(left == right);

    public bool Equals(ProgressData other) => Message.Equals(other.Message, StringComparison.Ordinal)
        && Progress.Equals(other.Progress)
        && MaxValue.Equals(other.MaxValue)
        && IsIndeterminate.Equals(other.IsIndeterminate);
}