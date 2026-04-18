param(
    [string]$Source = ".deploy\AutoTool_publish",
    [string]$Destination = "C:\AutoTool"
)

$ErrorActionPreference = "Stop"

$root = Get-Location
$sourcePath = if ([System.IO.Path]::IsPathRooted($Source)) { $Source } else { Join-Path $root $Source }
$sourcePath = (Resolve-Path -LiteralPath $sourcePath).Path

if (-not (Test-Path -LiteralPath $sourcePath)) {
    throw "Publish output not found: $sourcePath"
}

if (-not (Test-Path -LiteralPath $Destination)) {
    New-Item -ItemType Directory -Path $Destination | Out-Null
}
$destinationPath = (Resolve-Path -LiteralPath $Destination).Path

# Destructive sync (/MIR) の前に、入力成果物の最低限を検証する。
$sourceMainExe = Get-ChildItem -LiteralPath $sourcePath -File -Filter *.exe | Select-Object -First 1
if ($null -eq $sourceMainExe) {
    throw "No main executable was found in source: $sourcePath"
}

$macroDir = Join-Path $destinationPath "Macro"
$userDataCountBefore = if (Test-Path -LiteralPath $macroDir) {
    (Get-ChildItem -LiteralPath $macroDir -Recurse -File | Measure-Object).Count
}
else {
    0
}

# Mirror deploy artifacts but preserve user data/config folders.
robocopy $sourcePath $destinationPath /MIR /XD Macro Settings x86 /R:2 /W:1 /NFL /NDL /NP /NJH /NJS
$code = $LASTEXITCODE
if ($code -gt 7) {
    throw "robocopy failed with exit code $code"
}

$destinationSettingsDir = Join-Path $destinationPath "Settings"
if (-not (Test-Path -LiteralPath $destinationSettingsDir)) {
    New-Item -ItemType Directory -Path $destinationSettingsDir | Out-Null
}

$destinationSettingsFile = Join-Path $destinationSettingsDir "appsettings.json"
$sourceSettingsFile = Join-Path $sourcePath "Settings\\appsettings.json"
if (-not (Test-Path -LiteralPath $destinationSettingsFile) -and (Test-Path -LiteralPath $sourceSettingsFile)) {
    Copy-Item -LiteralPath $sourceSettingsFile -Destination $destinationSettingsFile -Force
}

# Remove unsupported deployment artifacts.
Get-ChildItem -LiteralPath $destinationPath -Recurse -File |
    Where-Object { $_.Extension -in ".pdb", ".lib" } |
    ForEach-Object {
    Remove-Item -LiteralPath $_.FullName -Force
}

$x86Dir = Join-Path $destinationPath "x86"
if (Test-Path -LiteralPath $x86Dir) {
    Remove-Item -LiteralPath $x86Dir -Recurse -Force
}

# Remove empty locale directories (example: cs, de, fr, ja-JP).
$localePattern = '^[a-z]{2}(-[A-Z]{2})?$'
Get-ChildItem -LiteralPath $destinationPath -Recurse -Directory |
    Sort-Object FullName -Descending |
    Where-Object { $_.Name -match $localePattern } |
    ForEach-Object {
        if (-not (Get-ChildItem -LiteralPath $_.FullName -Force)) {
            Remove-Item -LiteralPath $_.FullName -Force
        }
    }

# Minimum post-deploy verification.
$destinationMainExe = Join-Path $destinationPath $sourceMainExe.Name
if (-not (Test-Path -LiteralPath $destinationMainExe)) {
    throw "Main executable is missing after deploy: $destinationMainExe"
}

if (-not (Test-Path -LiteralPath $destinationSettingsFile)) {
    throw "Settings file is missing after deploy: $destinationSettingsFile"
}

$userDataCountAfter = if (Test-Path -LiteralPath $macroDir) {
    (Get-ChildItem -LiteralPath $macroDir -Recurse -File | Measure-Object).Count
}
else {
    0
}

Write-Host "Deploy completed to $destinationPath (Macro/Settings preserved). robocopy exit=$code"
Write-Host "Verified: exe=$destinationMainExe, settings=$destinationSettingsFile, userData(before=$userDataCountBefore, after=$userDataCountAfter)"
