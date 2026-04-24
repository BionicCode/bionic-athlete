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

function ConvertTo-RepositoryPath {
    param([Parameter(Mandatory = $true)][string]$Path)

    return $Path.Replace("\", "/")
}

function Get-LatestSemanticVersionTag {
    param([Parameter(Mandatory = $true)][string]$RemoteRepositoryUrl)

    $refs = git ls-remote --tags --refs $RemoteRepositoryUrl
    if (-not $refs) {
        throw "No tags found for remote repository '$RemoteRepositoryUrl'."
    }

    $tags = foreach ($line in $refs) {
        if ($line -match "refs/tags/(?<tag>\d+\.\d+\.\d+)$") {
            [PSCustomObject]@{
                Tag = $Matches["tag"]
                Version = [version]$Matches["tag"]
                Sha = ($line -split "\s+")[0]
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
        $firstLine = Get-Content -Path $Path -TotalCount 1 -Encoding UTF8 -ErrorAction Stop
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

    $Value
        | ConvertTo-Json -Depth 20
        | Set-Content -LiteralPath $Path -Encoding UTF8
}

$latest = Get-LatestSemanticVersionTag -RemoteRepositoryUrl $RepositoryUrl
$latestTag = $latest.Tag
$latestTagSha = $latest.Sha

$rawUrl = "https://raw.githubusercontent.com/garmin/fit-sdk-tools/$latestTag/$RepositoryFilePath"
$sourceUrl = "https://github.com/garmin/fit-sdk-tools/blob/$latestTag/$RepositoryFilePath"

$tempDirectory = Join-Path ([System.IO.Path]::GetTempPath()) ("fit-profile-" + [System.Guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Force -Path $tempDirectory | Out-Null

try {
    $downloadPath = Join-Path $tempDirectory "Profile.xlsx"

    Write-Host "Latest Garmin FIT Profile tag: $latestTag"
    Write-Host "Downloading $rawUrl"

    Invoke-WebRequest -Uri $rawUrl -OutFile $downloadPath -MaximumRedirection 10

    if (Test-IsGitLfsPointer -Path $downloadPath) {
        throw "Downloaded file appears to be a Git LFS pointer, not the actual Profile.xlsx content. Use a git-lfs based fetch path instead."
    }

    $downloadedSha256 = Get-Sha256 -Path $downloadPath
    $currentSha256 = $null
    if (Test-Path -LiteralPath $ProfilePath) {
        $currentSha256 = Get-Sha256 -Path $ProfilePath
    }

    $existingMetadata = Read-JsonFileOrNull -Path $MetadataPath
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

    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $ProfilePath) | Out-Null

    if ($shouldUpdateProfile) {
        Copy-Item -LiteralPath $downloadPath -Destination $ProfilePath -Force
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
            notes = "Updated from latest semantic version tag in Garmin fit-sdk-tools."
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

    Write-JsonFile -Value $metadata -Path $MetadataPath
    Write-Host "Updated $MetadataPath"
}
finally {
    if (Test-Path -LiteralPath $tempDirectory) {
        Remove-Item -LiteralPath $tempDirectory -Recurse -Force
    }
}