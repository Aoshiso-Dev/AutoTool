using AutoTool.Command.Base;
using AutoTool.Command.Definition;
using AutoTool.Command.Interface;
using AutoTool.Services;
using AutoTool.Services.ImageProcessing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using YoloWinLib;

namespace AutoTool.Command.Commands
{
    // インターフェース定義
    public interface IWaitCommand : AutoTool.Command.Interface.ICommand 
    {
        int Wait { get; set; }
    }

    public interface IWaitImageCommand : AutoTool.Command.Interface.ICommand 
    {
        string ImagePath { get; set; }
        int Timeout { get; set; }
        int Interval { get; set; }
        double Threshold { get; set; }
        string WindowTitle { get; set; }
        string WindowClassName { get; set; }
        System.Windows.Media.Color? SearchColor { get; set; }
    }

    // 実装クラス
    /// <summary>
    /// 待機コマンド（DI対応）
    /// </summary>
    [DirectCommand(DirectCommandRegistry.CommandTypes.Wait, "待機", "Basic", "指定した時間待機します")]
    public class WaitCommand : BaseCommand, IWaitCommand
    {
        [SettingProperty("待機時間", SettingControlType.NumberBox,
            description: "待機時間（ミリ秒）",
            category: "基本設定",
            isRequired: true,
            defaultValue: 1000)]
        public int Wait { get; set; } = 1000;

        public WaitCommand(AutoTool.Command.Interface.ICommand? parent = null, IServiceProvider? serviceProvider = null)
            : base(parent, serviceProvider)
        {
            Description = "待機";
        }

        protected override async Task<bool> DoExecuteAsync(CancellationToken cancellationToken)
        {
            // 0以下は即完了
            if (Wait <= 0)
            {
                ReportProgress(1, 1);
                LogMessage("待機時間0のため即完了");
                return true;
            }

            var stopwatch = Stopwatch.StartNew();
            var totalWaitMs = Wait;
            int lastReported = -1;

            LogMessage($"待機開始 ({totalWaitMs}ms)");

            while (true)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    LogMessage("待機がキャンセルされました");
                    return false;
                }

                var elapsed = stopwatch.ElapsedMilliseconds;
                if (elapsed >= totalWaitMs)
                {
                    // 最終100%通知（先に送る事で次コマンド開始前にUI反映）
                    ReportProgress(totalWaitMs, totalWaitMs);
                    LogMessage("待機が完了しました");
                    return true;
                }

                var progress = (int)Math.Clamp((elapsed / (double)totalWaitMs) * 100, 0, 100);
                if (progress != lastReported)
                {
                    ReportProgress(elapsed, totalWaitMs);
                    lastReported = progress;

                    var remaining = Math.Max(0, totalWaitMs - elapsed);
                    LogMessage($"待機中... {progress}% (残り約{remaining}ms)");
                }

                // 残りが100ms未満ならその分だけ待機して正確に終了
                var nextSlice = (int)Math.Min(100, totalWaitMs - elapsed);
                await Task.Delay(nextSlice, cancellationToken);
            }
        }
    }

    /// <summary>
    /// 画像待機コマンド（DI対応）
    /// </summary>
    [DirectCommand(DirectCommandRegistry.CommandTypes.WaitImage, "画像待機", "Image", "指定した画像が画面に表示されるまで待機します")]
    public class WaitImageCommand : BaseCommand, IWaitImageCommand
    {
        [SettingProperty("画像ファイル", SettingControlType.FilePicker,
            description: "待機する画像ファイル",
            category: "基本設定",
            isRequired: true,
            fileFilter: "画像ファイル (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp")]
        public string ImagePath { get; set; } = string.Empty;

        [SettingProperty("タイムアウト", SettingControlType.NumberBox,
            description: "タイムアウト時間（ミリ秒）",
            category: "基本設定",
            defaultValue: 5000)]
        public int Timeout { get; set; } = 5000; // ミリ秒扱い（以前は *1000 されていた不具合を是正）

        [SettingProperty("検索間隔", SettingControlType.NumberBox,
            description: "画像検索の間隔（ミリ秒）",
            category: "基本設定",
            defaultValue: 500)]
        public int Interval { get; set; } = 500;

        [SettingProperty("閾値", SettingControlType.Slider,
            description: "画像認識の閾値",
            category: "詳細設定",
            defaultValue: 0.8,
            minValue: 0.0,
            maxValue: 1.0)]
        public double Threshold { get; set; } = 0.8;

        [SettingProperty("ウィンドウタイトル", SettingControlType.WindowPicker,
            description: "対象ウィンドウのタイトル",
            category: "ウィンドウ")]
        public string WindowTitle { get; set; } = string.Empty;

        [SettingProperty("ウィンドウクラス名", SettingControlType.TextBox,
            description: "対象ウィンドウのクラス名",
            category: "ウィンドウ")]
        public string WindowClassName { get; set; } = string.Empty;

        [SettingProperty("検索色", SettingControlType.ColorPicker,
            description: "特定の色で検索する場合の色",
            category: "詳細設定")]
        public System.Windows.Media.Color? SearchColor { get; set; } = null;

        private readonly IImageProcessingService? _imageProcessingService;

        public WaitImageCommand(AutoTool.Command.Interface.ICommand? parent = null, IServiceProvider? serviceProvider = null)
            : base(parent, serviceProvider)
        {
            Description = "画像待機";
            _imageProcessingService = GetService<IImageProcessingService>();
        }

        protected override void ValidateFiles()
        {
            if (!string.IsNullOrEmpty(ImagePath))
            {
                _logger?.LogDebug("[ValidateFiles] WaitImage ImagePath検証開始: {ImagePath}", ImagePath);
                ValidateFileExists(ImagePath, "画像ファイル");
                _logger?.LogDebug("[ValidateFiles] WaitImage ImagePath検証成功: {ImagePath}", ImagePath);
            }
        }

        protected override async Task<bool> DoExecuteAsync(CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(ImagePath)) return false;

            var timeoutMs = Timeout <= 0 ? 0 : Timeout; // ms
            var intervalMs = Interval <= 0 ? 500 : Interval; // 安全な下限
            var stopwatch = Stopwatch.StartNew();
            bool found = false;

            LogMessage($"画像待機開始 Timeout={timeoutMs}ms Interval={intervalMs}ms Image={ImagePath}");

            if (timeoutMs == 0)
            {
                LogMessage("Timeout=0 のため即終了(失敗)" );
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
                    LogMessage($"画像検索中エラー: {ex.Message}");
                }

                if (point != null)
                {
                    found = true;
                    LogMessage($"画像が見つかりました: ({point.Value.X}, {point.Value.Y}) - {stopwatch.ElapsedMilliseconds}ms");
                    break;
                }

                // 進捗更新
                var elapsed = stopwatch.ElapsedMilliseconds;
                ReportProgress(elapsed, timeoutMs);

                // Interval を尊重。ただしUI反映を阻害しないよう最大250msに分割
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
            }

            // 最終状態 100% を通知（見つかった / 見つからなかった 双方で統一）
            ReportProgress(timeoutMs, timeoutMs);
            return found;
        }
    }
}