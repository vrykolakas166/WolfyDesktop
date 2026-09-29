<#
.SYNOPSIS
    Works out the next release version from the latest vX.Y.Z git tag.

.DESCRIPTION
    Returns the version string (without the "v"). With -Version, checks that exact version
    instead (manual bump). Fails if the result is not newer than the latest release or its
    tag already exists. Used by New-Release.ps1 and the Release workflow.

.EXAMPLE
    ./scripts/Get-NextVersion.ps1                    # v1.2.0 -> 1.2.1
    ./scripts/Get-NextVersion.ps1 -Bump minor        # v1.2.0 -> 1.3.0
    ./scripts/Get-NextVersion.ps1 -Version 2.0.0     # exactly 2.0.0
#>
[CmdletBinding()]
param(
    [ValidateSet('patch', 'minor', 'major')]
    [string] $Bump = 'patch',
    [string] $Version
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$semVer = '^(\d+)\.(\d+)\.(\d+)(-[0-9A-Za-z.-]+)?$'

# Stable releases only; pre-release tags such as v1.3.0-beta.1 never become the bump base.
$latestTag = git tag --list 'v*' --sort=-v:refname | Where-Object { $_ -match '^v\d+\.\d+\.\d+$' } | Select-Object -First 1
$latest = if ($latestTag) { [version]$latestTag.TrimStart('v') } else { [version]'0.0.0' }

if ($Version) {
    $next = $Version.Trim().TrimStart('v')
    if ($next -notmatch $semVer) { throw "Version '$next' is not SemVer (e.g. 1.3.0 or 1.3.0-beta.1)." }
    $core = [version]"$($Matches[1]).$($Matches[2]).$($Matches[3])"
    if ($core -le $latest) { throw "Version $next must be newer than the latest release ($latest)." }
}
else {
    $next = switch ($Bump) {
        'major' { "$($latest.Major + 1).0.0" }
        'minor' { "$($latest.Major).$($latest.Minor + 1).0" }
        'patch' { "$($latest.Major).$($latest.Minor).$($latest.Build + 1)" }
    }
}

if (git tag --list "v$next") { throw "Tag v$next already exists." }

Write-Verbose "Latest release: $latest; next: $next"
$next
