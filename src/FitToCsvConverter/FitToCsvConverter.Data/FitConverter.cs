namespace FitToCsvConverter.Data;

using System.Diagnostics;
using BionicCode.Utilities.Net;

public static class FitConverter
{
    private const string ScriptFilePath = @"Tools\fit2Csv.ps1";
    private const string FitCsvToolPath = @"Tools\fitCsvTool.jar";

    public static async Task<int> RunFitToCsvAsync(IEnumerable<ConversionInfo> conversionInfoList, int conversionInfoCount, IProgress<ProgressData> progressReporter)
    {
        ArgumentNullExceptionAdvanced.ThrowIfNullOrEmpty(conversionInfoList);
        ArgumentNullExceptionAdvanced.ThrowIfNull(progressReporter);

        var startInfo = new ProcessStartInfo
        {
            FileName = "pwsh.exe", // or "powershell.exe" if you need Windows PowerShell 5.1
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        startInfo.ArgumentList.Add("-NoLogo");
        startInfo.ArgumentList.Add("-NoProfile");
        startInfo.ArgumentList.Add("-NonInteractive");
        startInfo.ArgumentList.Add("-ExecutionPolicy");
        startInfo.ArgumentList.Add("Bypass");
        startInfo.ArgumentList.Add("-File");
        startInfo.ArgumentList.Add(ScriptFilePath);

        startInfo.ArgumentList.Add("-FitCsvToolPath");
        startInfo.ArgumentList.Add(FitCsvToolPath);

        int completedCount = 0;
        foreach (ConversionInfo conversionInfo in conversionInfoList)
        {
            progressReporter.Report(new ProgressData
            {
                Progress = (double)completedCount / conversionInfoCount,
                Message = $"Exporting fit file {completedCount + 1} of {conversionInfoCount}: {conversionInfo.SourceFilePath}"
            });

            string destinationDirectory = Path.GetDirectoryName(conversionInfo.DestinationFilePath) ?? throw new InvalidOperationException("Destination directory cannot be determined.");
            string destinationFileName = Path.GetFileName(conversionInfo.DestinationFilePath) ?? throw new InvalidOperationException("Destination file name cannot be determined.");

            _ = startInfo.ArgumentList.Remove("-DestinationDirectory");
            _ = startInfo.ArgumentList.Remove(destinationDirectory);
            startInfo.ArgumentList.Add("-DestinationDirectory");
            startInfo.ArgumentList.Add(destinationDirectory);

            _ = startInfo.ArgumentList.Remove("-DestinationFileName");
            _ = startInfo.ArgumentList.Remove(destinationFileName);
            startInfo.ArgumentList.Add("-DestinationFileName");
            startInfo.ArgumentList.Add(destinationFileName);

            _ = startInfo.ArgumentList.Remove("-SourcePath");
            _ = startInfo.ArgumentList.Remove(conversionInfo.SourceFilePath);
            startInfo.ArgumentList.Add("-SourcePath");
            startInfo.ArgumentList.Add(conversionInfo.SourceFilePath);

            using var process = new Process
            {
                StartInfo = startInfo
            };

            _ = process.Start();

            string standardOutput = await process.StandardOutput.ReadToEndAsync();
            string standardError = await process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException(
                    $"fit2Csv.ps1 failed with exit code {process.ExitCode}.{Environment.NewLine}" +
                    $"STDERR: {standardError}{Environment.NewLine}" +
                    $"STDOUT: {standardOutput}");
            }

            completedCount++;
        }

        progressReporter.Report(new ProgressData
        {
            Progress = 1.0,
            Message = "All fit files have been successfully exported."
        });

        return 0;
    }
}
