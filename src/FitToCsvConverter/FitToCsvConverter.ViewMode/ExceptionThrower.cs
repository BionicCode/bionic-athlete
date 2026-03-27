namespace FitToCsvConverter.ViewModel;

using System;
using System.Diagnostics.CodeAnalysis;
using BionicCode.Utilities.Net;
using FitToCsvConverter.Data.Activities;
using FitToCsvConverter.Data.Decoding;

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
