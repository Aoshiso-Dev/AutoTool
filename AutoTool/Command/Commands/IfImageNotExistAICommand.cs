using AutoTool.Command.Base;
using AutoTool.Command.Definition;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using YoloWinLib;
using System.ComponentModel;

namespace AutoTool.Command.Commands
{
    [AutoToolCommand(nameof(IfImageNotExistAICommand), typeof(IfImageNotExistAICommand))]
    public class IfImageNotExistAICommand : IfCommand
    {
        [Category("基本設定"), DisplayName("ONNXモデルファイル")]
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

        public IfImageNotExistAICommand(IAutoToolCommand? parent = null, IServiceProvider? serviceProvider = null)
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
            var resolvedModelPath = ResolvePath(ModelPath);
            _logger?.LogDebug("[EvaluateConditionAsync] IfImageNotExistAI 解決されたModelPath: {OriginalPath} -> {ResolvedPath}", ModelPath, resolvedModelPath);

            LogMessage($"AI非検出を確認中: ClassID {ClassID}");

            try
            {
                YoloWin.Init(resolvedModelPath, 640, true);

                var det = YoloWin.DetectFromWindowTitle(WindowTitle, (float)ConfThreshold, (float)IoUThreshold).Detections;

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
}
