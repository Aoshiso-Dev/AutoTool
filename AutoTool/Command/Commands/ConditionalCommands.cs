using AutoTool.Command.Base;
using AutoTool.Command.Definition;
using AutoTool.Command.Interface;
using AutoTool.Services;
using AutoTool.Services.ImageProcessing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using YoloWinLib;

namespace AutoTool.Command.Commands
{
    // インターフェース定義
    public interface IIfImageExistCommand : AutoTool.Command.Interface.ICommand, IIfCommand 
    {
        string ImagePath { get; set; }
        double Threshold { get; set; }
        string WindowTitle { get; set; }
        string WindowClassName { get; set; }
        System.Windows.Media.Color? SearchColor { get; set; }
    }

    public interface IIfImageNotExistCommand : AutoTool.Command.Interface.ICommand, IIfCommand 
    {
        string ImagePath { get; set; }
        double Threshold { get; set; }
        string WindowTitle { get; set; }
        string WindowClassName { get; set; }
        System.Windows.Media.Color? SearchColor { get; set; }
    }

    public interface IIfImageExistAICommand : AutoTool.Command.Interface.ICommand, IIfCommand 
    {
        string ModelPath { get; set; }
        int ClassID { get; set; }
        string WindowTitle { get; set; }
        string WindowClassName { get; set; }
        double ConfThreshold { get; set; }
        double IoUThreshold { get; set; }
    }

    public interface IIfImageNotExistAICommand : AutoTool.Command.Interface.ICommand, IIfCommand 
    {
        string ModelPath { get; set; }
        int ClassID { get; set; }
        string WindowTitle { get; set; }
        string WindowClassName { get; set; }
        double ConfThreshold { get; set; }
        double IoUThreshold { get; set; }
    }

    public interface IIfVariableCommand : AutoTool.Command.Interface.ICommand, IIfCommand 
    {
        string Name { get; set; }
        string Operator { get; set; }
        string Value { get; set; }
    }

    // 実装クラス
    /// <summary>
    /// 画像存在確認If文（DI対応）
    /// </summary>
    [DirectCommand(DirectCommandRegistry.CommandTypes.IfImageExist, "画像存在確認", "Condition", "指定した画像が存在する場合に子コマンドを実行します")]
    public class IfImageExistCommand : IfCommand, IIfImageExistCommand
    {
        [SettingProperty("画像ファイル", SettingControlType.FilePicker,
            description: "確認する画像ファイル",
            category: "基本設定",
            isRequired: true,
            fileFilter: "画像ファイル (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp")]
        public string ImagePath { get; set; } = string.Empty;

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

        public IfImageExistCommand(AutoTool.Command.Interface.ICommand? parent = null, IServiceProvider? serviceProvider = null)
            : base(parent, serviceProvider)
        {
            Description = "画像存在確認";
            _imageProcessingService = GetService<IImageProcessingService>();
        }

        protected override void ValidateFiles()
        {
            if (!string.IsNullOrEmpty(ImagePath))
            {
                _logger?.LogDebug("[ValidateFiles] IfImageExist ImagePath検証開始: {ImagePath}", ImagePath);
                ValidateFileExists(ImagePath, "画像ファイル");
                _logger?.LogDebug("[ValidateFiles] IfImageExist ImagePath検証成功: {ImagePath}", ImagePath);
            }
        }

        protected override async Task<bool> EvaluateConditionAsync(CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(ImagePath)) 
            {
                LogMessage("画像パスが設定されていません");
                return false;
            }

            if (_imageProcessingService == null)
            {
                LogMessage("画像処理サービスが利用できません");
                return false;
            }

            // 相対パスを解決して実際の検索に使用
            var resolvedImagePath = ResolvePath(ImagePath);
            _logger?.LogDebug("[EvaluateConditionAsync] IfImageExist 解決されたImagePath: {OriginalPath} -> {ResolvedPath}", ImagePath, resolvedImagePath);

            LogMessage($"画像の存在を確認中: {Path.GetFileName(resolvedImagePath)}");

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

            if (point != null)
            {
                LogMessage($"画像が見つかりました（条件: 真）: ({point.Value.X}, {point.Value.Y})");
                return true;
            }

            LogMessage("画像が見つかりませんでした（条件: 偽）");
            return false;
        }
    }

    /// <summary>
    /// 画像非存在確認If文（DI対応）
    /// </summary>
    [DirectCommand(DirectCommandRegistry.CommandTypes.IfImageNotExist, "画像非存在確認", "Condition", "指定した画像が存在しない場合に子コマンドを実行します")]
    public class IfImageNotExistCommand : IfCommand, IIfImageNotExistCommand
    {
        [SettingProperty("画像ファイル", SettingControlType.FilePicker,
            description: "確認する画像ファイル",
            category: "基本設定",
            isRequired: true,
            fileFilter: "画像ファイル (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp")]
        public string ImagePath { get; set; } = string.Empty;

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

        public IfImageNotExistCommand(AutoTool.Command.Interface.ICommand? parent = null, IServiceProvider? serviceProvider = null)
            : base(parent, serviceProvider)
        {
            Description = "画像非存在確認";
            _imageProcessingService = GetService<IImageProcessingService>();
        }

        protected override void ValidateFiles()
        {
            if (!string.IsNullOrEmpty(ImagePath))
            {
                _logger?.LogDebug("[ValidateFiles] ImagePath検証開始: {ImagePath}", ImagePath);
                ValidateFileExists(ImagePath, "画像ファイル");
                _logger?.LogDebug("[ValidateFiles] ImagePath検証成功: {ImagePath}", ImagePath);
            }
        }

        protected override async Task<bool> EvaluateConditionAsync(CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(ImagePath)) 
            {
                LogMessage("画像パスが設定されていません");
                return false;
            }

            if (_imageProcessingService == null)
            {
                LogMessage("画像処理サービスが利用できません");
                return false;
            }

            // 相対パスを解決して実際の検索に使用
            var resolvedImagePath = ResolvePath(ImagePath);
            _logger?.LogDebug("[EvaluateConditionAsync] IfImageNotExist 解決されたImagePath: {OriginalPath} -> {ResolvedPath}", ImagePath, resolvedImagePath);

            LogMessage($"画像の非存在を確認中: {Path.GetFileName(resolvedImagePath)}");

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
                LogMessage("画像が見つかりませんでした（条件: 真）");
                return true;
            }

            LogMessage($"画像が見つかりました（条件: 偽）: ({point.Value.X}, {point.Value.Y})");
            return false;
        }
    }

    /// <summary>
    /// AI画像存在確認If文（DI対応）
    /// </summary>
    [DirectCommand(DirectCommandRegistry.CommandTypes.IfImageExistAI, "AI画像存在確認", "Condition", "指定したAIモデルで画像が存在する場合に子コマンドを実行します")]
    public class IfImageExistAICommand : IfCommand, IIfImageExistAICommand
    {
        [SettingProperty("ONNXモデルファイル", SettingControlType.FilePicker,
            description: "使用するONNXモデルファイル",
            category: "基本設定",
            isRequired: true,
            fileFilter: "ONNXファイル (*.onnx)|*.onnx")]
        public string ModelPath { get; set; } = string.Empty;

        [SettingProperty("クラスID", SettingControlType.NumberBox,
            description: "検出対象のクラスID",
            category: "基本設定",
            isRequired: true,
            defaultValue: 0)]
        public int ClassID { get; set; } = 0;

        [SettingProperty("ウィンドウタイトル", SettingControlType.WindowPicker,
            description: "対象ウィンドウのタイトル",
            category: "ウィンドウ")]
        public string WindowTitle { get; set; } = string.Empty;

        [SettingProperty("ウィンドウクラス名", SettingControlType.TextBox,
            description: "対象ウィンドウのクラス名",
            category: "ウィンドウ")]
        public string WindowClassName { get; set; } = string.Empty;

        [SettingProperty("信頼度閾値", SettingControlType.Slider,
            description: "検出の信頼度閾値",
            category: "詳細設定",
            defaultValue: 0.3,
            minValue: 0.0,
            maxValue: 1.0)]
        public double ConfThreshold { get; set; } = 0.3;
        [SettingProperty("IoU閾値", SettingControlType.Slider,
            description: "Non-Maximum SuppressionのIoU閾値",
            category: "詳細設定",
            defaultValue: 0.5,
            minValue: 0.0,
            maxValue: 1.0)]
        public double IoUThreshold { get; set; } = 0.5;

        public IfImageExistAICommand(AutoTool.Command.Interface.ICommand? parent = null, IServiceProvider? serviceProvider = null)
            : base(parent, serviceProvider)
        {
            Description = "AI画像存在確認";
        }

        protected override void ValidateFiles()
        {
            ValidateFileExists(ModelPath, "ONNXモデルファイル");
        }

        protected override async Task<bool> EvaluateConditionAsync(CancellationToken cancellationToken)
        {
            // 相対パスを解決して実際のモデル読み込みに使用
            var resolvedModelPath = ResolvePath(ModelPath);
            _logger?.LogDebug("[EvaluateConditionAsync] IfImageExistAI 解決されたModelPath: {OriginalPath} -> {ResolvedPath}", ModelPath, resolvedModelPath);

            LogMessage($"AI検出を開始中: ClassID {ClassID}");

            try
            {
                YoloWin.Init(resolvedModelPath, 640, true);

                // AI検出は即座に実行し、ループやタイムアウトは行わない
                var det = YoloWin.DetectFromWindowTitle(WindowTitle, (float)ConfThreshold, (float)IoUThreshold).Detections;

                if (det.Count > 0)
                {
                    var best = det.OrderByDescending(d => d.Score).FirstOrDefault();

                    if (best.ClassId == ClassID)
                    {
                        LogMessage($"AI画像が見つかりました（条件: 真）: ({best.Rect.X}, {best.Rect.Y}) ClassId: {best.ClassId}");
                        return true;
                    }
                }

                LogMessage("AI画像が見つかりませんでした（条件: 偽）");
                return false;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "[EvaluateConditionAsync] AI検出エラー: ModelPath={ModelPath}, ClassID={ClassID}", resolvedModelPath, ClassID);
                LogMessage($"AI検出エラー: {ex.Message}");
                return false;
            }
        }
    }

    /// <summary>
    /// AI画像非存在確認If文（DI対応）
    /// </summary>
    [DirectCommand(DirectCommandRegistry.CommandTypes.IfImageNotExistAI, "AI画像非存在確認", "Condition", "指定したAIモデルで画像が存在しない場合に子コマンドを実行します")]
    public class IfImageNotExistAICommand : IfCommand, IIfImageNotExistAICommand
    {
        [SettingProperty("ONNXモデルファイル", SettingControlType.FilePicker,
            description: "使用するONNXモデルファイル",
            category: "基本設定",
            isRequired: true,
            fileFilter: "ONNXファイル (*.onnx)|*.onnx")]
        public string ModelPath { get; set; } = string.Empty;

        [SettingProperty("クラスID", SettingControlType.NumberBox,
            description: "検出対象のクラスID",
            category: "基本設定",
            isRequired: true,
            defaultValue: 0)]
        public int ClassID { get; set; } = 0;

        [SettingProperty("ウィンドウタイトル", SettingControlType.WindowPicker,
            description: "対象ウィンドウのタイトル",
            category: "ウィンドウ")]
        public string WindowTitle { get; set; } = string.Empty;

        [SettingProperty("ウィンドウクラス名", SettingControlType.TextBox,
            description: "対象ウィンドウのクラス名",
            category: "ウィンドウ")]
        public string WindowClassName { get; set; } = string.Empty;

        [SettingProperty("信頼度閾値", SettingControlType.Slider,
            description: "検出の信頼度閾値",
            category: "詳細設定",
            defaultValue: 0.3,
            minValue: 0.0,
            maxValue: 1.0)]
        public double ConfThreshold { get; set; } = 0.3;
        [SettingProperty("IoU閾値", SettingControlType.Slider,
            description: "Non-Maximum SuppressionのIoU閾値",
            category: "詳細設定",
            defaultValue: 0.5,
            minValue: 0.0,
            maxValue: 1.0)]
        public double IoUThreshold { get; set; } = 0.5;

        public IfImageNotExistAICommand(AutoTool.Command.Interface.ICommand? parent = null, IServiceProvider? serviceProvider = null)
            : base(parent, serviceProvider)
        {
            Description = "AI画像非存在確認";
        }

        protected override void ValidateFiles()
        {
            ValidateFileExists(ModelPath, "ONNXモデルファイル");
        }

        protected override async Task<bool> EvaluateConditionAsync(CancellationToken cancellationToken)
        {
            // 相対パスを解決して実際のモデル読み込みに使用
            var resolvedModelPath = ResolvePath(ModelPath);
            _logger?.LogDebug("[EvaluateConditionAsync] IfImageNotExistAI 解決されたModelPath: {OriginalPath} -> {ResolvedPath}", ModelPath, resolvedModelPath);

            LogMessage($"AI非検出を確認中: ClassID {ClassID}");

            try
            {
                YoloWin.Init(resolvedModelPath, 640, true);

                // AI検出は即座に実行し、ループやタイムアウトは行わない
                var det = YoloWin.DetectFromWindowTitle(WindowTitle, (float)ConfThreshold, (float)IoUThreshold).Detections;

                // 指定クラスIDが検出されなかった場合に子コマンド実行
                var targetDetections = det.Where(d => d.ClassId == ClassID).ToList();

                if (targetDetections.Count == 0)
                {
                    LogMessage($"AI画像が見つかりませんでした（条件: 真）: ClassID {ClassID}");
                    return true;
                }

                LogMessage($"AI画像が見つかりました（条件: 偽）: ClassID {ClassID}");
                return false;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "[EvaluateConditionAsync] AI非検出エラー: ModelPath={ModelPath}, ClassID={ClassID}", resolvedModelPath, ClassID);
                LogMessage($"AI非検出エラー: {ex.Message}");
                return false;
            }
        }
    }

    /// <summary>
    /// 変数条件確認If文（DI対応）
    /// </summary>
    [DirectCommand(DirectCommandRegistry.CommandTypes.IfVariable, "変数条件確認", "Condition", "変数の値に基づいて条件判定を行います")]
    public class IfVariableCommand : IfCommand, IIfVariableCommand
    {
        [SettingProperty("変数名", SettingControlType.TextBox,
            description: "設定する変数の名前",
            category: "基本設定",
            isRequired: true)]
        public string Name { get; set; } = string.Empty;

        [SettingProperty("比較演算子", SettingControlType.ComboBox,
            description: "比較に使用する演算子",
            category: "基本設定",
            sourceCollection: "ComparisonOperators",
            defaultValue: "==")]
        public string Operator { get; set; } = "==";

        [SettingProperty("比較値", SettingControlType.TextBox,
            description: "比較する値",
            category: "基本設定")]
        public string Value { get; set; } = string.Empty;

        private readonly IVariableStoreService? _variableStore;

        public IfVariableCommand(AutoTool.Command.Interface.ICommand? parent = null, IServiceProvider? serviceProvider = null)
            : base(parent, serviceProvider)
        {
            Description = "変数条件確認";
            _variableStore = GetService<IVariableStoreService>();
        }

        protected override async Task<bool> EvaluateConditionAsync(CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(Name)) 
            {
                LogMessage("変数名が設定されていません");
                return false;
            }

            var lhs = _variableStore?.Get(Name) ?? string.Empty;
            var rhs = Value ?? string.Empty;

            LogMessage($"変数条件を評価中: {Name}({lhs}) {Operator} {rhs}");

            bool result = Evaluate(lhs, rhs, Operator);
            
            LogMessage($"変数条件の結果: {Name}({lhs}) {Operator} {rhs} => {(result ? "真" : "偽")}");

            return result;
        }

        private static bool Evaluate(string lhs, string rhs, string op)
        {
            op = (op ?? "").Trim();
            if (double.TryParse(lhs, out var lnum) && double.TryParse(rhs, out var rnum))
            {
                return op switch
                {
                    "==" => lnum == rnum,
                    "!=" => lnum != rnum,
                    ">" => lnum > rnum,
                    "<" => lnum < rnum,
                    ">=" => lnum >= rnum,
                    "<=" => lnum <= rnum,
                    _ => throw new Exception($"不明な数値比較演算子です: {op}"),
                };
            }
            else
            {
                return op switch
                {
                    "==" => string.Equals(lhs, rhs, StringComparison.Ordinal),
                    "!=" => !string.Equals(lhs, rhs, StringComparison.Ordinal),
                    "Contains" => lhs.Contains(rhs, StringComparison.Ordinal),
                    "StartsWith" => lhs.StartsWith(rhs, StringComparison.Ordinal),
                    "EndsWith" => lhs.EndsWith(rhs, StringComparison.Ordinal),
                    "IsEmpty" => string.IsNullOrEmpty(lhs),
                    "IsNotEmpty" => !string.IsNullOrEmpty(lhs),
                    _ => throw new Exception($"不明な文字列比較演算子です: {op}"),
                };
            }
        }
    }
}