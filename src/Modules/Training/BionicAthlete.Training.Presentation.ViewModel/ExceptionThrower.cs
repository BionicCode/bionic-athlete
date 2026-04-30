namespace BionicAthlete.Training.Presentation.ViewModel;

using System;
using System.Diagnostics.CodeAnalysis;
using BionicAthlete.Training.Application.Decoding;
using BionicAthlete.Training.Application.Exceptions;
using BionicAthlete.Training.Domain.Activities;
using BionicCode.Utilities.Net;

public static class ExceptionThrower
{
    [return: NotNull]
    public static FitActivity GetActivityOrThrowIfDecodingFailed([NotNull] this FitActivityDecodeResult decodeResult)
    {
        if (!decodeResult.IsSuccess
            || decodeResult.Activity is null)
        {
            string reason = decodeResult.Issues
                .Select(issue => issue.ToString())
                .JoinToString(Environment.NewLine);
            throw new DecodingFailedException(
                $"Failed to decode FIT file '{decodeResult.Source}': {reason}.",
                decodeResult.Issues);
        }

        return decodeResult.Activity;
    }
}
