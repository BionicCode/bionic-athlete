#requires -Version 7.0

[CmdletBinding()]
param(
    [Parameter()]
    [string]$RepositoryUrl = "https://github.com/garmin/fit-sdk-tools.git",

    [Parameter()]
    [string]$RepositoryFilePath = "Profile.xlsx",

    [Parameter()]
    [string]$ProfilePath = "docs/reference/garmin-fit/Profile.xlsx",

    [Parameter()]
    [string]$MetadataPath = "docs/reference/garmin-fit/Profile.source.json"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Require-Executable {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Name,

        [Parameter(Mandatory = $true)]
        [string]$Hint
    )

    if (-not (Get-Command $Name -ErrorAction SilentlyContinue)) {
        throw "Required executable not found: $Name. $Hint"
    }
}

function Invoke-Git {
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$Arguments
    )

    & git @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "git $($Arguments -join ' ') failed with exit code $LASTEXITCODE."
    }
}

function Get-RepositoryRoot {
    $root = (& git rev-parse --show-toplevel 2>$null)
    if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($root)) {
        throw "Could not determine repository root. Run this script from inside the FitToCsvConverter git repository."
    }

    return $root.Trim()
}

function ConvertTo-RepositoryPath {
    param([Parameter(Mandatory = $true)][string]$Path)

    return $Path.Replace("\", "/")
}

function Resolve-RepoRelativePath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RepoRoot,

        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    if ([System.IO.Path]::IsPathRooted($Path)) {
        return $Path
    }

    return Join-Path $RepoRoot $Path
}

function Get-LatestSemanticVersionTag {
    param([Parameter(Mandatory = $true)][string]$RemoteRepositoryUrl)

    $refs = & git ls-remote --tags --refs $RemoteRepositoryUrl
    if ($LASTEXITCODE -ne 0) {
        throw "git ls-remote failed for '$RemoteRepositoryUrl'."
    }

    if (-not $refs) {
        throw "No tags found for remote repository '$RemoteRepositoryUrl'."
    }

    $tags = foreach ($line in $refs) {
        if ($line -match "^(?<sha>[0-9a-f]{40})\s+refs/tags/(?<tag>\d+\.\d+\.\d+)$") {
            [PSCustomObject]@{
                Tag = $Matches["tag"]
                Version = [version]$Matches["tag"]
                Sha = $Matches["sha"]
            }
        }
    }

    $latest = $tags | Sort-Object -Property Version -Descending | Select-Object -First 1
    if ($null -eq $latest) {
        throw "Could not find a semantic version tag like '21.201.0' in '$RemoteRepositoryUrl'."
    }

    return $latest
}

function Get-Sha256 {
    param([Parameter(Mandatory = $true)][string]$Path)

    return (Get-FileHash -Path $Path -Algorithm SHA256).Hash.ToLowerInvariant()
}

function Test-IsGitLfsPointer {
    param([Parameter(Mandatory = $true)][string]$Path)

    try {
        $firstLine = Get-Content -LiteralPath $Path -TotalCount 1 -Encoding UTF8 -ErrorAction Stop
        return $firstLine -eq "version https://git-lfs.github.com/spec/v1"
    }
    catch {
        return $false
    }
}

function Read-JsonFileOrNull {
    param([Parameter(Mandatory = $true)][string]$Path)

    if (-not (Test-Path -LiteralPath $Path)) {
        return $null
    }

    return Get-Content -LiteralPath $Path -Raw | ConvertFrom-Json
}

function Write-JsonFile {
    param(
        [Parameter(Mandatory = $true)]
        [object]$Value,

        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    $directory = Split-Path -Parent $Path
    if (-not [string]::IsNullOrWhiteSpace($directory)) {
        New-Item -ItemType Directory -Force -Path $directory | Out-Null
    }

    $Value |
        ConvertTo-Json -Depth 20 |
        Set-Content -LiteralPath $Path -Encoding UTF8
}

function Copy-ProfileFromGitLfs {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RemoteRepositoryUrl,

        [Parameter(Mandatory = $true)]
        [string]$Tag,

        [Parameter(Mandatory = $true)]
        [string]$RepositoryFilePath,

        [Parameter(Mandatory = $true)]
        [string]$DestinationPath
    )

    $tempDirectory = Join-Path ([System.IO.Path]::GetTempPath()) ("fit-profile-" + [System.Guid]::NewGuid().ToString("N"))

    try {
        New-Item -ItemType Directory -Force -Path $tempDirectory | Out-Null

        Write-Host "Cloning Garmin fit-sdk-tools tag $Tag with Git LFS support..."
        Invoke-Git -Arguments @(
            "clone",
            "--depth", "1",
            "--branch", $Tag,
            "--filter=blob:none",
            "--no-checkout",
            $RemoteRepositoryUrl,
            $tempDirectory
        )

        Invoke-Git -Arguments @("-C", $tempDirectory, "lfs", "install", "--local")

        # Fetch only the target file into the working tree.
        Invoke-Git -Arguments @("-C", $tempDirectory, "sparse-checkout", "init", "--cone")
        Invoke-Git -Arguments @("-C", $tempDirectory, "sparse-checkout", "set", $RepositoryFilePath)
        Invoke-Git -Arguments @("-C", $tempDirectory, "checkout", $Tag)

        # Ensure the LFS object is materialized, not left as a pointer.
        Invoke-Git -Arguments @("-C", $tempDirectory, "lfs", "pull", "--include=$RepositoryFilePath", "--exclude=")

        $sourcePath = Join-Path $tempDirectory $RepositoryFilePath
        if (-not (Test-Path -LiteralPath $sourcePath)) {
            throw "Expected '$RepositoryFilePath' was not found after clone/LFS pull."
        }

        if (Test-IsGitLfsPointer -Path $sourcePath) {
            throw "The checked-out '$RepositoryFilePath' is still a Git LFS pointer after 'git lfs pull'. Check Git LFS installation/auth/network access."
        }

        $destinationDirectory = Split-Path -Parent $DestinationPath
        New-Item -ItemType Directory -Force -Path $destinationDirectory | Out-Null
        Copy-Item -LiteralPath $sourcePath -Destination $DestinationPath -Force
    }
    finally {
        if (Test-Path -LiteralPath $tempDirectory) {
            Remove-Item -LiteralPath $tempDirectory -Recurse -Force
        }
    }
}

Require-Executable -Name "git" -Hint "Install Git for Windows."
Require-Executable -Name "git-lfs" -Hint "Install Git LFS. On Windows, Git for Windows may include it; otherwise install from https://git-lfs.com/."

# Also validates that 'git lfs' is usable.
& git lfs version | Out-Host
if ($LASTEXITCODE -ne 0) {
    throw "'git lfs version' failed. Git LFS is required because Profile.xlsx is stored through Git LFS."
}

$repoRoot = Get-RepositoryRoot
$resolvedProfilePath = Resolve-RepoRelativePath -RepoRoot $repoRoot -Path $ProfilePath
$resolvedMetadataPath = Resolve-RepoRelativePath -RepoRoot $repoRoot -Path $MetadataPath

$latest = Get-LatestSemanticVersionTag -RemoteRepositoryUrl $RepositoryUrl
$latestTag = $latest.Tag
$latestTagSha = $latest.Sha

$sourceUrl = "https://github.com/garmin/fit-sdk-tools/blob/$latestTag/$RepositoryFilePath"
$rawUrl = "git-lfs:$RepositoryUrl@$latestTag/$RepositoryFilePath"

Write-Host "Latest Garmin FIT Profile tag: $latestTag"

$downloadPath = Join-Path ([System.IO.Path]::GetTempPath()) ("Profile-" + [System.Guid]::NewGuid().ToString("N") + ".xlsx")

try {
    Copy-ProfileFromGitLfs `
        -RemoteRepositoryUrl $RepositoryUrl `
        -Tag $latestTag `
        -RepositoryFilePath $RepositoryFilePath `
        -DestinationPath $downloadPath

    $downloadedSha256 = Get-Sha256 -Path $downloadPath
    $currentSha256 = $null

    if (Test-Path -LiteralPath $resolvedProfilePath) {
        $currentSha256 = Get-Sha256 -Path $resolvedProfilePath
    }

    $existingMetadata = Read-JsonFileOrNull -Path $resolvedMetadataPath

    $existingTag = $null
    if ($null -ne $existingMetadata -and $null -ne $existingMetadata.upstream) {
        $existingTag = $existingMetadata.upstream.tag
    }

    $shouldUpdateProfile = ($currentSha256 -ne $downloadedSha256)
    $shouldUpdateMetadata = ($existingTag -ne $latestTag)

    if (-not $shouldUpdateProfile -and -not $shouldUpdateMetadata) {
        Write-Host "Profile.xlsx is already up to date at tag $latestTag."
        return
    }

    if ($shouldUpdateProfile) {
        New-Item -ItemType Directory -Force -Path (Split-Path -Parent $resolvedProfilePath) | Out-Null
        Copy-Item -LiteralPath $downloadPath -Destination $resolvedProfilePath -Force
        Write-Host "Updated $ProfilePath"
    }
    else {
        Write-Host "Profile.xlsx content hash is unchanged; metadata will still be refreshed because the upstream tag changed."
    }

    $previous = [ordered]@{}
    if ($null -ne $existingMetadata) {
        $previous = [ordered]@{
            tag = $existingMetadata.upstream.tag
            sha256 = $existingMetadata.hashes.sha256
            retrieved = $existingMetadata.retrieved
        }
    }

    $metadata = [ordered]@{
        schemaVersion = 1
        artifactName = "Garmin FIT Profile.xlsx"
        purpose = "Reference copy of Garmin's public standard FIT Profile workbook used to validate documented standard FIT messages, fields, types, units, scale and offset metadata."
        localPath = (ConvertTo-RepositoryPath -Path $ProfilePath)
        upstream = [ordered]@{
            repositoryUrl = $RepositoryUrl
            filePath = $RepositoryFilePath
            tag = $latestTag
            ref = "refs/tags/$latestTag"
            tagRefSha = $latestTagSha
            sourceUrl = $sourceUrl
            rawUrl = $rawUrl
            changeId = $null
        }
        retrieved = [ordered]@{
            date = (Get-Date -Format "yyyy-MM-dd")
            timestampUtc = (Get-Date).ToUniversalTime().ToString("o")
            by = "github-actions-or-local-script"
            notes = "Updated from latest semantic version tag in Garmin fit-sdk-tools using Git LFS."
        }
        hashes = [ordered]@{
            sha256 = $downloadedSha256
        }
        previous = $previous
        validationRole = [ordered]@{
            standardFitProfileSourceOfTruth = $true
            specificActivityPresenceSourceOfTruth = $false
            developerFieldsMayExistOutsideProfile = $true
            unknownVendorFieldsMayExistOutsideProfile = $true
        }
    }

    Write-JsonFile -Value $metadata -Path $resolvedMetadataPath
    Write-Host "Updated $MetadataPath"
}
finally {
    if (Test-Path -LiteralPath $downloadPath) {
        Remove-Item -LiteralPath $downloadPath -Force
    }
}