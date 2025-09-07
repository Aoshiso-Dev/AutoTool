using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AutoTool.Command.Base;
using AutoTool.Command.Interface;
using AutoTool.Services.ImageProcessing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using AutoTool.Command.Definition;

namespace AutoTool.Command.Commands
{
    // インターフェース定義
    public interface IExecuteCommand : AutoTool.Command.Interface.ICommand 
    {
        string ProgramPath { get; set; }
        string Arguments { get; set; }
        string WorkingDirectory { get; set; }
    }

    public interface IScreenshotCommand : AutoTool.Command.Interface.ICommand 
    {
        string SaveDirectory { get; set; }
        string WindowTitle { get; set; }
        string WindowClassName { get; set; }
    }

    // 実装クラス
    /// <summary>
    /// プログラム実行コマンド（DI対応）
    /// </summary>
    [DirectCommand(DirectCommandRegistry.CommandTypes.Execute, "プログラム実行", "System", "外部プログラムを実行します")]
    public class ExecuteCommand : BaseCommand, IExecuteCommand
    {
        [SettingProperty("プログラムパス", SettingControlType.FilePicker,
            description: "実行するプログラムのパス",
            category: "基本設定",
            isRequired: true,
            fileFilter: "実行ファイル (*.exe;*.bat;*.cmd)|*.exe;*.bat;*.cmd|すべてのファイル (*.*)|*.*")]
        public string ProgramPath { get; set; } = string.Empty;

        [SettingProperty("引数", SettingControlType.TextBox,
            description: "プログラムに渡す引数",
            category: "基本設定")]
        public string Arguments { get; set; } = string.Empty;

        [SettingProperty("作業ディレクトリ", SettingControlType.FolderPicker,
            description: "プログラムの作業ディレクトリ",
            category: "詳細設定")]
        public string WorkingDirectory { get; set; } = string.Empty;

        public ExecuteCommand(AutoTool.Command.Interface.ICommand? parent = null, IServiceProvider? serviceProvider = null)
            : base(parent, serviceProvider)
        {
            Description = "プログラム実行";
        }

        protected override void ValidateSettings()
        {
            if (string.IsNullOrWhiteSpace(ProgramPath)) throw new ArgumentException("実行するプログラムのパスを指定してください。");
        }

        protected override void ValidateFiles()
        {
            if (!string.IsNullOrEmpty(ProgramPath))
            {
                ValidateFileExists(ProgramPath, "実行ファイル");
            }
            if (!string.IsNullOrEmpty(WorkingDirectory))
            {
                ValidateDirectoryExists(WorkingDirectory, "ワーキングディレクトリ");
            }
        }

        protected override async Task<bool> DoExecuteAsync(CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(ProgramPath)) return false;

            try
            {
                // 相対パスを解決して実際の実行に使用
                var resolvedProgramPath = ResolvePath(ProgramPath);
                var resolvedWorkingDirectory = !string.IsNullOrEmpty(WorkingDirectory) ? ResolvePath(WorkingDirectory) : string.Empty;
                
                _logger?.LogDebug("[DoExecuteAsync] Execute 解決されたProgramPath: {OriginalPath} -> {ResolvedPath}", ProgramPath, resolvedProgramPath);
                if (!string.IsNullOrEmpty(WorkingDirectory))
                {
                    _logger?.LogDebug("[DoExecuteAsync] Execute 解決されたWorkingDirectory: {OriginalPath} -> {ResolvedPath}", WorkingDirectory, resolvedWorkingDirectory);
                }

                var startInfo = new ProcessStartInfo
                {
                    FileName = resolvedProgramPath,
                    Arguments = Arguments,
                    WorkingDirectory = resolvedWorkingDirectory,
                    UseShellExecute = true,
                };
                await Task.Run(() =>
                {
                    Process.Start(startInfo);
                    LogMessage($"プログラムを実行しました: {Path.GetFileName(resolvedProgramPath)}");
                });
                return true;
            }
            catch (Exception ex)
            {
                LogMessage($"プログラムの実行に失敗しました: {ex.Message}");
                return false;
            }
        }
    }

    /// <summary>
    /// スクリーンショットコマンド（DI対応）
    /// </summary>
    [DirectCommand(DirectCommandRegistry.CommandTypes.Screenshot, "スクリーンショット", "System", "スクリーンショットを撮影して保存します")]
    public class ScreenshotCommand : BaseCommand, IScreenshotCommand
    {
        [SettingProperty("保存ディレクトリ", SettingControlType.FolderPicker,
            description: "スクリーンショットの保存先ディレクトリ",
            category: "基本設定")]
        public string SaveDirectory { get; set; } = string.Empty;

        [SettingProperty("ウィンドウタイトル", SettingControlType.WindowPicker,
            description: "キャプチャ対象のウィンドウタイトル（空の場合は全画面）",
            category: "ウィンドウ")]
        public string WindowTitle { get; set; } = string.Empty;

        [SettingProperty("ウィンドウクラス名", SettingControlType.TextBox,
            description: "キャプチャ対象のウィンドウクラス名",
            category: "ウィンドウ")]
        public string WindowClassName { get; set; } = string.Empty;

        private readonly IImageProcessingService? _imageProcessingService;

        public ScreenshotCommand(AutoTool.Command.Interface.ICommand? parent = null, IServiceProvider? serviceProvider = null)
            : base(parent, serviceProvider)
        {
            Description = "スクリーンショット";
            _imageProcessingService = GetService<IImageProcessingService>();
        }

        protected override void ValidateFiles()
        {
            ValidateSaveDirectoryParentExists(SaveDirectory, "保存先ディレクトリ");
        }

        protected override async Task<bool> DoExecuteAsync(CancellationToken cancellationToken)
        {
            if (_imageProcessingService == null)
            {
                LogMessage("画像処理サービスが利用できません");
                return false;
            }

            try
            {
                var dir = string.IsNullOrWhiteSpace(SaveDirectory)
                    ? Path.Combine(Environment.CurrentDirectory, "Screenshots")
                    : ResolvePath(SaveDirectory); // 相対パス対応

                _logger?.LogDebug("[DoExecuteAsync] Screenshot 解決された保存ディレクトリ: {OriginalPath} -> {ResolvedPath}", SaveDirectory ?? "(empty)", dir);

                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                var file = $"{DateTime.Now:yyyyMMdd_HHmmssfff}.png";
                var fullPath = Path.Combine(dir, file);

                string? capturedPath = null;
                if (string.IsNullOrEmpty(WindowTitle) && string.IsNullOrEmpty(WindowClassName))
                {
                    // 全画面キャプチャ
                    var screenRect = new System.Windows.Rect(0, 0, 
                        System.Windows.SystemParameters.PrimaryScreenWidth, 
                        System.Windows.SystemParameters.PrimaryScreenHeight);
                    capturedPath = await _imageProcessingService.CaptureRegionAsync(screenRect, cancellationToken);
                }
                else
                {
                    // ウィンドウキャプチャ
                    capturedPath = await _imageProcessingService.CaptureWindowAsync(WindowTitle, WindowClassName, cancellationToken);
                }

                if (cancellationToken.IsCancellationRequested) return false;

                if (!string.IsNullOrEmpty(capturedPath) && File.Exists(capturedPath))
                {
                    // キャプチャされたファイルを指定の場所に移動
                    File.Move(capturedPath, fullPath);
                    LogMessage($"スクリーンショットを保存しました: {fullPath}");
                    return true;
                }
                else
                {
                    LogMessage("スクリーンショットのキャプチャに失敗しました");
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogMessage($"スクリーンショットの保存に失敗しました: {ex.Message}");
                return false;
            }
        }
    }
}