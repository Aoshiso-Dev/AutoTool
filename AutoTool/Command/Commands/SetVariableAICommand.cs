using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.ComponentModel;
using AutoTool.Command.Base;
using AutoTool.Command.Definition;
using AutoTool.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using YoloWinLib;

namespace AutoTool.Command.Commands
{
    [AutoToolCommand(nameof(SetVariableAICommand), typeof(SetVariableAICommand))]
    public class SetVariableAICommand : BaseCommand
    {
        [Category("基本設定"), DisplayName("モデルファイル")]
        public string ModelPath { get; set; } = string.Empty;

        [Category("基本設定"), DisplayName("クラスID")]
        public int ClassID { get; set; } = 0;

        [Category("ウィンドウ"), DisplayName("ウィンドウタイトル")]
        public string WindowTitle { get; set; } = string.Empty;

        [Category("ウィンドウ"), DisplayName("ウィンドウクラス名")]
        public string WindowClassName { get; set; } = string.Empty;

        [Category("詳細設定"), DisplayName("信頼度閾値")]
        public double ConfThreshold { get; set; } = 0.3;

        [Category("詳細設定"), DisplayName("IoU閾値")]
        public double IoUThreshold { get; set; } = 0.5;

        [Category("基本設定"), DisplayName("変数名")]
        public string VariableName { get; set; } = string.Empty;

        [Category("基本設定"), DisplayName("検出モード")]
        public string AIDetectMode { get; set; } = "Class"; // "Class" or "Count"

        private readonly IVariableStoreService? _variableStore;

        public SetVariableAICommand(IAutoToolCommand? parent = null, IServiceProvider? serviceProvider = null)
            : base(parent, serviceProvider)
        {
            Description = "AI変数代入";
            _variableStore = GetService<IVariableStoreService>();
        }

        protected override void ValidateFiles()
        {
            ValidateFileExists(ModelPath, "ONNXモデルファイル");
        }

        protected override async Task<bool> DoExecuteAsync(CancellationToken cancellationToken)
        {
            var resolvedModelPath = ResolvePath(ModelPath);
            _logger?.LogDebug("[DoExecuteAsync] SetVariableAI ModelPath: {Original} -> {Resolved}", ModelPath, resolvedModelPath);

            YoloWin.Init(resolvedModelPath, 640, true);

            var det = YoloWin.DetectFromWindowTitle(WindowTitle, (float)ConfThreshold, (float)IoUThreshold).Detections;

            if (det.Count == 0)
            {
                _variableStore?.Set(VariableName, "-1");
                LogMessage($"対象が検出されませんでした: {VariableName} = -1");
            }
            else
            {
                switch (AIDetectMode)
                {
                    case "Class":
                        var best = det.OrderByDescending(d => d.Score).FirstOrDefault();
                        _variableStore?.Set(VariableName, best.ClassId.ToString());
                        LogMessage($"変数に代入しました: {VariableName} = {best.ClassId}");
                        break;
                    case "Count":
                        _variableStore?.Set(VariableName, det.Count.ToString());
                        LogMessage($"変数に代入しました: {VariableName} = {det.Count}");
                        break;
                    default:
                        throw new Exception($"不明なAI検出モード: {AIDetectMode}");
                }
            }

            return true;
        }
    }
}
