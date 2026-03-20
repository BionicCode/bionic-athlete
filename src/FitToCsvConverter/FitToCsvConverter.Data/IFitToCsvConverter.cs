namespace FitToCsvConverter.Data;

using BionicCode.Utilities.Net;

public interface IFitToCsvConverter
{
    Task<int> ExportToCsvAsync(IEnumerable<ConversionInfo> conversionInfoList, int conversionInfoCount, IProgress<ProgressData> progressReporter);
}