using System;
using System.Threading;
using System.Threading.Tasks;
using AutoTool.Command.Base;
using AutoTool.Command.Definition;
using AutoTool.Command.Interface;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AutoTool.Command.Commands
{
    // インターフェース定義
    public interface ILoopCommand : AutoTool.Command.Interface.ICommand 
    {
        int LoopCount { get; set; }
    }

    public interface ILoopBreakCommand : AutoTool.Command.Interface.ICommand { }

    // 実装クラス
    /// <summary>
    /// ループコマンド（DI対応）
    /// </summary>
    [DirectCommand(DirectCommandRegistry.CommandTypes.Loop, "ループ", "Control", "指定した回数だけ子コマンドを繰り返し実行します")]
    public class LoopCommand : BaseCommand, ILoopCommand
    {
        [SettingProperty("ループ回数", SettingControlType.NumberBox,
            description: "繰り返し実行する回数",
            category: "基本設定",
            isRequired: true,
            defaultValue: 1)]
        public int LoopCount { get; set; } = 1;

        public LoopCommand(AutoTool.Command.Interface.ICommand? parent = null, IServiceProvider? serviceProvider = null)
            : base(parent, serviceProvider)
        {
            Description = "ループ";
        }

        protected override async Task<bool> DoExecuteAsync(CancellationToken cancellationToken)
        {
            LogMessage($"ループを開始します。({LoopCount}回)");

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
                    // LoopBreakExceptionをキャッチしてこのループを中断
                    LogMessage($"ループが中断されました。(実行回数: {i + 1}/{LoopCount})");
                    break; // このループのみを抜ける
                }

                ReportProgress(i + 1, LoopCount);
            }

            LogMessage("ループが完了しました。");
            return true;
        }

        /// <summary>
        /// 子コマンドを順次実行（LoopBreakException対応版・LineNumber同期強化）
        /// </summary>
        protected new async Task<bool> ExecuteChildrenAsync(CancellationToken cancellationToken)
        {
            foreach (var child in Children)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // 実行コンテキストを設定
                if (child is BaseCommand baseChild && _executionContext != null)
                {
                    baseChild.SetExecutionContext(_executionContext);
                }

                // 子コマンドのLineNumberは既に設定済みなので、ここでは変更しない
                // （MacroFactoryでペア再構築時に正しく設定されている）
                _logger?.LogDebug("[LoopCommand.ExecuteChildrenAsync] 子コマンド実行: {ChildType} (Line: {LineNumber})", 
                    child.GetType().Name, child.LineNumber);

                try
                {
                    var result = await child.Execute(cancellationToken);
                    if (!result)
                        return false;
                }
                catch (LoopBreakException)
                {
                    // LoopBreakExceptionは上位のLoopCommandに伝播
                    throw;
                }
            }
            return true;
        }
    }

    /// <summary>
    /// ループ中断コマンド
    /// </summary>
    [DirectCommand(DirectCommandRegistry.CommandTypes.LoopBreak, "ループ中断", "Control", "ループを中断します")]
    public class LoopBreakCommand : BaseCommand, ILoopBreakCommand
    {
        public LoopBreakCommand(AutoTool.Command.Interface.ICommand? parent = null, IServiceProvider? serviceProvider = null)
            : base(parent, serviceProvider)
        {
            Description = "ループ中断";
        }

        protected override Task<bool> DoExecuteAsync(CancellationToken cancellationToken)
        {
            LogMessage("ループ中断を実行します。");

            // LoopBreakExceptionを投げて最も内側のループのみを中断
            throw new LoopBreakException("ループ中断コマンドが実行されました");
        }
    }

    /// <summary>
    /// If終了コマンド
    /// </summary>
    [DirectCommand(DirectCommandRegistry.CommandTypes.IfEnd, "If終了", "Control", "If文の終了を示します")]
    public class IfEndCommand : BaseCommand
    {
        public IfEndCommand(AutoTool.Command.Interface.ICommand? parent = null, IServiceProvider? serviceProvider = null)
            : base(parent, serviceProvider)
        {
            Description = "If終了";
        }

        protected override Task<bool> DoExecuteAsync(CancellationToken cancellationToken)
        {
            ResetChildrenProgress();
            return Task.FromResult(true);
        }
    }

    /// <summary>
    /// ループ終了コマンド
    /// </summary>
    [DirectCommand(DirectCommandRegistry.CommandTypes.LoopEnd, "ループ終了", "Control", "ループの終了を示します")]
    public class LoopEndCommand : BaseCommand
    {
        public LoopEndCommand(AutoTool.Command.Interface.ICommand? parent = null, IServiceProvider? serviceProvider = null)
            : base(parent, serviceProvider)
        {
            Description = "ループ終了";
        }

        protected override Task<bool> DoExecuteAsync(CancellationToken cancellationToken)
        {
            ResetChildrenProgress();
            return Task.FromResult(true);
        }
    }
}