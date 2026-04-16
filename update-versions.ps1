<#
.SYNOPSIS
    Update tool versions in external-tool-external-tool-versions.json and propagate to all build files.
.DESCRIPTION
    Uses external-tool-external-tool-versions.json as the single source of truth.
    build.ps1 reads external-tool-external-tool-versions.json at runtime automatically.
    Files that cannot load JSON dynamically (main.yml, Dockerfiles, go.mod)
    are patched in-place by this script.

    Run with -Fetch to pull the latest stable releases from upstream.
    Run with explicit version flags to set specific versions.
    Run with no arguments to propagate the current external-tool-external-tool-versions.json to all files.

    NOTE: Docker Compose is intentionally pinned to v2.x.
          v5 is a breaking major version (removes the internal builder).
    NOTE: go-dnsmasq is frozen at 1.0.7 — no upstream release since 2016.
.PARAMETER Fetch
    Fetch latest stable versions from GitHub / go.dev before applying.
.PARAMETER Docker
    Override Docker Engine version (e.g. 28.3.0).
.PARAMETER DockerCompose
    Override Docker Compose version, must be v2.x (e.g. v2.37.2).
.PARAMETER DockerBuildx
    Override Docker Buildx version (e.g. v0.33.0).
.PARAMETER Go
    Override Go version as major.minor (e.g. 1.26).
.EXAMPLE
    # Fetch and apply latest stable versions
    .\update-versions.ps1 -Fetch

    # Apply specific overrides
    .\update-versions.ps1 -Docker 28.4.0 -DockerCompose v2.38.0

    # Re-propagate current external-tool-versions.json to all files (after manual edit)
    .\update-versions.ps1
#>
param(
    [switch]$Fetch,
    [string]$Docker,
    [string]$DockerCompose,
    [string]$DockerBuildx,
    [string]$Go
)

$ErrorActionPreference = "Stop"
$root = $PSScriptRoot
$versionsPath = Join-Path $root "external-tool-versions.json"

function Get-LatestGitHubRelease([string]$repo) {
    $headers = @{ "User-Agent" = "container-desktop-update-versions/1.0" }
    $response = Invoke-RestMethod -Uri "https://api.github.com/repos/$repo/releases/latest" -Headers $headers
    return $response.tag_name
}

function Get-LatestGoMinorVersion {
    $releases = Invoke-RestMethod "https://go.dev/dl/?mode=json"
    $latest = $releases | Where-Object { $_.stable -eq $true } | Select-Object -First 1
    $version = $latest.version -replace '^go', ''
    # Return major.minor only (e.g. "1.26" from "1.26.3")
    return ($version -split '\.')[0..1] -join '.'
}

function Update-File([string]$path, [string]$updated) {
    $rel = $path.Replace($root, '').TrimStart('/\')
    $current = Get-Content $path -Raw
    if ($updated -ne $current) {
        Set-Content $path $updated -NoNewline
        Write-Host "  [updated]    $rel"
    } else {
        Write-Host "  [no change]  $rel"
    }
}

# ── Load current versions ──────────────────────────────────────────────────────
$v = Get-Content $versionsPath | ConvertFrom-Json

Write-Host "Current versions:"
Write-Host "  docker        = $($v.docker)"
Write-Host "  dockerCompose = $($v.dockerCompose)"
Write-Host "  dockerBuildx  = $($v.dockerBuildx)"
Write-Host "  go            = $($v.go)"
Write-Host "  goDnsmasq     = $($v.goDnsmasq)  (frozen — no upstream release since 2016)"
Write-Host ""

# ── Fetch latest from upstream ────────────────────────────────────────────────
if ($Fetch) {
    Write-Host "Fetching latest versions from upstream..."

    $v.docker = (Get-LatestGitHubRelease "moby/moby") -replace '^v', ''
    Write-Host "  docker        = $($v.docker)"

    $latestCompose = Get-LatestGitHubRelease "docker/compose"
    if ($latestCompose -notmatch '^v2\.') {
        Write-Warning "Latest Docker Compose is $latestCompose (not v2.x) — breaking major version, keeping $($v.dockerCompose)"
    } else {
        $v.dockerCompose = $latestCompose
        Write-Host "  dockerCompose = $($v.dockerCompose)"
    }

    $v.dockerBuildx = Get-LatestGitHubRelease "docker/buildx"
    Write-Host "  dockerBuildx  = $($v.dockerBuildx)"

    $v.go = Get-LatestGoMinorVersion
    Write-Host "  go            = $($v.go)"

    Write-Host ""
}

# ── Apply explicit overrides ──────────────────────────────────────────────────
if ($Docker)        { $v.docker        = $Docker        ; Write-Host "Override: docker        = $Docker" }
if ($DockerCompose) { $v.dockerCompose = $DockerCompose ; Write-Host "Override: dockerCompose = $DockerCompose" }
if ($DockerBuildx)  { $v.dockerBuildx  = $DockerBuildx  ; Write-Host "Override: dockerBuildx  = $DockerBuildx" }
if ($Go)            { $v.go            = $Go            ; Write-Host "Override: go            = $Go" }

# ── Save external-tool-versions.json ────────────────────────────────────────────────────────
$v | ConvertTo-Json | Set-Content $versionsPath
Write-Host "Saved external-tool-versions.json"
Write-Host ""

# ── Propagate to files ────────────────────────────────────────────────────────
Write-Host "Propagating to build files..."

# .github/workflows/main.yml — env: block
$mainYml = Get-Content (Join-Path $root ".github/workflows/main.yml") -Raw
$mainYml = $mainYml -replace '(?m)(  DOCKER_VERSION:\s*")[^"]*(")',         "`${1}$($v.docker)`${2}"
$mainYml = $mainYml -replace '(?m)(  DOCKER_COMPOSE_VERSION:\s*")[^"]*(")', "`${1}$($v.dockerCompose)`${2}"
$mainYml = $mainYml -replace '(?m)(  DOCKER_BUILDX_VERSION:\s*")[^"]*(")',  "`${1}$($v.dockerBuildx)`${2}"
$mainYml = $mainYml -replace '(?m)(  GO_VERSION:\s*")[^"]*(")',             "`${1}$($v.go)`${2}"
Update-File (Join-Path $root ".github/workflows/main.yml") $mainYml

# Dockerfile (root) — ARG DOCKER_VERSION default
$df = Get-Content (Join-Path $root "Dockerfile") -Raw
$df = $df -replace '(?m)(ARG DOCKER_VERSION=")[^"]*(")', "`${1}$($v.docker)`${2}"
Update-File (Join-Path $root "Dockerfile") $df

# tools/container-desktop-tools/Dockerfile — ARG DOCKER_VERSION default
$toolsDf = Get-Content (Join-Path $root "tools/container-desktop-tools/Dockerfile") -Raw
$toolsDf = $toolsDf -replace '(?m)(ARG DOCKER_VERSION=")[^"]*(")', "`${1}$($v.docker)`${2}"
Update-File (Join-Path $root "tools/container-desktop-tools/Dockerfile") $toolsDf

# go.mod — go directive
$goMod = Get-Content (Join-Path $root "go.mod") -Raw
$goMod = $goMod -replace '(?m)^(go\s+)\S+', "`${1}$($v.go)"
Update-File (Join-Path $root "go.mod") $goMod

Write-Host ""
Write-Host "Done. build.ps1 reads external-tool-versions.json at runtime — no propagation needed for it."
