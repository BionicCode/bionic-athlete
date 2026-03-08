namespace BionicCode.Utilities.Net;

using System;

public readonly struct ProgressData : IEquatable<ProgressData>
{
    /// <summary>
    /// Data model to report progress to a implementation of <see cref="IProgressReporterCommon"/>. When using the <see cref="IProgress{T}"/> returned from the <see cref="IProgressReporterCommon.CreateProgressReporterFromCurrentThread"/> method, the <see cref="ProgressData"/> serves as the argument.
    /// </summary>
    /// <param name="message">A progress message.</param>
    /// <param name="progress">The progress value.</param>
    public ProgressData(string message, double progress)
    {
        Message = message;
        Progress = progress;
    }

    /// <summary>
    /// The progress message text.
    /// </summary>
    public string Message { get; }
    /// <summary>
    /// The progress value.
    /// </summary>
    public double Progress { get; }

    public override bool Equals(object obj) => obj is ProgressData other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(Message, Progress);

    public static bool operator ==(ProgressData left, ProgressData right) => left.Equals(right);

    public static bool operator !=(ProgressData left, ProgressData right) => !(left == right);

    public bool Equals(ProgressData other) => Message.Equals(other.Message, StringComparison.Ordinal)
        && Progress.Equals(other.Progress);
}
