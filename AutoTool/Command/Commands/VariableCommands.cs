using AutoTool.Command.Base;
using AutoTool.Command.Definition;
using AutoTool.Command.Interface;
using AutoTool.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using YoloWinLib;

namespace AutoTool.Command.Commands
{
    // インターフェース定義
    public interface ISetVariableCommand : AutoTool.Command.Interface.ICommand 
    {
        string Name { get; set; }
        string Value { get; set; }
    }

    public interface ISetVariableAICommand : AutoTool.Command.Interface.ICommand 
    {
        string ModelPath { get; set; }
        int ClassID { get; set; }
        string WindowTitle { get; set; }
        string WindowClassName { get; set; }
        double ConfThreshold { get; set; }
        double IoUThreshold { get; set; }
        string Name { get; set; }
        string AIDetectMode { get; set; }
    }

    // 実装クラス
    /// <summary>
    /// 変数設定コマンド
    /// </summary>
    [DirectCommand(DirectCommandRegistry.CommandTypes.SetVariable, "変数設定", "Variable", "変数に値を設定します")]
    public class SetVariableCommand : BaseCommand, ISetVariableCommand
    {
        [SettingProperty("変数名", SettingControlType.TextBox,
            description: "設定する変数の名前",
            category: "基本設定",
            isRequired: true)]
        public string Name { get; set; } = string.Empty;

        [SettingProperty("値", SettingControlType.TextBox,
            description: "変数に設定する値",
            category: "基本設定")]
        public string Value { get; set; } = string.Empty;

        private readonly IVariableStoreService? _variableStore;

        public SetVariableCommand(AutoTool.Command.Interface.ICommand? parent = null, IServiceProvider? serviceProvider = null)
            : base(parent, serviceProvider)
        {
            Description = "変数設定";
            _variableStore = GetService<IVariableStoreService>();
        }

        protected override async Task<bool> DoExecuteAsync(CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(Name)) return false;

            _variableStore?.Set(Name, Value);
            LogMessage($"変数を設定しました。{Name} = \"{Value}\"");
            return true;
        }
    }

    /// <summary>
    /// AI変数設定コマンド
    /// </summary>
    [DirectCommand(DirectCommandRegistry.CommandTypes.SetVariableAI, "AI変数設定", "Variable", "AIモデルで検出した結果を変数に設定します")]
    public class SetVariableAICommand : BaseCommand, ISetVariableAICommand
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

        [SettingProperty("変数名", SettingControlType.TextBox,
            description: "設定する変数の名前",
            category: "基本設定",
            isRequired: true)]
        public string Name { get; set; } = string.Empty;

        [SettingProperty("検出モード", SettingControlType.ComboBox,
            description: "変数に設定する内容のモード",
            category: "基本設定",
            sourceCollection: "AIDetectModes",
            defaultValue: "Class")]
        public string AIDetectMode { get; set; } = "Class"; // "Class" or "Count"

        private readonly IVariableStoreService? _variableStore;

        public SetVariableAICommand(AutoTool.Command.Interface.ICommand? parent = null, IServiceProvider? serviceProvider = null)
            : base(parent, serviceProvider)
        {
            Description = "AI変数設定";
            _variableStore = GetService<IVariableStoreService>();
        }

        protected override void ValidateFiles()
        {
            ValidateFileExists(ModelPath, "ONNXモデルファイル");
        }

        protected override async Task<bool> DoExecuteAsync(CancellationToken cancellationToken)
        {
            // 相対パスを解決して実際のモデル読み込みに使用
            var resolvedModelPath = ResolvePath(ModelPath);
            _logger?.LogDebug("[DoExecuteAsync] SetVariableAI 解決されたModelPath: {OriginalPath} -> {ResolvedPath}", ModelPath, resolvedModelPath);

            YoloWin.Init(resolvedModelPath, 640, true);

            var det = YoloWin.DetectFromWindowTitle(WindowTitle, (float)ConfThreshold, (float)IoUThreshold).Detections;

            if (det.Count == 0)
            {
                _variableStore?.Set(Name, "-1");
                LogMessage($"画像が見つかりませんでした。{Name}に-1をセットしました。");
            }
            else
            {
                switch (AIDetectMode)
                {
                    case "Class":
                        // 最高スコアのものをセット
                        var best = det.OrderByDescending(d => d.Score).FirstOrDefault();
                        _variableStore?.Set(Name, best.ClassId.ToString());
                        LogMessage($"画像が見つかりました。{Name}に{best.ClassId}をセットしました。");
                        break;
                    case "Count":
                        // 検出された数をセット
                        _variableStore?.Set(Name, det.Count.ToString());
                        LogMessage($"画像が{det.Count}個見つかりました。{Name}に{det.Count}をセットしました。");
                        break;
                    default:
                        throw new Exception($"不明なモードです: {AIDetectMode}");
                }
            }

            return true;
        }
    }
}