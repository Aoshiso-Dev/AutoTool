using AutoTool.Command.Base;
using AutoTool.Command.Definition;
using AutoTool.Services.Mouse;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using YoloWinLib;

namespace AutoTool.Command.Commands
{
    [AutoToolCommand(nameof(ClickImageAICommand), typeof(ClickImageAICommand))]
    public class ClickImageAICommand : BaseCommand
    {
        private readonly IMouseService _mouseService;

        [Category("基本設定"), DisplayName("マウスボタン")]
        public MouseButton Button { get; set; } = MouseButton.Left;

        [Category("基本設定"), DisplayName("ONNXモデル")]
        public string ModelPath { get; set; } = string.Empty;

        [Category("基本設定"), DisplayName("クラスID")]
        public int ClassID { get; set; } = 0;

        [Category("基本設定"), DisplayName("信頼度閾値")]
        public double ConfThreshold { get; set; } = 0.5;

        [Category("基本設定"), DisplayName("IoU閾値")]
        public double IoUThreshold { get; set; } = 0.4;

        [Category("時間設定"), DisplayName("タイムアウト(ms)")]
        public int Timeout { get; set; } = 5000;

        [Category("時間設定"), DisplayName("検索間隔(ms)")]
        public int Interval { get; set; } = 500;

        [Category("ウィンドウ設定"), DisplayName("ウィンドウタイトル")]
        public string WindowTitle { get; set; } = string.Empty;

        [Category("ウィンドウ設定"), DisplayName("ウィンドウクラス名")]
        public string WindowClassName { get; set; } = string.Empty;

        public ClickImageAICommand(IAutoToolCommand? parent = null, IServiceProvider? serviceProvider = null)
            : base(parent, serviceProvider)
        {
            Description = "AI画像クリック";
            _mouseService = serviceProvider?.GetService<IMouseService>() ?? throw new InvalidOperationException("IMouseServiceが見つかりません");
        }

        protected override void ValidateSettings()
        {
            if (string.IsNullOrWhiteSpace(ModelPath)) throw new ArgumentException("ONNXモデルファイルを指定してください。");
            if (ClassID < 0) throw new ArgumentException("ClassIDは0以上で指定してください。");
            if (ConfThreshold is < 0.0 or > 1.0) throw new ArgumentException("信頼度閾値は0.0〜1.0の範囲で指定してください。");
            if (IoUThreshold is < 0.0 or > 1.0) throw new ArgumentException("IoU閾値は0.0〜1.0の範囲で指定してください。");
            if (Timeout <= 0) throw new ArgumentException("タイムアウトは正の値で指定してください。");
            if (Interval <= 0) throw new ArgumentException("検索間隔は正の値で指定してください。");
        }

        protected override void ValidateFiles()
        {
            if (!string.IsNullOrEmpty(ModelPath))
            {
                _logger?.LogDebug("[ValidateFiles] ClickImageAI ModelPath検証開始: {ModelPath}", ModelPath);
                ValidateFileExists(ModelPath, "ONNXモデルファイル");
                _logger?.LogDebug("[ValidateFiles] ClickImageAI ModelPath検証成功: {ModelPath}", ModelPath);
            }
        }

        protected override async Task<bool> DoExecuteAsync(CancellationToken cancellationToken)
        {
            var timeoutMs = Timeout <= 0 ? 0 : Timeout;
            var intervalMs = Interval <= 0 ? 500 : Interval;
            var stopwatch = Stopwatch.StartNew();
            bool found = false;

            var resolvedModelPath = ResolvePath(ModelPath);
            _logger?.LogDebug("[DoExecuteAsync] ClickImageAI 解決されたModelPath: {OriginalPath} -> {ResolvedPath}", ModelPath, resolvedModelPath);

            LogMessage($"AI画像検出開始: ClassID {ClassID} Model={Path.GetFileName(resolvedModelPath)}");

            if (timeoutMs == 0)
            {
                LogMessage("Timeout=0 のため即終了(失敗)");
                ReportProgress(1, 1);
                return false;
            }

            try
            {
                YoloWin.Init(resolvedModelPath, 640, true);

                while (stopwatch.ElapsedMilliseconds < timeoutMs && !cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        var det = YoloWin.DetectFromWindowTitle(WindowTitle, (float)ConfThreshold, (float)IoUThreshold).Detections;

                        if (det.Count > 0)
                        {
                            var detected = det.FirstOrDefault(d => d.ClassId == ClassID);
                            if (detected != null)
                            {
                                var centerX = detected.Rect.X + detected.Rect.Width / 2;
                                var centerY = detected.Rect.Y + detected.Rect.Height / 2;

                                LogMessage($"AI画像が見つかりました: ({centerX}, {centerY}) ClassId: {detected.ClassId}");

                                await (Button switch
                                {
                                    MouseButton.Left => _mouseService.ClickAsync((int)centerX, (int)centerY, WindowTitle, WindowClassName),
                                    MouseButton.Right => _mouseService.RightClickAsync((int)centerX, (int)centerY, WindowTitle, WindowClassName),
                                    MouseButton.Middle => _mouseService.MiddleClickAsync((int)centerX, (int)centerY, WindowTitle, WindowClassName),
                                    _ => throw new Exception("マウスボタンが不正です。"),
                                });

                                var targetDesc = string.IsNullOrEmpty(WindowTitle) && string.IsNullOrEmpty(WindowClassName)
                                    ? "グローバル" : $"{WindowTitle}[{WindowClassName}]";

                                LogMessage($"{targetDesc} の ({(int)centerX}, {(int)centerY}) を {Button} クリックしました");

                                found = true;
                                break;
                            }
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        LogMessage("AI画像検出がキャンセルされました");
                        throw;
                    }
                    catch (Exception ex)
                    {
                        LogMessage($"AI画像検出中エラー: {ex.Message}");
                        throw;
                    }

                    var elapsed = stopwatch.ElapsedMilliseconds;
                    ReportProgress(elapsed, timeoutMs);

                    var slice = Math.Min(intervalMs, 250);
                    await Task.Delay(slice, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                LogMessage("AI画像検出がキャンセルされました");
                throw;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "[DoExecuteAsync] AI画像検出エラー: ModelPath={ModelPath}, ClassID={ClassID}", resolvedModelPath, ClassID);
                LogMessage($"AI画像検出エラー: {ex.Message}");
                throw;
            }

            if (!found)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    LogMessage("AI画像が見つかりませんでした（キャンセル）");
                    return false;
                }
                LogMessage("AI画像が見つかりませんでした");
                throw new TimeoutException("AI画像が見つかりませんでした。");
            }

            ReportProgress(timeoutMs, timeoutMs);
            return true;
        }
    }
}
