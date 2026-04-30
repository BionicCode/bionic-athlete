namespace BionicAthlete.Training.Application.Exceptions;

using System;
using System.Collections.Immutable;
using BionicAthlete.Training.Application.Decoding;

public class DecodingFailedException : Exception
{
    public DecodingFailedException()
    {
    }

    public DecodingFailedException(string message, ImmutableArray<FitDecodeIssue> issues)
        : base(message) => Issues = issues;

    public DecodingFailedException(string message, Exception innerException, ImmutableArray<FitDecodeIssue> issues)
        : base(message, innerException) => Issues = issues;

    public ImmutableArray<FitDecodeIssue> Issues { get; }

    public DecodingFailedException(string message)
        : base(message) => Issues = ImmutableArray<FitDecodeIssue>.Empty;

    public DecodingFailedException(string message, Exception innerException)
        : base(message, innerException) => Issues = ImmutableArray<FitDecodeIssue>.Empty;
}
