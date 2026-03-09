# fit2Csv.ps1
# Converts a binary .fit file to a .csv file using Garmin's FitCSVTool.jar.
# Usage: .\fit2Csv.ps1 [-FitCsvToolPath <path>] [-SourcePath <path>] [-DestinationDirectory <path>] [-DestinationFileName <name>]

param(
    [string]$FitCsvToolPath,
    [string]$SourcePath,
    [string]$DestinationDirectory,
    [string]$DestinationFileName
)

# Internal fallback location for FitCSVTool.jar
$defaultFitCsvToolPath = "C:\FitCSVTool.jar"

function Normalize-UserInput {
    param(
        [Parameter(Mandatory = $true)]
        [AllowEmptyString()]
        [string]$Value,

        [Parameter(Mandatory = $true)]
        [string]$FieldName
    )

    $normalizedValue = $Value.Trim()

    if ([string]::IsNullOrWhiteSpace($normalizedValue)) {
        throw "$FieldName cannot be empty."
    }

    if (
        ($normalizedValue.StartsWith('"') -and $normalizedValue.EndsWith('"')) -or
        ($normalizedValue.StartsWith("'") -and $normalizedValue.EndsWith("'"))
    ) {
        $normalizedValue = $normalizedValue.Substring(1, $normalizedValue.Length - 2).Trim()
    }

    if ([string]::IsNullOrWhiteSpace($normalizedValue)) {
        throw "$FieldName cannot be empty."
    }

    return $normalizedValue
}

function Resolve-UserPath {
    param(
        [Parameter(Mandatory = $true)]
        [AllowEmptyString()]
        [string]$Path,

        [Parameter(Mandatory = $true)]
        [string]$FieldName
    )

    $normalizedPath = Normalize-UserInput -Value $Path -FieldName $FieldName

    try {
        if ([System.IO.Path]::IsPathRooted($normalizedPath)) {
            return [System.IO.Path]::GetFullPath($normalizedPath)
        }

        return [System.IO.Path]::GetFullPath((Join-Path -Path (Get-Location).ProviderPath -ChildPath $normalizedPath))
    }
    catch {
        throw "Invalid ${FieldName}: $($_.Exception.Message)"
    }
}

function Find-FitCsvToolPathInPath {
    $pathEntries = ($env:PATH -split [System.IO.Path]::PathSeparator) | Select-Object -Unique

    foreach ($entry in $pathEntries) {
        if ([string]::IsNullOrWhiteSpace($entry)) {
            continue
        }

        try {
            $candidateDirectory = Resolve-UserPath -Path $entry -FieldName "PATH entry"
        }
        catch {
            continue
        }

        if (-not (Test-Path -LiteralPath $candidateDirectory -PathType Container)) {
            continue
        }

        $match = Get-ChildItem -LiteralPath $candidateDirectory -Filter "FitCSVTool.jar" -File -Recurse -ErrorAction SilentlyContinue |
            Select-Object -First 1 -ExpandProperty FullName

        if (-not [string]::IsNullOrWhiteSpace($match)) {
            return $match
        }
    }

    return $null
}

function Resolve-FitCsvToolPath {
    param(
        [string]$CommandLinePath,

        [Parameter(Mandatory = $true)]
        [string]$FallbackPath
    )

    $discoveredPath = Find-FitCsvToolPathInPath
    if (-not [string]::IsNullOrWhiteSpace($discoveredPath)) {
        return $discoveredPath
    }

    if (-not [string]::IsNullOrWhiteSpace($CommandLinePath)) {
        return Resolve-UserPath -Path $CommandLinePath -FieldName "FitCSVTool.jar command-line argument"
    }

    return Resolve-UserPath -Path $FallbackPath -FieldName "Internal FitCSVTool.jar path"
}

function Get-ValidatedDestinationName {
    param(
        [Parameter(Mandatory = $true)]
        [AllowEmptyString()]
        [string]$Name
    )

    $normalizedName = Normalize-UserInput -Value $Name -FieldName "Destination file name"

    if ($normalizedName -match '[\\/]') {
        throw "Destination file name must not contain path separators. Enter only the file name, without a directory path."
    }

    if ($normalizedName.EndsWith('.csv', [System.StringComparison]::OrdinalIgnoreCase)) {
        $normalizedName = [System.IO.Path]::GetFileNameWithoutExtension($normalizedName)
    }

    if ([string]::IsNullOrWhiteSpace($normalizedName)) {
        throw "Destination file name cannot be empty."
    }

    if ($normalizedName.IndexOfAny([System.IO.Path]::GetInvalidFileNameChars()) -ge 0) {
        throw "Destination file name contains invalid characters."
    }

    if ($normalizedName -in '.', '..') {
        throw "Destination file name cannot be '.' or '..'."
    }

    $reservedNames = @(
        'CON', 'PRN', 'AUX', 'NUL',
        'COM1', 'COM2', 'COM3', 'COM4', 'COM5', 'COM6', 'COM7', 'COM8', 'COM9',
        'LPT1', 'LPT2', 'LPT3', 'LPT4', 'LPT5', 'LPT6', 'LPT7', 'LPT8', 'LPT9'
    )

    if ($reservedNames -contains $normalizedName.ToUpperInvariant()) {
        throw "Destination file name '$normalizedName' is reserved by Windows."
    }

    return $normalizedName
}

function Get-InputOrPrompt {
    param(
        [AllowEmptyString()]
        [string]$Value,

        [Parameter(Mandatory = $true)]
        [string]$Prompt
    )

    if (-not [string]::IsNullOrWhiteSpace($Value)) {
        return $Value
    }

    return Read-Host $Prompt
}

try {
    $fitCsvToolPath = Resolve-FitCsvToolPath -CommandLinePath $FitCsvToolPath -FallbackPath $defaultFitCsvToolPath
}
catch {
    Write-Error $_.Exception.Message
    exit 1
}

# Validate prerequisites
if (-not (Get-Command java -ErrorAction SilentlyContinue)) {
    Write-Error "Java was not found in PATH. Install Java or add it to PATH before running this script."
    exit 1
}

if (-not (Test-Path -LiteralPath $fitCsvToolPath -PathType Leaf)) {
    Write-Error "FitCSVTool.jar was not found. Checked PATH, then the command-line argument, then the internal fallback path. Current resolved path: $fitCsvToolPath"
    exit 1
}

# Use command-line values directly when they are fully provided; otherwise prompt for missing values.
$sourceInput = Get-InputOrPrompt -Value $SourcePath -Prompt "Enter the source .fit file path (relative or absolute)"
$destInput   = Get-InputOrPrompt -Value $DestinationDirectory -Prompt "Enter the destination directory path (relative or absolute)"
$destName    = Get-InputOrPrompt -Value $DestinationFileName -Prompt "Enter the destination file name (without extension)"

try {
    $sourcePath = Resolve-UserPath -Path $sourceInput -FieldName "Source file path"
    $destDir    = Resolve-UserPath -Path $destInput -FieldName "Destination directory path"
    $destName   = Get-ValidatedDestinationName -Name $destName
}
catch {
    Write-Error $_.Exception.Message
    exit 1
}

# Validate that the source file exists
if (-not (Test-Path -LiteralPath $sourcePath)) {
    Write-Error "Source file not found: $sourcePath"
    exit 1
}

if (-not (Test-Path -LiteralPath $sourcePath -PathType Leaf)) {
    Write-Error "Source path must point to a file: $sourcePath"
    exit 1
}

# Validate that the source file has a .fit extension
if ([System.IO.Path]::GetExtension($sourcePath) -ine ".fit") {
    Write-Error "Source file must have a .fit extension: $sourcePath"
    exit 1
}

# Validate that the destination is a directory path
if (Test-Path -LiteralPath $destDir -PathType Leaf) {
    Write-Error "Destination directory path points to a file, not a directory: $destDir"
    exit 1
}

# Build the full destination file path
$destFile = Join-Path -Path $destDir -ChildPath "$destName.csv"

# Ensure the destination directory exists
if (-not (Test-Path -LiteralPath $destDir)) {
    try {
        New-Item -ItemType Directory -Path $destDir -Force -ErrorAction Stop | Out-Null
    }
    catch {
        Write-Error "Failed to create destination directory '$destDir': $($_.Exception.Message)"
        exit 1
    }
}

# Execute FitCSVTool.jar with the source .fit file as argument
java -jar $fitCsvToolPath $sourcePath

if ($LASTEXITCODE -ne 0) {
    Write-Error "FitCSVTool.jar failed with exit code $LASTEXITCODE. Conversion aborted."
    exit $LASTEXITCODE
}

# The tool writes <basename>.csv alongside the source file
$sourceBaseName = [System.IO.Path]::GetFileNameWithoutExtension($sourcePath)
$sourceDir      = Split-Path -Path $sourcePath -Parent
if ([string]::IsNullOrEmpty($sourceDir)) {
    $sourceDir = "."
}
$generatedCsv = Join-Path -Path $sourceDir -ChildPath "$sourceBaseName.csv"

if (-not (Test-Path -LiteralPath $generatedCsv -PathType Leaf)) {
    Write-Error "Expected generated CSV was not found: $generatedCsv"
    exit 1
}

# Move and rename the generated CSV to the desired destination
try {
    Move-Item -LiteralPath $generatedCsv -Destination $destFile -Force -ErrorAction Stop
}
catch {
    Write-Error "Failed to move generated CSV to '$destFile': $($_.Exception.Message)"
    exit 1
}

Write-Host "Conversion complete. Output saved to: $destFile"
