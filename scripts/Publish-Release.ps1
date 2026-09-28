<#
.SYNOPSIS
    Builds WolfyDesktop as Native AOT and packages it with Velopack.

.DESCRIPTION
    Produces, in artifacts/releases:
      WolfyInc.WolfyDesktop-win-Setup.exe      installer for new users
      WolfyInc.WolfyDesktop-<ver>-full.nupkg   full update package
      WolfyInc.WolfyDesktop-<ver>-delta.nupkg  delta from the previous GitHub release (when one exists)
      releases.win.json                        feed read by the in-app updater

    With -Upload, publishes them as a GitHub release, which is where installed copies look for updates.

.EXAMPLE
    ./scripts/Publish-Release.ps1                     # package the version in Directory.Build.props
    ./scripts/Publish-Release.ps1 -Version 1.2.1 -Upload -Token $env:GITHUB_TOKEN
#>
[CmdletBinding()]
param(
    [string] $Version,
    [switch] $Upload,
    [string] $Token = $env:GITHUB_TOKEN,
    [switch] $SkipTests
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$root = Split-Path $PSScriptRoot -Parent
$repoUrl = 'https://github.com/vrykolakas166/WolfyDesktop'
$packId = 'WolfyInc.WolfyDesktop'   # Also names the install folder, so it must differ from the data folder "WolfyDesktop".
$publishDir = Join-Path $root 'artifacts/publish'
$releaseDir = Join-Path $root 'artifacts/releases'

function Invoke-Checked([string] $what, [scriptblock] $command) {
    Write-Host "==> $what" -ForegroundColor Cyan
    & $command
    if ($LASTEXITCODE -ne 0) { throw "$what failed (exit code $LASTEXITCODE)." }
}

if (-not $Version) {
    $Version = ([xml](Get-Content (Join-Path $root 'Directory.Build.props'))).Project.PropertyGroup.Version | Select-Object -First 1
}
$Version = $Version.TrimStart('v')   # accept tag names like v1.2.1
if ($Version -notmatch '^\d+\.\d+\.\d+(-[0-9A-Za-z.-]+)?$') { throw "Version '$Version' is not SemVer (e.g. 1.2.0)." }
if ($Upload -and -not $Token) { throw 'Uploading needs a GitHub token: pass -Token or set GITHUB_TOKEN.' }

# Native AOT locates the C++ linker through vswhere, which is not on PATH outside a VS developer shell.
$vsInstaller = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer'
if (-not (Get-Command vswhere -ErrorAction SilentlyContinue) -and (Test-Path $vsInstaller)) {
    $env:PATH = "$env:PATH;$vsInstaller"
}

Push-Location $root
try {
    Invoke-Checked 'Restore tools' { dotnet tool restore }

    if (-not $SkipTests) {
        Invoke-Checked 'Run tests' { dotnet test WolfyDesktop.Tests -c Release --nologo }
    }

    Remove-Item $publishDir -Recurse -Force -ErrorAction SilentlyContinue
    Invoke-Checked "Publish $Version (Native AOT)" {
        dotnet publish WolfyDesktop/WolfyDesktop.csproj -c Release -r win-x64 -p:Platform=x64 `
            -p:Version=$Version -o $publishDir --nologo
    }

    # Deltas are built against the latest published release; the very first Velopack release has none.
    New-Item $releaseDir -ItemType Directory -Force | Out-Null
    Write-Host '==> Fetch previous release for delta updates' -ForegroundColor Cyan
    $downloadArgs = @('vpk', 'download', 'github', '--repoUrl', $repoUrl, '-o', $releaseDir)
    if ($Token) { $downloadArgs += @('--token', $Token) }
    dotnet @downloadArgs
    if ($LASTEXITCODE -ne 0) { Write-Warning 'No previous Velopack release found; packaging without a delta.' }

    Invoke-Checked 'Package with Velopack' {
        dotnet vpk pack `
            --packId $packId `
            --packVersion $Version `
            --packDir $publishDir `
            --mainExe WolfyDesktop.exe `
            --packTitle 'Wolfy Desktop' `
            --packAuthors 'Phuc Pham Hong' `
            --icon WolfyDesktop/logo.ico `
            --exclude '.*\.pdb' `
            --outputDir $releaseDir
    }

    if ($Upload) {
        Invoke-Checked 'Upload GitHub release' {
            dotnet vpk upload github --repoUrl $repoUrl --token $Token -o $releaseDir `
                --publish --tag "v$Version" --releaseName "WolfyDesktop $Version"
        }
    }

    Write-Host "`nDone. Installer: $(Join-Path $releaseDir "$packId-win-Setup.exe")" -ForegroundColor Green
}
finally {
    Pop-Location
}
