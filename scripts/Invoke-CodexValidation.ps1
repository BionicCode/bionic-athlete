[CmdletBinding()]
param(
    [ValidateSet('Restore', 'Build', 'Test', 'Validate', 'RestoreValidate')]
    [string]$Action = 'Validate',

    [string]$BuildTarget = 'test\FitToCsvConverter.Test\FitToCsvConverter.Test.csproj',

    [string]$TestProject = 'test\FitToCsvConverter.Test\FitToCsvConverter.Test.csproj',

    [ValidateSet('CurrentUser', 'WorkspaceLocal')]
    [string]$ProfileMode = 'CurrentUser',

    [string]$Configuration = 'Debug'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path -Path $PSScriptRoot -ChildPath '..'))
$codexEnvironmentRoot = Join-Path -Path $repositoryRoot -ChildPath '.codex-env'
$localDotNetHome = Join-Path -Path $repositoryRoot -ChildPath '.dotnet'
$localTempDirectory = Join-Path -Path $codexEnvironmentRoot -ChildPath 'temp'
$workspaceProfileRoot = Join-Path -Path $codexEnvironmentRoot -ChildPath 'userprofile'
$workspaceAppData = Join-Path -Path $workspaceProfileRoot -ChildPath 'AppData\Roaming'
$workspaceLocalAppData = Join-Path -Path $workspaceProfileRoot -ChildPath 'AppData\Local'

[System.IO.Directory]::CreateDirectory($codexEnvironmentRoot) | Out-Null
[System.IO.Directory]::CreateDirectory($localDotNetHome) | Out-Null
[System.IO.Directory]::CreateDirectory($localTempDirectory) | Out-Null

function Resolve-RepositoryPath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RelativeOrAbsolutePath
    )

    if ([System.IO.Path]::IsPathRooted($RelativeOrAbsolutePath)) {
        return [System.IO.Path]::GetFullPath($RelativeOrAbsolutePath)
    }

    return [System.IO.Path]::GetFullPath((Join-Path -Path $repositoryRoot -ChildPath $RelativeOrAbsolutePath))
}

function Get-DotNetExecutablePath {
    $dotNetCommand = Get-Command -Name dotnet -ErrorAction SilentlyContinue
    if ($null -eq $dotNetCommand -or [string]::IsNullOrWhiteSpace($dotNetCommand.Source)) {
        throw 'dotnet was not found on PATH.'
    }

    return $dotNetCommand.Source
}

function Get-MsBuildExecutablePath {
    $vsWherePath = Join-Path -Path ${env:ProgramFiles(x86)} -ChildPath 'Microsoft Visual Studio\Installer\vswhere.exe'
    if (Test-Path -LiteralPath $vsWherePath -PathType Leaf) {
        $installationPath = & $vsWherePath -latest -products * -requires Microsoft.Component.MSBuild -property installationPath
        if (-not [string]::IsNullOrWhiteSpace($installationPath)) {
            $candidatePath = Join-Path -Path $installationPath.Trim() -ChildPath 'MSBuild\Current\Bin\MSBuild.exe'
            if (Test-Path -LiteralPath $candidatePath -PathType Leaf) {
                return $candidatePath
            }
        }
    }

    $fallbackPath = 'C:\Program Files\Microsoft Visual Studio\18\Enterprise\MSBuild\Current\Bin\MSBuild.exe'
    if (Test-Path -LiteralPath $fallbackPath -PathType Leaf) {
        return $fallbackPath
    }

    throw 'MSBuild.exe was not found. Install Visual Studio Build Tools or update this script to match the local installation path.'
}

function New-SanitizedEnvironment {
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$AdditionalPathEntries,

        [Parameter(Mandatory = $true)]
        [ValidateSet('MsBuild', 'DotNet')]
        [string]$ToolKind,

        [switch]$IncludeCurrentUserNuGetState
    )

    $environmentTable = [ordered]@{}

    $pathEntries = [System.Collections.Generic.List[string]]::new()
    foreach ($entry in $AdditionalPathEntries) {
        if (-not [string]::IsNullOrWhiteSpace($entry)) {
            $pathEntries.Add($entry)
        }
    }

    foreach ($entry in @('C:\WINDOWS\system32', 'C:\WINDOWS')) {
        $pathEntries.Add($entry)
    }

    $uniquePathEntries = [System.Collections.Generic.List[string]]::new()
    $seenPathEntries = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
    foreach ($entry in $pathEntries) {
        if ($seenPathEntries.Add($entry)) {
            $uniquePathEntries.Add($entry)
        }
    }

    $environmentTable['Path'] = [string]::Join([System.IO.Path]::PathSeparator, $uniquePathEntries)
    $environmentTable['SystemRoot'] = 'C:\WINDOWS'
    $environmentTable['TEMP'] = $localTempDirectory
    $environmentTable['TMP'] = $localTempDirectory

    if ($ProfileMode -eq 'WorkspaceLocal') {
        [System.IO.Directory]::CreateDirectory($workspaceProfileRoot) | Out-Null
        [System.IO.Directory]::CreateDirectory($workspaceAppData) | Out-Null
        [System.IO.Directory]::CreateDirectory($workspaceLocalAppData) | Out-Null

        $environmentTable['USERPROFILE'] = $workspaceProfileRoot
        $environmentTable['APPDATA'] = $workspaceAppData
        $environmentTable['LOCALAPPDATA'] = $workspaceLocalAppData
    }
    else {
        if (Test-Path -LiteralPath 'Env:USERPROFILE') {
            $environmentTable['USERPROFILE'] = (Get-Item -LiteralPath 'Env:USERPROFILE').Value
        }

        if ($IncludeCurrentUserNuGetState) {
            foreach ($variableName in @('APPDATA', 'LOCALAPPDATA', 'NUGET_PACKAGES')) {
                if (Test-Path -LiteralPath "Env:$variableName") {
                    $environmentTable[$variableName] = (Get-Item -LiteralPath "Env:$variableName").Value
                }
            }
        }
    }

    if ($ToolKind -eq 'DotNet') {
        $environmentTable['DOTNET_CLI_HOME'] = $localDotNetHome
        $environmentTable['DOTNET_SKIP_FIRST_TIME_EXPERIENCE'] = '1'
        $environmentTable['DOTNET_CLI_TELEMETRY_OPTOUT'] = '1'
        $environmentTable['DOTNET_ADD_GLOBAL_TOOLS_TO_PATH'] = '0'
        $environmentTable['MSBuildEnableWorkloadResolver'] = 'false'
    }

    return $environmentTable
}

function Invoke-SanitizedProcess {
    param(
        [Parameter(Mandatory = $true)]
        [string]$FilePath,

        [Parameter(Mandatory = $true)]
        [string[]]$ArgumentList,

        [Parameter(Mandatory = $true)]
        [string[]]$AdditionalPathEntries,

        [Parameter(Mandatory = $true)]
        [ValidateSet('MsBuild', 'DotNet')]
        [string]$ToolKind,

        [switch]$IncludeCurrentUserNuGetState
    )

    $startInfo = [System.Diagnostics.ProcessStartInfo]::new()
    $startInfo.FileName = $FilePath
    $startInfo.WorkingDirectory = $repositoryRoot
    $startInfo.UseShellExecute = $false
    $startInfo.RedirectStandardOutput = $true
    $startInfo.RedirectStandardError = $true

    foreach ($argument in $ArgumentList) {
        $null = $startInfo.ArgumentList.Add($argument)
    }

    $startInfo.Environment.Clear()
    $environmentTable = New-SanitizedEnvironment -AdditionalPathEntries $AdditionalPathEntries -ToolKind $ToolKind -IncludeCurrentUserNuGetState:$IncludeCurrentUserNuGetState
    foreach ($entry in $environmentTable.GetEnumerator()) {
        $startInfo.Environment[$entry.Key] = [string]$entry.Value
    }

    $process = [System.Diagnostics.Process]::Start($startInfo)
    $standardOutput = $process.StandardOutput.ReadToEnd()
    $standardError = $process.StandardError.ReadToEnd()
    $process.WaitForExit()

    if (-not [string]::IsNullOrWhiteSpace($standardOutput)) {
        Write-Host -NoNewline $standardOutput
    }

    if (-not [string]::IsNullOrWhiteSpace($standardError)) {
        Write-Error $standardError
    }

    if ($process.ExitCode -ne 0) {
        throw "Process failed with exit code $($process.ExitCode): $FilePath $($ArgumentList -join ' ')"
    }
}

$dotNetExecutablePath = Get-DotNetExecutablePath
$msBuildExecutablePath = Get-MsBuildExecutablePath
$buildTargetPath = Resolve-RepositoryPath -RelativeOrAbsolutePath $BuildTarget
$testProjectPath = Resolve-RepositoryPath -RelativeOrAbsolutePath $TestProject
$dotNetDirectory = Split-Path -Path $dotNetExecutablePath -Parent
$msBuildDirectory = Split-Path -Path $msBuildExecutablePath -Parent

function Invoke-RestoreStep {
    Invoke-SanitizedProcess `
        -FilePath $msBuildExecutablePath `
        -ArgumentList @(
            $buildTargetPath,
            '/t:Restore',
            "/p:Configuration=$Configuration",
            '/v:minimal',
            '/nologo') `
        -AdditionalPathEntries @($msBuildDirectory, $dotNetDirectory) `
        -ToolKind 'MsBuild' `
        -IncludeCurrentUserNuGetState
}

function Invoke-BuildStep {
    Invoke-SanitizedProcess `
        -FilePath $msBuildExecutablePath `
        -ArgumentList @(
            $buildTargetPath,
            '/t:Build',
            '/p:Restore=false',
            "/p:Configuration=$Configuration",
            '/v:minimal',
            '/nologo') `
        -AdditionalPathEntries @($msBuildDirectory, $dotNetDirectory) `
        -ToolKind 'MsBuild'
}

function Invoke-TestStep {
    Invoke-SanitizedProcess `
        -FilePath $dotNetExecutablePath `
        -ArgumentList @(
            'test',
            $testProjectPath,
            '--no-build',
            '--no-restore',
            '-c',
            $Configuration,
            '-v',
            'minimal') `
        -AdditionalPathEntries @($dotNetDirectory) `
        -ToolKind 'DotNet'
}

switch ($Action) {
    'Restore' {
        Invoke-RestoreStep
    }
    'Build' {
        Invoke-BuildStep
    }
    'Test' {
        Invoke-TestStep
    }
    'Validate' {
        Invoke-TestStep
    }
    'RestoreValidate' {
        Invoke-RestoreStep
        Invoke-TestStep
    }
    default {
        throw "Unsupported action '$Action'."
    }
}

Write-Host "Codex validation action '$Action' completed successfully."
