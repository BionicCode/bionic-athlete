namespace FitToCsvConverter.Data;

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading.Tasks;
using BionicCode.Utilities.Net;

public static class FitConverter
{
    private const string ScriptFilePath = @"Tools\fit2Csv.ps1";
    private const string FitCsvToolPath = @"Tools\fitCsvTool.jar";

    public static async Task<int> RunFitToCsvAsync(ImmutableList<ConversionInfo> conversionInfoList)
    {
        ArgumentNullExceptionAdvanced.ThrowIfNullOrEmpty(conversionInfoList);

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

        foreach (ConversionInfo conversionInfo in conversionInfoList)
        {
            string destinationDirectory = Path.GetDirectoryName(conversionInfo.DestinationFilePath) ?? throw new InvalidOperationException("Destination directory cannot be determined.");
            string destinationFileName = Path.GetFileName(conversionInfo.DestinationFilePath) ?? throw new InvalidOperationException("Destination file name cannot be determined.");

            startInfo.ArgumentList.Add("-DestinationDirectory");
            startInfo.ArgumentList.Add(destinationDirectory);

            startInfo.ArgumentList.Add("-DestinationFileName");
            startInfo.ArgumentList.Add(destinationFileName);

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
        }

        return 0;
    }
}
