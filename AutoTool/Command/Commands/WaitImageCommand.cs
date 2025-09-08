using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using System.ComponentModel;
using AutoTool.Command.Base;
using AutoTool.Command.Definition;
using AutoTool.Services.ImageProcessing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AutoTool.Command.Commands
{
    [AutoToolCommand(nameof(WaitImageCommand), typeof(WaitImageCommand))]
    public class WaitImageCommand : BaseCommand
    {
        [Category("基本設定"), DisplayName("画像ファイル")]
        public string ImagePath { get; set; } = string.Empty;

        [Category("基本設定"), DisplayName("タイムアウト(ms)")]
        public int Timeout { get; set; } = 5000;

        [Category("基本設定"), DisplayName("間隔(ms)")]
        public int Interval { get; set; } = 500;

        [Category("詳細設定"), DisplayName("閾値")]
        public double Threshold { get; set; } = 0.8;

        [Category("ウィンドウ"), DisplayName("ウィンドウタイトル")]
        public string WindowTitle { get; set; } = string.Empty;

        [Category("ウィンドウ"), DisplayName("ウィンドウクラス名")]
        public string WindowClassName { get; set; } = string.Empty;

        [Category("詳細設定"), DisplayName("検索色")]
        public System.Windows.Media.Color? SearchColor { get; set; } = null;

        private readonly IImageProcessingService? _imageProcessingService;

        public WaitImageCommand(IAutoToolCommand? parent = null, IServiceProvider? serviceProvider = null)
            : base(parent, serviceProvider)
        {
            Description = "画像待機";
            _imageProcessingService = GetService<IImageProcessingService>();
        }

        protected override void ValidateSettings()
        {
            if (Timeout <= 0) throw new ArgumentException("タイムアウトは正の値を指定してください");
            if (Interval <= 0) throw new ArgumentException("間隔は正の値を指定してください");
            if (Threshold < 0.0 || Threshold > 1.0) throw new ArgumentException("閾値は0.0-1.0の範囲で指定してください");
        }

        protected override void ValidateFiles()
        {
            if (!string.IsNullOrEmpty(ImagePath))
            {
                _logger?.LogDebug("[ValidateFiles] WaitImage ImagePath: {ImagePath}", ImagePath);
                ValidateFileExists(ImagePath, "画像ファイル");
            }
        }

        protected override async Task<bool> DoExecuteAsync(CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(ImagePath)) return false;

            var timeoutMs = Timeout <= 0 ? 0 : Timeout;
            var intervalMs = Interval <= 0 ? 500 : Interval;
            var stopwatch = Stopwatch.StartNew();
            bool found = false;

            LogMessage($"画像待機開始: Timeout={timeoutMs}ms Interval={intervalMs}ms Image={ImagePath}");

            if (timeoutMs == 0)
            {
                LogMessage("Timeout=0 の場合は即終了します(失敗)");
                ReportProgress(1, 1);
                return false;
            }

            while (stopwatch.ElapsedMilliseconds < timeoutMs && !cancellationToken.IsCancellationRequested)
            {
                var resolvedImagePath = ResolvePath(ImagePath);
                System.Windows.Point? point = null;

                try
                {
                    if (SearchColor.HasValue)
                    {
                        var c = System.Drawing.Color.FromArgb(SearchColor.Value.A, SearchColor.Value.R, SearchColor.Value.G, SearchColor.Value.B);
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
                    return false;
                }
                catch (Exception ex)
                {
                    LogMessage($"画像検出エラー: {ex.Message}");
                }

                if (point != null)
                {
                    found = true;
                    LogMessage($"画像が見つかりました: ({point.Value.X}, {point.Value.Y}) - {stopwatch.ElapsedMilliseconds}ms");
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
                LogMessage("タイムアウトにより画像が見つかりませんでした");
            }

            ReportProgress(timeoutMs, timeoutMs);
            return found;
        }
    }
}
