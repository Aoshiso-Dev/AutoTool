using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;
using AutoTool.Command.Base;
using AutoTool.Command.Definition;
using AutoTool.Command.Interface;
using AutoTool.Services;
using AutoTool.Services.ImageProcessing;
using AutoTool.Services.Mouse;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using YoloWinLib;

namespace AutoTool.Command.Commands
{
    // インターフェース定義
    public interface IClickCommand : AutoTool.Command.Interface.ICommand 
    {
        System.Windows.Point MousePosition { get; set; }
        MouseButton Button { get; set; }
        bool UseBackgroundClick { get; set; }
        string WindowTitle { get; set; }
        string WindowClassName { get; set; }
    }

    public interface IClickImageCommand : AutoTool.Command.Interface.ICommand 
    {
        string ImagePath { get; set; }
        int Timeout { get; set; }
        int Interval { get; set; }
        double Threshold { get; set; }
        MouseButton Button { get; set; }
        bool UseBackgroundClick { get; set; }
        string WindowTitle { get; set; }
        string WindowClassName { get; set; }
        System.Windows.Media.Color? SearchColor { get; set; }
    }

    public interface IClickImageAICommand : AutoTool.Command.Interface.ICommand 
    {
        string ModelPath { get; set; }
        int ClassID { get; set; }
        double ConfThreshold { get; set; }
        double IoUThreshold { get; set; }
        int Timeout { get; set; }
        int Interval { get; set; }
        MouseButton Button { get; set; }
        bool UseBackgroundClick { get; set; }
        string WindowTitle { get; set; }
        string WindowClassName { get; set; }
    }

    // 実装クラス
    /// <summary>
    /// クリックコマンド（DI対応） - X/Y を MousePosition に統合
    /// </summary>
    [DirectCommand(DirectCommandRegistry.CommandTypes.Click, "クリック", "Mouse", "指定した座標をクリックします")]
    public class ClickCommand : BaseCommand, IClickCommand
    {
        private readonly IMouseService _mouseService;

        [SettingProperty("クリック座標", SettingControlType.CoordinatePicker,
             description: "クリックする座標 (X,Y)",
             category: "座標",
             defaultValue: null)]
        public System.Windows.Point MousePosition { get; set; } = new(100, 100);

        [SettingProperty("マウスボタン", SettingControlType.ComboBox,
            description: "使用するマウスボタン",
            category: "詳細",
            sourceCollection: "MouseButtons",
            defaultValue: MouseButton.Left)]
        public MouseButton Button { get; set; } = MouseButton.Left;

        [SettingProperty("バックグラウンドクリック", SettingControlType.CheckBox,
            description: "バックグラウンドでクリックする",
            category: "詳細",
            defaultValue: false)]
        public bool UseBackgroundClick { get; set; } = true;

        [SettingProperty("ウィンドウタイトル", SettingControlType.WindowPicker,
            description: "対象ウィンドウのタイトル",
            category: "ウィンドウ")]
        public string WindowTitle { get; set; } = string.Empty;

        [SettingProperty("ウィンドウクラス名", SettingControlType.TextBox,
            description: "対象ウィンドウのクラス名",
            category: "ウィンドウ")]
        public string WindowClassName { get; set; } = string.Empty;

        public ClickCommand(AutoTool.Command.Interface.ICommand? parent = null, IServiceProvider? serviceProvider = null)
            : base(parent, serviceProvider)
        {
            Description = "クリック";
            _mouseService = serviceProvider?.GetService<IMouseService>() ?? throw new InvalidOperationException("IMouseServiceが見つかりません");
        }

        protected override void ValidateSettings()
        {
            if (MousePosition.X < 0 || MousePosition.Y < 0)
            {
                throw new ArgumentException("座標は0以上で指定してください。");
            }
        }

        protected override async Task<bool> DoExecuteAsync(CancellationToken cancellationToken)
        {
            var x = (int)MousePosition.X;
            var y = (int)MousePosition.Y;

            await (Button switch
            {
                MouseButton.Left => UseBackgroundClick
                    ? _mouseService.BackgroundClickAsync(x, y, WindowTitle, WindowClassName)
                    : _mouseService.ClickAsync(x, y, WindowTitle, WindowClassName),
                MouseButton.Right => UseBackgroundClick
                    ? _mouseService.BackgroundRightClickAsync(x, y, WindowTitle, WindowClassName)
                    : _mouseService.RightClickAsync(x, y, WindowTitle, WindowClassName),
                MouseButton.Middle => UseBackgroundClick
                    ? _mouseService.BackgroundMiddleClickAsync(x, y, WindowTitle, WindowClassName)
                    : _mouseService.MiddleClickAsync(x, y, WindowTitle, WindowClassName),
                _ => throw new Exception("マウスボタンが不正です。"),
            });

            var targetDesc = string.IsNullOrEmpty(WindowTitle) && string.IsNullOrEmpty(WindowClassName)
                ? "グローバル" : $"{WindowTitle}[{WindowClassName}]";
            LogMessage($"{targetDesc} の ({x}, {y}) を {(UseBackgroundClick ? "バックグラウンドで" : string.Empty)}{Button} クリックしました");
            return true;
        }
    }

    /// <summary>
    /// 画像クリックコマンド（DI対応）
    /// </summary>
    [DirectCommand(DirectCommandRegistry.CommandTypes.ClickImage, "画像クリック", "Image", "指定した画像を検索してクリックします")]
    public class ClickImageCommand : BaseCommand, IClickImageCommand
    {
        private readonly IMouseService _mouseService;
        private readonly IImageProcessingService _imageProcessingService;

        [SettingProperty("画像ファイル", SettingControlType.FilePicker,
            description: "クリックする画像ファイル",
            category: "基本設定",
            isRequired: true,
            fileFilter: "画像ファイル (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp")]
        public string ImagePath { get; set; } = string.Empty;

        [SettingProperty("タイムアウト", SettingControlType.NumberBox,
            description: "タイムアウト時間（ミリ秒）",
            category: "基本設定",
            defaultValue: 5000)]
        public int Timeout { get; set; } = 5000;

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

        [SettingProperty("マウスボタン", SettingControlType.ComboBox,
            description: "使用するマウスボタン",
            category: "詳細",
            sourceCollection: "MouseButtons",
            defaultValue: MouseButton.Left)]
        public MouseButton Button { get; set; } = MouseButton.Left;

        [SettingProperty("バックグラウンドクリック", SettingControlType.CheckBox,
            description: "バックグラウンドでクリックする",
            category: "詳細",
            defaultValue: false)]
        public bool UseBackgroundClick { get; set; } = true;

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

        public ClickImageCommand(AutoTool.Command.Interface.ICommand? parent = null, IServiceProvider? serviceProvider = null)
            : base(parent, serviceProvider)
        {
            Description = "画像クリック";
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
            var timeoutMs = Timeout <= 0 ? 0 : Timeout; // ms
            var intervalMs = Interval <= 0 ? 500 : Interval; // 安全な下限
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
                        MouseButton.Left => UseBackgroundClick
                            ? _mouseService.BackgroundClickAsync((int)point.Value.X, (int)point.Value.Y, WindowTitle, WindowClassName)
                            : _mouseService.ClickAsync((int)point.Value.X, (int)point.Value.Y, WindowTitle, WindowClassName),
                        MouseButton.Right => UseBackgroundClick
                            ? _mouseService.BackgroundRightClickAsync((int)point.Value.X, (int)point.Value.Y, WindowTitle, WindowClassName)
                            : _mouseService.RightClickAsync((int)point.Value.X, (int)point.Value.Y, WindowTitle, WindowClassName),
                        MouseButton.Middle => UseBackgroundClick
                            ? _mouseService.BackgroundMiddleClickAsync((int)point.Value.X, (int)point.Value.Y, WindowTitle, WindowClassName)
                            : _mouseService.MiddleClickAsync((int)point.Value.X, (int)point.Value.Y, WindowTitle, WindowClassName),
                        _ => throw new Exception("マウスボタンが不正です。"),
                    });
                    var targetDesc = string.IsNullOrEmpty(WindowTitle) && string.IsNullOrEmpty(WindowClassName)
                        ? "グローバル" : $"{WindowTitle}[{WindowClassName}]";
                    LogMessage($"{targetDesc} の ({(int)point.Value.X}, {(int)point.Value.Y}) を {(UseBackgroundClick ? "バックグラウンドで" : string.Empty)}{Button} クリックしました");
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

    /// <summary>
    /// AI画像クリックコマンド（DI対応）
    /// </summary>
    [DirectCommand(DirectCommandRegistry.CommandTypes.ClickImageAI, "AI画像クリック", "AI", "AI検出した画像をクリックします")]
    public class ClickImageAICommand : BaseCommand, IClickImageAICommand
    {
        private readonly IMouseService _mouseService;

        [SettingProperty("ONNXモデル", SettingControlType.OnnxPicker,
            description: "使用するONNXモデルファイル",
            category: "AI設定",
            isRequired: true)]
        public string ModelPath { get; set; } = string.Empty;

        [SettingProperty("クラスID", SettingControlType.NumberBox,
            description: "検出対象のクラスID",
            category: "AI設定",
            isRequired: true,
            defaultValue: 0)]
        public int ClassID { get; set; } = 0;

        [SettingProperty("信頼度閾値", SettingControlType.Slider,
            description: "検出の信頼度閾値",
            category: "AI設定",
            defaultValue: 0.5,
            minValue: 0.0,
            maxValue: 1.0)]
        public double ConfThreshold { get; set; } = 0.5;

        [SettingProperty("IoU閾値", SettingControlType.Slider,
            description: "IoU（重複度）の閾値",
            category: "AI設定",
            defaultValue: 0.4,
            minValue: 0.0,
            maxValue: 1.0)]
        public double IoUThreshold { get; set; } = 0.4;

        [SettingProperty("タイムアウト", SettingControlType.NumberBox,
            description: "タイムアウト時間（ミリ秒）",
            category: "基本設定",
            defaultValue: 5000)]
        public int Timeout { get; set; } = 5000;

        [SettingProperty("検索間隔", SettingControlType.NumberBox,
            description: "AI検出の間隔（ミリ秒）",
            category: "基本設定",
            defaultValue: 500)]
        public int Interval { get; set; } = 500;

        [SettingProperty("マウスボタン", SettingControlType.ComboBox,
            description: "使用するマウスボタン",
            category: "詳細",
            sourceCollection: "MouseButtons",
            defaultValue: MouseButton.Left)]
        public MouseButton Button { get; set; } = MouseButton.Left;

        [SettingProperty("バックグラウンドクリック", SettingControlType.CheckBox,
            description: "バックグラウンドでクリックする",
            category: "詳細",
            defaultValue: false)]
        public bool UseBackgroundClick { get; set; } = true;

        [SettingProperty("ウィンドウタイトル", SettingControlType.WindowPicker,
            description: "対象ウィンドウのタイトル",
            category: "ウィンドウ")]
        public string WindowTitle { get; set; } = string.Empty;

        [SettingProperty("ウィンドウクラス名", SettingControlType.TextBox,
            description: "対象ウィンドウのクラス名",
            category: "ウィンドウ")]
        public string WindowClassName { get; set; } = string.Empty;

        public ClickImageAICommand(AutoTool.Command.Interface.ICommand? parent = null, IServiceProvider? serviceProvider = null)
            : base(parent, serviceProvider)
        {
            Description = "AI画像クリック";
            _mouseService = serviceProvider?.GetService<IMouseService>() ?? throw new InvalidOperationException("IMouseServiceが見つかりません");
        }

        protected override void ValidateSettings()
        {
            if (string.IsNullOrWhiteSpace(ModelPath)) throw new ArgumentException("ONNXモデルファイルを指定してください。");
            if (ClassID < 0) throw new ArgumentException("ClassIDは0以上で指定してください。");
            if (ConfThreshold < 0.0 || ConfThreshold > 1.0) throw new ArgumentException("信頼度閾値は0.0〜1.0の範囲で指定してください。");
            if (IoUThreshold < 0.0 || IoUThreshold > 1.0) throw new ArgumentException("IoU閾値は0.0〜1.0の範囲で指定してください。");
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
                                    MouseButton.Left => UseBackgroundClick
                                        ? _mouseService.BackgroundClickAsync((int)centerX, (int)centerY, WindowTitle, WindowClassName)
                                        : _mouseService.ClickAsync((int)centerX, (int)centerY, WindowTitle, WindowClassName),
                                    MouseButton.Right => UseBackgroundClick
                                        ? _mouseService.BackgroundRightClickAsync((int)centerX, (int)centerY, WindowTitle, WindowClassName)
                                        : _mouseService.RightClickAsync((int)centerX, (int)centerY, WindowTitle, WindowClassName),
                                    MouseButton.Middle => UseBackgroundClick
                                        ? _mouseService.BackgroundMiddleClickAsync((int)centerX, (int)centerY, WindowTitle, WindowClassName)
                                        : _mouseService.MiddleClickAsync((int)centerX, (int)centerY, WindowTitle, WindowClassName),
                                    _ => throw new Exception("マウスボタンが不正です。"),
                                });

                                var targetDesc = string.IsNullOrEmpty(WindowTitle) && string.IsNullOrEmpty(WindowClassName)
                                    ? "グローバル" : $"{WindowTitle}[{WindowClassName}]";

                                LogMessage($"{targetDesc} の ({(int)centerX}, {(int)centerY}) を {(UseBackgroundClick ? "バックグラウンドで" : string.Empty)}{Button} クリックしました");

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
                    LogMessage("AI画像検出がキャンセルされました");
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