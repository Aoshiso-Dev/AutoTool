using System;
using System.IO;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using AutoTool.Command.Base;
using AutoTool.Command.Definition;
using AutoTool.Services.ImageProcessing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AutoTool.Command.Commands
{
    [AutoToolCommand(nameof(ScreenshotCommand), typeof(ScreenshotCommand))]
    public class ScreenshotCommand : BaseCommand
    {
        [Category("基本設定"), DisplayName("保存先フォルダ")]
        public string SaveDirectory { get; set; } = string.Empty;

        [Category("ウィンドウ"), DisplayName("ウィンドウタイトル")]
        public string WindowTitle { get; set; } = string.Empty;

        [Category("ウィンドウ"), DisplayName("ウィンドウクラス名")]
        public string WindowClassName { get; set; } = string.Empty;

        private readonly IImageProcessingService? _imageProcessingService;

        public ScreenshotCommand(IAutoToolCommand? parent = null, IServiceProvider? serviceProvider = null)
            : base(parent, serviceProvider)
        {
            Description = "スクリーンショット";
            _imageProcessingService = GetService<IImageProcessingService>();
        }

        protected override void ValidateFiles()
        {
            ValidateSaveDirectoryParentExists(SaveDirectory, "保存先フォルダ");
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
                    : ResolvePath(SaveDirectory);

                _logger?.LogDebug("[DoExecuteAsync] Screenshot SaveDirectory: {Original} -> {Resolved}", SaveDirectory ?? "(empty)", dir);

                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                var file = $"{DateTime.Now:yyyyMMdd_HHmmssfff}.png";
                var fullPath = Path.Combine(dir, file);

                string? capturedPath = null;
                if (string.IsNullOrEmpty(WindowTitle) && string.IsNullOrEmpty(WindowClassName))
                {
                    var screenRect = new System.Windows.Rect(0, 0, System.Windows.SystemParameters.PrimaryScreenWidth, System.Windows.SystemParameters.PrimaryScreenHeight);
                    capturedPath = await _imageProcessingService.CaptureRegionAsync(screenRect, cancellationToken);
                }
                else
                {
                    capturedPath = await _imageProcessingService.CaptureWindowAsync(WindowTitle, WindowClassName, cancellationToken);
                }

                if (cancellationToken.IsCancellationRequested) return false;

                if (!string.IsNullOrEmpty(capturedPath) && File.Exists(capturedPath))
                {
                    File.Move(capturedPath, fullPath);
                    LogMessage($"スクリーンショットを保存しました: {fullPath}");
                    return true;
                }
                else
                {
                    LogMessage("スクリーンショットキャプチャに失敗しました");
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogMessage($"スクリーンショットエラー: {ex.Message}");
                return false;
            }
        }
    }
}
