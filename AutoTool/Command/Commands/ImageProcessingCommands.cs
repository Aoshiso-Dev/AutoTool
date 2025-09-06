using AutoTool.Command.Base;
using AutoTool.Command.Interface;
using AutoTool.Command.Definition;
using AutoTool.Services.ImageProcessing;
using AutoTool.Services.UI;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace AutoTool.Command.Commands
{
    /// <summary>
    /// 画像検索コマンド
    /// </summary>
    [DirectCommand(DirectCommandRegistry.CommandTypes.SearchImage, "画像検索", "ImageProcessing", "指定した画像をテンプレートマッチングで検索します")]
    public class SearchImageCommand : BaseCommand
    {
        [SettingProperty("画像パス", SettingControlType.FilePicker,
            description: "検索対象の画像ファイルを選択してください",
            category: "基本設定",
            isRequired: true,
            fileFilter: "画像ファイル (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp")]
        public string ImagePath { get; set; } = "";

        [SettingProperty("閾値", SettingControlType.Slider,
            description: "マッチング閾値 (0.0-1.0)",
            category: "詳細設定",
            defaultValue: 0.8,
            minValue: 0.0,
            maxValue: 1.0)]
        public double Threshold { get; set; } = 0.8;

        [SettingProperty("ウィンドウタイトル", SettingControlType.WindowPicker,
            description: "検索対象のウィンドウタイトル（空の場合は画面全体）",
            category: "ウィンドウ")]
        public string WindowTitle { get; set; } = "";

        [SettingProperty("ウィンドウクラス名", SettingControlType.TextBox,
            description: "検索対象のウィンドウクラス名",
            category: "ウィンドウ")]
        public string WindowClassName { get; set; } = "";

        private readonly IImageProcessingService? _imageProcessingService;

        public SearchImageCommand(AutoTool.Command.Interface.ICommand? parent = null, IServiceProvider? serviceProvider = null)
            : base(parent, serviceProvider)
        {
            Description = "画像検索";
            _imageProcessingService = GetService<IImageProcessingService>();
        }

        protected override void ValidateFiles()
        {
            if (!string.IsNullOrEmpty(ImagePath))
            {
                ValidateFileExists(ImagePath, "画像ファイル");
            }
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
                if (string.IsNullOrEmpty(ImagePath))
                {
                    LogMessage("画像パスが指定されていません");
                    return false;
                }

                var resolvedImagePath = ResolvePath(ImagePath);
                LogMessage($"画像検索開始: {Path.GetFileName(resolvedImagePath)}");

                System.Windows.Point? result = null;
                if (!string.IsNullOrEmpty(WindowTitle))
                {
                    result = await _imageProcessingService.SearchImageInWindowAsync(
                        resolvedImagePath, WindowTitle, WindowClassName, Threshold, cancellationToken);
                }
                else
                {
                    result = await _imageProcessingService.SearchImageOnScreenAsync(
                        resolvedImagePath, Threshold, cancellationToken);
                }

                if (result.HasValue)
                {
                    LogMessage($"画像が見つかりました: ({result.Value.X}, {result.Value.Y})");
                    return true;
                }
                else
                {
                    LogMessage("画像が見つかりませんでした");
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogMessage($"画像検索エラー: {ex.Message}");
                return false;
            }
        }
    }

    /// <summary>
    /// 色検索コマンド
    /// </summary>
    [DirectCommand(DirectCommandRegistry.CommandTypes.SearchColor, "色検索", "ImageProcessing", "指定した色を画面から検索します")]
    public class SearchColorCommand : BaseCommand
    {
        [SettingProperty("検索色", SettingControlType.ColorPicker,
            description: "検索対象の色",
            category: "基本設定",
            defaultValue: "#FF0000")]
        public System.Windows.Media.Color SearchColor { get; set; } = System.Windows.Media.Color.FromRgb(255, 0, 0);

        [SettingProperty("許容差", SettingControlType.NumberBox,
            description: "色の許容差 (0-255)",
            category: "詳細設定",
            defaultValue: 10,
            minValue: 0,
            maxValue: 255)]
        public int Tolerance { get; set; } = 10;

        [SettingProperty("ウィンドウタイトル", SettingControlType.WindowPicker,
            description: "検索対象のウィンドウタイトル",
            category: "ウィンドウ")]
        public string WindowTitle { get; set; } = "";

        [SettingProperty("ウィンドウクラス名", SettingControlType.TextBox,
            description: "検索対象のウィンドウクラス名",
            category: "ウィンドウ")]
        public string WindowClassName { get; set; } = "";

        private readonly IImageProcessingService? _imageProcessingService;

        public SearchColorCommand(AutoTool.Command.Interface.ICommand? parent = null, IServiceProvider? serviceProvider = null)
            : base(parent, serviceProvider)
        {
            Description = "色検索";
            _imageProcessingService = GetService<IImageProcessingService>();
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
                // System.Windows.Media.Color を System.Drawing.Color に変換
                var drawingColor = System.Drawing.Color.FromArgb(SearchColor.A, SearchColor.R, SearchColor.G, SearchColor.B);

                LogMessage($"色検索開始: RGB({drawingColor.R}, {drawingColor.G}, {drawingColor.B})");

                var result = await _imageProcessingService.SearchColorAsync(
                    drawingColor, Tolerance, WindowTitle, WindowClassName, cancellationToken);

                if (result.HasValue)
                {
                    LogMessage($"色が見つかりました: ({result.Value.X}, {result.Value.Y})");
                    return true;
                }
                else
                {
                    LogMessage("色が見つかりませんでした");
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogMessage($"色検索エラー: {ex.Message}");
                return false;
            }
        }
    }

    /// <summary>
    /// 色フィルタ付き画像検索コマンド
    /// </summary>
    [DirectCommand(DirectCommandRegistry.CommandTypes.SearchImageWithColorFilter, "色フィルタ画像検索", "ImageProcessing", "指定した色をフィルタリングして画像を検索します")]
    public class SearchImageWithColorFilterCommand : BaseCommand
    {
        [SettingProperty("画像パス", SettingControlType.FilePicker,
            description: "検索対象の画像ファイル",
            category: "基本設定",
            isRequired: true,
            fileFilter: "画像ファイル (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp")]
        public string ImagePath { get; set; } = "";

        [SettingProperty("フィルタ色", SettingControlType.ColorPicker,
            description: "フィルタリングする色",
            category: "基本設定",
            defaultValue: "#FF0000")]
        public System.Windows.Media.Color FilterColor { get; set; } = System.Windows.Media.Color.FromRgb(255, 0, 0);

        [SettingProperty("マッチ閾値", SettingControlType.Slider,
            description: "マッチング閾値 (0.0-1.0)",
            category: "詳細設定",
            defaultValue: 0.8,
            minValue: 0.0,
            maxValue: 1.0)]
        public double MatchThreshold { get; set; } = 0.8;

        [SettingProperty("ウィンドウタイトル", SettingControlType.WindowPicker,
            description: "検索対象のウィンドウタイトル",
            category: "ウィンドウ")]
        public string WindowTitle { get; set; } = "";

        [SettingProperty("ウィンドウクラス名", SettingControlType.TextBox,
            description: "検索対象のウィンドウクラス名",
            category: "ウィンドウ")]
        public string WindowClassName { get; set; } = "";

        private readonly IImageProcessingService? _imageProcessingService;

        public SearchImageWithColorFilterCommand(AutoTool.Command.Interface.ICommand? parent = null, IServiceProvider? serviceProvider = null)
            : base(parent, serviceProvider)
        {
            Description = "色フィルタ画像検索";
            _imageProcessingService = GetService<IImageProcessingService>();
        }

        protected override void ValidateFiles()
        {
            if (!string.IsNullOrEmpty(ImagePath))
            {
                ValidateFileExists(ImagePath, "画像ファイル");
            }
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
                if (string.IsNullOrEmpty(ImagePath))
                {
                    LogMessage("画像パスが指定されていません");
                    return false;
                }

                var resolvedImagePath = ResolvePath(ImagePath);

                // System.Windows.Media.Color を System.Drawing.Color に変換
                var drawingFilterColor = System.Drawing.Color.FromArgb(FilterColor.A, FilterColor.R, FilterColor.G, FilterColor.B);

                LogMessage($"色フィルタ画像検索開始: {Path.GetFileName(resolvedImagePath)}, フィルタ色: RGB({drawingFilterColor.R}, {drawingFilterColor.G}, {drawingFilterColor.B})");

                var result = await _imageProcessingService.SearchImageWithColorFilterAsync(
                    resolvedImagePath, drawingFilterColor, MatchThreshold, WindowTitle, WindowClassName, cancellationToken);

                if (result.HasValue)
                {
                    LogMessage($"色フィルタ画像検索成功: ({result.Value.X}, {result.Value.Y})");
                    return true;
                }
                else
                {
                    LogMessage("色フィルタ画像が見つかりませんでした");
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogMessage($"色フィルタ画像検索エラー: {ex.Message}");
                return false;
            }
        }
    }
}