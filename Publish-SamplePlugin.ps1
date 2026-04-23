param(
    [string]$Project = ".\AutoTool.Tests.Plugin.Sample\AutoTool.Tests.Plugin.Sample.csproj",
    [string]$Configuration = "Release",
    [string]$Destination = ".\.deploy\AutoTool_publish\Plugins\Sample.Plugin",
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
$sampleDirectoryPath = Resolve-AbsolutePath -Path '.\PluginSamples\Sample.Plugin' -BaseDirectory $root

if (-not (Test-Path -LiteralPath $projectPath)) {
    throw "サンプルプラグイン プロジェクトが見つかりません: $projectPath"
}

if (-not (Test-Path -LiteralPath $sampleDirectoryPath)) {
    throw "サンプルプラグイン定義が見つかりません: $sampleDirectoryPath"
}

if (-not $SkipBuild) {
    dotnet build $projectPath -c $Configuration --no-restore -v minimal
    if ($LASTEXITCODE -ne 0) {
        throw "サンプルプラグインのビルドが失敗しました。終了コード: $LASTEXITCODE"
    }
}

$outputDirectoryPath = Join-Path ([System.IO.Path]::GetDirectoryName($projectPath)) "bin\$Configuration\net10.0-windows"
$pluginAssemblyPath = Join-Path $outputDirectoryPath 'AutoTool.Tests.Plugin.Sample.dll'
$pluginManifestPath = Join-Path $sampleDirectoryPath 'plugin.json'

if (-not (Test-Path -LiteralPath $pluginAssemblyPath)) {
    throw "サンプルプラグイン DLL が見つかりません: $pluginAssemblyPath"
}

New-Item -ItemType Directory -Force -Path $destinationPath | Out-Null
Copy-Item -LiteralPath $pluginManifestPath -Destination (Join-Path $destinationPath 'plugin.json') -Force
Copy-Item -LiteralPath $pluginAssemblyPath -Destination (Join-Path $destinationPath 'AutoTool.Tests.Plugin.Sample.dll') -Force

Write-Host "サンプルプラグインを配置しました: $destinationPath"
