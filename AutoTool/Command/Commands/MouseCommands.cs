using System;
using System.Diagnostics;
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
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using YoloWinLib;

namespace AutoTool.Command.Commands
{
    // インターフェース定義
    public interface IClickCommand : AutoTool.Command.Interface.ICommand 
    {
        int X { get; set; }
        int Y { get; set; }
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
        MouseButton Button { get; set; }
        bool UseBackgroundClick { get; set; }
        string WindowTitle { get; set; }
        string WindowClassName { get; set; }
    }

    // 実装クラス
    /// <summary>
    /// クリックコマンド（DI対応）
    /// </summary>
    [DirectCommand(DirectCommandRegistry.CommandTypes.Click, "クリック", "Mouse", "指定した座標をクリックします")]
    public class ClickCommand : BaseCommand, IClickCommand
    {
        private readonly IMouseService _mouseService;

        [SettingProperty("座標", SettingControlType.CoordinatePicker,
            description: "クリックする座標",
            category: "基本設定",
            isRequired: true,
            defaultValue: 0)]
        public int X { get; set; } = 0;
        public int Y { get; set; } = 0;

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
        public bool UseBackgroundClick { get; set; } = false;

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

        protected override async Task<bool> DoExecuteAsync(CancellationToken cancellationToken)
        {
            await (Button switch
            {
                MouseButton.Left => UseBackgroundClick
                    ? _mouseService.BackgroundClickAsync(X, Y, WindowTitle, WindowClassName)
                    : _mouseService.ClickAsync(X, Y, WindowTitle, WindowClassName),
                MouseButton.Right => UseBackgroundClick
                    ? _mouseService.BackgroundRightClickAsync(X, Y, WindowTitle, WindowClassName)
                    : _mouseService.RightClickAsync(X, Y, WindowTitle, WindowClassName),
                MouseButton.Middle => UseBackgroundClick
                    ? _mouseService.BackgroundMiddleClickAsync(X, Y, WindowTitle, WindowClassName)
                    : _mouseService.MiddleClickAsync(X, Y, WindowTitle, WindowClassName),
                _ => throw new Exception("マウスボタンが不正です。"),
            });

            var target = string.IsNullOrEmpty(WindowTitle) && string.IsNullOrEmpty(WindowClassName)
                ? "グローバル" : $"{WindowTitle}[{WindowClassName}]";
            
            LogMessage($"{target}の({X}, {Y})を{(UseBackgroundClick ? "バックグラウンドで" : "")}{Button}クリックしました。");

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
        public bool UseBackgroundClick { get; set; } = false;

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
            if (string.IsNullOrEmpty(ImagePath)) return false;

            // 相対パスを解決して実際の検索に使用
            var resolvedImagePath = ResolvePath(ImagePath);
            
            System.Windows.Point? point = null;

            if (SearchColor.HasValue)
            {
                // カラーフィルター付き画像検索
                var searchColorDrawing = System.Drawing.Color.FromArgb(
                    SearchColor.Value.A,
                    SearchColor.Value.R,
                    SearchColor.Value.G,
                    SearchColor.Value.B);

                point = await _imageProcessingService.SearchImageWithColorFilterAsync(
                    resolvedImagePath, searchColorDrawing, Threshold, WindowTitle, WindowClassName, cancellationToken);
            }
            else if (!string.IsNullOrEmpty(WindowTitle))
            {
                // ウィンドウ内画像検索
                point = await _imageProcessingService.SearchImageInWindowAsync(
                    resolvedImagePath, WindowTitle, WindowClassName, Threshold, cancellationToken);
            }
            else
            {
                // スクリーン全体で画像検索
                point = await _imageProcessingService.SearchImageOnScreenAsync(
                    resolvedImagePath, Threshold, cancellationToken);
            }

            if (point == null)
            {
                LogMessage("画像が見つかりませんでした");
                return false;
            }

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

            LogMessage($"{(string.IsNullOrEmpty(WindowTitle) && string.IsNullOrEmpty(WindowClassName) ? "グローバル" : $"{WindowTitle}[{WindowClassName}]")}の({(int)point.Value.X}, {(int)point.Value.Y})を{(UseBackgroundClick ? "バックグラウンドで" : "")}{Button}クリックしました。");

            return true;
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
        public bool UseBackgroundClick { get; set; } = false;

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

        protected override void ValidateFiles()
        {
            if (!string.IsNullOrEmpty(ModelPath))
            {
                ValidateFileExists(ModelPath, "ONNXモデルファイル");
            }
        }

        protected override async Task<bool> DoExecuteAsync(CancellationToken cancellationToken)
        {
            // 相対パスを解決して実際のモデル読み込みに使用
            var resolvedModelPath = ResolvePath(ModelPath);
            _logger?.LogDebug("[DoExecuteAsync] ClickImageAI 解決されたModelPath: {OriginalPath} -> {ResolvedPath}", ModelPath, resolvedModelPath);

            LogMessage($"AI画像検出開始: ClassID {ClassID}");

            try
            {
                YoloWin.Init(resolvedModelPath, 640, true);

                var det = YoloWin.DetectFromWindowTitle(WindowTitle, (float)ConfThreshold, (float)IoUThreshold).Detections;

                if (det.Count > 0)
                {
                    var target = det.FirstOrDefault(d => d.ClassId == ClassID);

                    if (target != null)
                    {
                        var centerX = target.Rect.X + target.Rect.Width / 2;
                        var centerY = target.Rect.Y + target.Rect.Height / 2;

                        LogMessage($"AI画像が見つかりました: ({centerX}, {centerY}) ClassId: {target.ClassId}");

                        // マウスクリック実行
                        if (_mouseService != null)
                        {
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

                            LogMessage($"{(string.IsNullOrEmpty(WindowTitle) && string.IsNullOrEmpty(WindowClassName) ? "グローバル" : $"{WindowTitle}[{WindowClassName}]")}の({(int)centerX}, {(int)centerY})を{(UseBackgroundClick ? "バックグラウンドで" : "")}{Button}クリックしました。");
                        }

                        return true;
                    }
                }

                LogMessage("AI画像が見つかりませんでした");
                return false;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "[DoExecuteAsync] AI画像検出エラー: ModelPath={ModelPath}, ClassID={ClassID}", resolvedModelPath, ClassID);
                LogMessage($"AI画像検出エラー: {ex.Message}");
                return false;
            }
        }
    }
}