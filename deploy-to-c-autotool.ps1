param(
    [string]$Source = ".deploy\AutoTool_publish",
    [string]$Destination = "C:\AutoTool",
    [string]$Project = ".\AutoTool.Bootstrap\AutoTool.Bootstrap.csproj",
    [string]$Configuration = "Release",
    [switch]$SkipPublish
)

$ErrorActionPreference = "Stop"

function Resolve-AbsolutePath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,
        [Parameter(Mandatory = $true)]
        [string]$BaseDirectory
    )

    if ([System.IO.Path]::IsPathRooted($Path)) {
        return $Path
    }

    return Join-Path $BaseDirectory $Path
}

function Invoke-Publish {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ProjectPath,
        [Parameter(Mandatory = $true)]
        [string]$Configuration,
        [Parameter(Mandatory = $true)]
        [string]$OutputPath,
        [string]$PublishVersion
    )

    Write-Host "publish を実行します: project=$ProjectPath, configuration=$Configuration, output=$OutputPath"
    $arguments = @("publish", $ProjectPath, "-c", $Configuration, "-o", $OutputPath)
    if (-not [string]::IsNullOrWhiteSpace($PublishVersion)) {
        $arguments += "/p:Version=$PublishVersion"
        $arguments += "/p:FileVersion=$PublishVersion"
        $arguments += "/p:AssemblyVersion=$PublishVersion"
    }

    dotnet @arguments
    if ($LASTEXITCODE -ne 0) {
        throw "publish が失敗しました。終了コード: $LASTEXITCODE"
    }
}

function Get-LatestGitTagVersion {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RepositoryRoot
    )

    $tag = git -C $RepositoryRoot tag --list "v[0-9]*.[0-9]*.[0-9]*" --sort=-v:refname | Select-Object -First 1
    if ([string]::IsNullOrWhiteSpace($tag)) {
        throw "Git タグ（vMAJOR.MINOR.PATCH）が見つかりません。GitHub を正とするため、先にタグを作成してください。"
    }

    if ($tag -notmatch '^v(\d+)\.(\d+)\.(\d+)$') {
        throw "タグ形式が不正です。vMAJOR.MINOR.PATCH 形式が必要です。検出値: $tag"
    }

    return "$($Matches[1]).$($Matches[2]).$($Matches[3])"
}

function Get-FileHashSafe {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    return (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash
}

function Ensure-FileHashMatched {
    param(
        [Parameter(Mandatory = $true)]
        [string]$SourceFile,
        [Parameter(Mandatory = $true)]
        [string]$DestinationFile
    )

    if (-not (Test-Path -LiteralPath $DestinationFile)) {
        throw "配置先ファイルが見つかりません: $DestinationFile"
    }

    $sourceHash = Get-FileHashSafe -Path $SourceFile
    $destinationHash = Get-FileHashSafe -Path $DestinationFile
    if ($sourceHash -eq $destinationHash) {
        return
    }

    # robocopy 後も差分が残る場合の保険。ロック中だとここも失敗する。
    Copy-Item -LiteralPath $SourceFile -Destination $DestinationFile -Force

    $destinationHash = Get-FileHashSafe -Path $DestinationFile
    if ($sourceHash -ne $destinationHash) {
        throw "配置後ハッシュ不一致: $DestinationFile。実行中プロセスのロックを確認してください。"
    }
}

$root = (Get-Location).Path
$sourcePath = Resolve-AbsolutePath -Path $Source -BaseDirectory $root
$projectPath = Resolve-AbsolutePath -Path $Project -BaseDirectory $root

if (-not $SkipPublish) {
    if (-not (Test-Path -LiteralPath $projectPath)) {
        throw "publish 対象プロジェクトが見つかりません: $projectPath"
    }

    if (-not (Test-Path -LiteralPath $sourcePath)) {
        New-Item -ItemType Directory -Path $sourcePath -Force | Out-Null
    }

    $publishVersion = Get-LatestGitTagVersion -RepositoryRoot $root
    Write-Host "GitHub基準バージョンを適用します: $publishVersion"
    Invoke-Publish -ProjectPath $projectPath -Configuration $Configuration -OutputPath $sourcePath -PublishVersion $publishVersion
}

if (-not (Test-Path -LiteralPath $sourcePath)) {
    throw "発行済み成果物が見つかりません: $sourcePath"
}
$sourcePath = (Resolve-Path -LiteralPath $sourcePath).Path

if (-not (Test-Path -LiteralPath $Destination)) {
    New-Item -ItemType Directory -Path $Destination | Out-Null
}
$destinationPath = (Resolve-Path -LiteralPath $Destination).Path

# Destructive sync (/MIR) の前に、入力成果物の最低限を検証する。
$sourceMainExe = Get-ChildItem -LiteralPath $sourcePath -File -Filter *.exe | Where-Object { $_.Name -eq 'AutoTool.exe' } | Select-Object -First 1
if ($null -eq $sourceMainExe) {
    $sourceMainExe = Get-ChildItem -LiteralPath $sourcePath -File -Filter *.exe | Select-Object -First 1
}
if ($null -eq $sourceMainExe) {
    throw "ソースに実行ファイルが見つかりません: $sourcePath"
}

$macroDir = Join-Path $destinationPath "Macro"
$userDataCountBefore = if (Test-Path -LiteralPath $macroDir) {
    (Get-ChildItem -LiteralPath $macroDir -Recurse -File | Measure-Object).Count
}
else {
    0
}

# Mirror deploy artifacts but preserve user data/config folders.
robocopy $sourcePath $destinationPath /MIR /XD Macro Settings x86 tessdata /R:2 /W:1 /NFL /NDL /NP /NJH /NJS
$code = $LASTEXITCODE
if ($code -gt 7) {
    throw "robocopy が失敗しました。終了コード: $code"
}

$destinationSettingsDir = Join-Path $destinationPath "Settings"
if (-not (Test-Path -LiteralPath $destinationSettingsDir)) {
    New-Item -ItemType Directory -Path $destinationSettingsDir | Out-Null
}

$destinationSettingsFile = Join-Path $destinationSettingsDir "appsettings.json"
$sourceSettingsFile = Join-Path $sourcePath "Settings\appsettings.json"
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
    throw "デプロイ後にメイン実行ファイルが見つかりません: $destinationMainExe"
}

if (-not (Test-Path -LiteralPath $destinationSettingsFile)) {
    throw "デプロイ後に設定ファイルが見つかりません: $destinationSettingsFile"
}

# Ensure critical binaries are really updated.
$criticalFiles = @($sourceMainExe.Name, 'AutoTool.dll', 'AutoTool.Desktop.dll')
foreach ($name in $criticalFiles) {
    $sourceFile = Join-Path $sourcePath $name
    if (-not (Test-Path -LiteralPath $sourceFile)) {
        continue
    }

    $destinationFile = Join-Path $destinationPath $name
    Ensure-FileHashMatched -SourceFile $sourceFile -DestinationFile $destinationFile
}

$userDataCountAfter = if (Test-Path -LiteralPath $macroDir) {
    (Get-ChildItem -LiteralPath $macroDir -Recurse -File | Measure-Object).Count
}
else {
    0
}

Write-Host "デプロイが完了しました: $destinationPath（Macro/Settings 保護、robocopy 終了コード=$code）"
Write-Host "確認結果: exe=$destinationMainExe, settings=$destinationSettingsFile, userData(before=$userDataCountBefore, after=$userDataCountAfter)"
