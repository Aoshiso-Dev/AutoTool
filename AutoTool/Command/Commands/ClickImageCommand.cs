using AutoTool.Command.Base;
using AutoTool.Command.Definition;
using AutoTool.Services.ImageProcessing;
using AutoTool.Services.Mouse;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace AutoTool.Command.Commands
{
    [AutoToolCommand(nameof(ClickImageCommand), typeof(ClickImageCommand))]
    public class ClickImageCommand : BaseCommand
    {
        private readonly IMouseService _mouseService;
        private readonly IImageProcessingService _imageProcessingService;
        private CancellationTokenSource? _cts;

        [Category("基本設定"), DisplayName("画像ファイル")]
        public string ImagePath { get; set; } = string.Empty;

        [Category("基本設定"), DisplayName("マウスボタン")]
        public MouseButton Button { get; set; } = MouseButton.Left;

        [Category("時間設定"), DisplayName("タイムアウト(ms)")]
        public int Timeout { get; set; } = 5000;

        [Category("時間設定"), DisplayName("検索間隔(ms)")]
        public int Interval { get; set; } = 500;

        [Category("ウィンドウ設定"), DisplayName("ウィンドウタイトル")]
        public string WindowTitle { get; set; } = string.Empty;

        [Category("ウィンドウ設定"), DisplayName("ウィンドウクラス名")]
        public string WindowClassName { get; set; } = string.Empty;

        [Category("詳細設定"), DisplayName("閾値")]
        public double Threshold { get; set; } = 0.8;

        [Category("詳細設定"), DisplayName("検索色")]
        public System.Windows.Media.Color? SearchColor { get; set; } = null;

        public ClickImageCommand(IAutoToolCommand? parent = null, IServiceProvider? serviceProvider = null)
            : base(parent, serviceProvider)
        {
            Description = "指定した画像をクリックします";
            _mouseService = serviceProvider?.GetService<IMouseService>() ?? throw new InvalidOperationException("IMouseServiceが見つかりません");
            _imageProcessingService = serviceProvider?.GetService<IImageProcessingService>() ?? throw new InvalidOperationException("IImageProcessingServiceが見つかりません");
        }
        
        protected override void ValidateSettings()
        {
            if (Timeout <= 0) throw new ArgumentException("タイムアウトは正の値で指定してください。");
            if (Interval <= 0) throw new ArgumentException("検索間隔は正の値で指定してください。");
            if (Threshold < 0.0 || Threshold > 1.0) throw new ArgumentException("閾値は0.0〜1.0の範囲で指定してください。");
            if (string.IsNullOrWhiteSpace(ImagePath)) throw new ArgumentException("画像ファイルを指定してください。");
        }

        protected override void ValidateFiles()
        {
            if (!string.IsNullOrEmpty(ImagePath))
            {
                _logger?.LogDebug("[ValidateFiles] ClickImage ImagePath検証開始: {ImagePath}", ImagePath);
                ValidateFileExists(ImagePath, "画像ファイル");
                _logger?.LogDebug("[ValidateFiles] ClickImage ImagePath検証成功: {ImagePath}", ImagePath);
            }
        }

        protected override async Task<bool> DoExecuteAsync(CancellationToken cancellationToken)
        {
            var timeoutMs = Timeout <= 0 ? 0 : Timeout;
            var intervalMs = Interval <= 0 ? 500 : Interval;
            var stopwatch = Stopwatch.StartNew();
            bool found = false;

            var resolvedImagePath = ResolvePath(ImagePath);
            _logger?.LogDebug("[DoExecuteAsync] ClickImage 解決されたImagePath: {OriginalPath} -> {ResolvedPath}", ImagePath, resolvedImagePath);

            if (timeoutMs == 0)
            {
                LogMessage("Timeout=0 のため即終了(失敗)");
                ReportProgress(1, 1);
                return false;
            }

            while (stopwatch.ElapsedMilliseconds < timeoutMs && !cancellationToken.IsCancellationRequested)
            {
                System.Windows.Point? point = null;

                try
                {
                    if (SearchColor.HasValue)
                    {
                        var c = Color.FromArgb(SearchColor.Value.A, SearchColor.Value.R, SearchColor.Value.G, SearchColor.Value.B);
                        point = await _imageProcessingService.SearchImageWithColorFilterAsync(resolvedImagePath, c, Threshold, WindowTitle, WindowClassName, cancellationToken);
                    }
                    else if (!string.IsNullOrEmpty(WindowTitle))
                    {
                        point = await _imageProcessingService.SearchImageInWindowAsync(resolvedImagePath, WindowTitle, WindowClassName, Threshold, cancellationToken);
                    }
                    else
                    {
                        point = await _imageProcessingService.SearchImageOnScreenAsync(resolvedImagePath, Threshold, cancellationToken);
                    }
                }
                catch (OperationCanceledException)
                {
                    LogMessage("画像待機がキャンセルされました");
                    throw;
                }
                catch (Exception ex)
                {
                    LogMessage($"画像検索中エラー: {ex.Message}");
                    throw;
                }

                if (point != null)
                {
                    found = true;
                    LogMessage($"画像が見つかりました: ({point.Value.X}, {point.Value.Y})");
                    await (Button switch
                    {
                        MouseButton.Left => _mouseService.ClickAsync((int)point.Value.X, (int)point.Value.Y, WindowTitle, WindowClassName),
                        MouseButton.Right => _mouseService.RightClickAsync((int)point.Value.X, (int)point.Value.Y, WindowTitle, WindowClassName),
                        MouseButton.Middle => _mouseService.MiddleClickAsync((int)point.Value.X, (int)point.Value.Y, WindowTitle, WindowClassName),
                        _ => throw new Exception("マウスボタンが不正です。"),
                    });
                    var targetDesc = string.IsNullOrEmpty(WindowTitle) && string.IsNullOrEmpty(WindowClassName)
                        ? "グローバル" : $"{WindowTitle}[{WindowClassName}]";
                    LogMessage($"{targetDesc} の ({(int)point.Value.X}, {(int)point.Value.Y}) を {Button} クリックしました");
                    break;
                }

                var elapsed = stopwatch.ElapsedMilliseconds;
                ReportProgress(elapsed, timeoutMs);

                var slice = Math.Min(intervalMs, 250);
                await Task.Delay(slice, cancellationToken);
            }

            if (!found)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    LogMessage("画像待機がキャンセルされました");
                    return false;
                }
                LogMessage("画像が見つかりませんでした");
                throw new TimeoutException("画像が見つかりませんでした。");
            }

            ReportProgress(timeoutMs, timeoutMs);
            return found;
        }
    }
}
