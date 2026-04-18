param(
    [string]$Source = ".deploy\AutoTool_publish",
    [string]$Destination = "C:\AutoTool"
)

$ErrorActionPreference = "Stop"

$root = Get-Location
$sourcePath = if ([System.IO.Path]::IsPathRooted($Source)) { $Source } else { Join-Path $root $Source }

if (-not (Test-Path -LiteralPath $sourcePath)) {
    throw "Publish output not found: $sourcePath"
}

if (-not (Test-Path -LiteralPath $Destination)) {
    New-Item -ItemType Directory -Path $Destination | Out-Null
}

# Mirror deploy artifacts but preserve user data/config folders.
robocopy $sourcePath $Destination /MIR /XD Macro Settings /R:2 /W:1 /NFL /NDL /NP /NJH /NJS
$code = $LASTEXITCODE
if ($code -gt 7) {
    throw "robocopy failed with exit code $code"
}

$destinationSettingsDir = Join-Path $Destination "Settings"
if (-not (Test-Path -LiteralPath $destinationSettingsDir)) {
    New-Item -ItemType Directory -Path $destinationSettingsDir | Out-Null
}

$destinationSettingsFile = Join-Path $destinationSettingsDir "appsettings.json"
$sourceSettingsFile = Join-Path $sourcePath "Settings\\appsettings.json"
if (-not (Test-Path -LiteralPath $destinationSettingsFile) -and (Test-Path -LiteralPath $sourceSettingsFile)) {
    Copy-Item -LiteralPath $sourceSettingsFile -Destination $destinationSettingsFile -Force
}

Write-Host "Deploy completed to $Destination (Macro/Settings preserved). robocopy exit=$code"
