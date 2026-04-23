param(
    [string]$Project = ".\AutoTool.Plugin.Template\AutoTool.Plugin.Template.csproj",
    [string]$Configuration = "Release",
    [string]$Destination = ".\.tmp\Template.Plugin",
    [switch]$SkipBuild
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

$root = (Get-Location).Path
$projectPath = Resolve-AbsolutePath -Path $Project -BaseDirectory $root
$destinationPath = Resolve-AbsolutePath -Path $Destination -BaseDirectory $root
$templateDirectoryPath = Resolve-AbsolutePath -Path '.\PluginTemplates\Template.Plugin' -BaseDirectory $root

if (-not (Test-Path -LiteralPath $projectPath)) {
    throw "テンプレート プラグイン プロジェクトが見つかりません: $projectPath"
}

if (-not (Test-Path -LiteralPath $templateDirectoryPath)) {
    throw "テンプレート プラグイン定義が見つかりません: $templateDirectoryPath"
}

if (-not $SkipBuild) {
    dotnet build $projectPath -c $Configuration --no-restore -v minimal
    if ($LASTEXITCODE -ne 0) {
        throw "テンプレート プラグインのビルドが失敗しました。終了コード: $LASTEXITCODE"
    }
}

$outputDirectoryPath = Join-Path ([System.IO.Path]::GetDirectoryName($projectPath)) "bin\$Configuration\net10.0-windows"
$pluginAssemblyPath = Join-Path $outputDirectoryPath 'AutoTool.Plugin.Template.dll'
$pluginManifestPath = Join-Path $templateDirectoryPath 'plugin.json'
$pluginReadmePath = Join-Path $templateDirectoryPath 'README.md'

if (-not (Test-Path -LiteralPath $pluginAssemblyPath)) {
    throw "テンプレート プラグイン DLL が見つかりません: $pluginAssemblyPath"
}

New-Item -ItemType Directory -Force -Path $destinationPath | Out-Null
Copy-Item -LiteralPath $pluginManifestPath -Destination (Join-Path $destinationPath 'plugin.json') -Force
Copy-Item -LiteralPath $pluginReadmePath -Destination (Join-Path $destinationPath 'README.md') -Force
Copy-Item -LiteralPath $pluginAssemblyPath -Destination (Join-Path $destinationPath 'AutoTool.Plugin.Template.dll') -Force

Write-Host "テンプレート プラグインを配置しました: $destinationPath"
