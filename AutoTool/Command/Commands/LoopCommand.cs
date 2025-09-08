using System;
using System.Threading;
using System.Threading.Tasks;
using System.ComponentModel;
using AutoTool.Command.Base;
using AutoTool.Command.Definition;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AutoTool.Command.Commands
{
    [AutoToolCommand(nameof(LoopCommand), typeof(LoopCommand))]
    public class LoopCommand : BaseCommand
    {
        [Category("基本設定"), DisplayName("ループ回数")]
        public int LoopCount { get; set; } = 1;

        public LoopCommand(IAutoToolCommand? parent = null, IServiceProvider? serviceProvider = null)
            : base(parent, serviceProvider)
        {
            Description = "ループ";
        }

        protected override async Task<bool> DoExecuteAsync(CancellationToken cancellationToken)
        {
            LogMessage($"ループを開始します({LoopCount}回)");

            for (int i = 0; i < LoopCount; i++)
            {
                if (cancellationToken.IsCancellationRequested) return false;

                ResetChildrenProgress();

                try
                {
                    var result = await ExecuteChildrenAsync(cancellationToken);
                    if (!result) return false;
                }
                catch (LoopBreakException)
                {
                    LogMessage($"ループが途中で中断されました (現在: {i + 1}/{LoopCount})");
                    break;
                }

                ReportProgress(i + 1, LoopCount);
            }

            LogMessage("ループが完了しました");
            return true;
        }

        protected new async Task<bool> ExecuteChildrenAsync(CancellationToken cancellationToken)
        {
            foreach (var child in Children)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (child is BaseCommand baseChild && _executionContext != null)
                {
                    baseChild.SetExecutionContext(_executionContext);
                }

                _logger?.LogDebug("[LoopCommand.ExecuteChildrenAsync] 子コマンド実行: {ChildType} (Line: {LineNumber})", child.GetType().Name, child.LineNumber);

                try
                {
                    var result = await child.Execute(cancellationToken);
                    if (!result)
                        return false;
                }
                catch (LoopBreakException)
                {
                    throw;
                }
            }
            return true;
        }
    }
}
