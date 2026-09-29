<#
.SYNOPSIS
    Tags and pushes a new release; GitHub Actions then builds and publishes it.

.DESCRIPTION
    The version is never edited in a file: it is the tag. By default the patch number of the
    latest release goes up by one; use -Bump for minor/major or -Version for an exact number.

    Releases are cut from an up-to-date, clean master so the tag matches what is on GitHub.

.EXAMPLE
    ./scripts/New-Release.ps1                    # v1.2.0 -> v1.2.1
    ./scripts/New-Release.ps1 -Bump minor        # v1.2.0 -> v1.3.0
    ./scripts/New-Release.ps1 -Version 2.0.0     # exactly v2.0.0
    ./scripts/New-Release.ps1 -WhatIf            # show what would happen
#>
[CmdletBinding(SupportsShouldProcess)]
param(
    [ValidateSet('patch', 'minor', 'major')]
    [string] $Bump = 'patch',
    [string] $Version,
    # Skip the confirmation prompt.
    [switch] $Yes
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

function Invoke-Git {
    $output = git @args
    if ($LASTEXITCODE -ne 0) { throw "git $($args -join ' ') failed." }
    $output
}

Push-Location (Split-Path $PSScriptRoot -Parent)
try {
    Invoke-Git fetch origin --tags --quiet

    $branch = Invoke-Git rev-parse --abbrev-ref HEAD
    if ($branch -ne 'master') { throw "Releases are made from master (currently on '$branch')." }
    if (Invoke-Git status --porcelain) { throw 'Commit or stash your changes first.' }
    if ((Invoke-Git rev-parse HEAD) -ne (Invoke-Git rev-parse origin/master)) {
        throw 'master is not the same as origin/master. Push or pull first.'
    }

    $next = & (Join-Path $PSScriptRoot 'Get-NextVersion.ps1') -Bump $Bump -Version $Version
    $commit = Invoke-Git log -1 --format='%h %s'
    Write-Host "Release v$next from $commit" -ForegroundColor Cyan

    if (-not $PSCmdlet.ShouldProcess("v$next", 'Create and push tag')) { return }
    if (-not $Yes -and (Read-Host 'Continue? [y/N]') -notmatch '^(y|yes)$') { Write-Host 'Cancelled.'; return }

    Invoke-Git tag -a "v$next" -m "WolfyDesktop $next"
    Invoke-Git push origin "v$next" --quiet

    Write-Host "Pushed v$next. Build progress: https://github.com/vrykolakas166/WolfyDesktop/actions" -ForegroundColor Green
}
finally {
    Pop-Location
}
