using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace MacroPanels.Command.Interface
{

    /// <summary>
    /// コマンドのインターフェース
    /// </summary>
    public interface ICommand
    {
        /// <summary>
        /// 行番号
        /// </summary>
        int LineNumber { get; set; }

        /// <summary>
        /// 親コマンド
        /// </summary>
        ICommand? Parent { get; }

        /// <summary>
        /// 子コマンド
        /// </summary>
        IEnumerable<ICommand> Children { get; }

        /// <summary>
        /// ネストレベル
        /// </summary>
        int NestLevel { get; set; }

        /// <summary>
        /// 設定オブジェクト
        /// </summary>
        object? Settings { get; set; }

        /// <summary>
        /// 説明
        /// </summary>
        string Description { get; }

        /// <summary>
        /// 有効/無効
        /// </summary>
        bool IsEnabled { get; set; }

        /// <summary>
        /// コマンドを実行
        /// </summary>
        Task<bool> Execute(CancellationToken cancellationToken);

        /// <summary>
        /// 子コマンドを追加
        /// </summary>
        void AddChild(ICommand child);

        /// <summary>
        /// 子コマンドを削除
        /// </summary>
        void RemoveChild(ICommand child);

        /// <summary>
        /// 子コマンドを取得
        /// </summary>
        IEnumerable<ICommand> GetChildren();

        /// <summary>
        /// コマンド開始時のイベント
        /// </summary>
        event System.EventHandler? OnStartCommand;
    }

    public interface IRootCommand : ICommand { }
    public interface IIfCommand : ICommand { }
    public interface IWaitImageCommand : ICommand { new IWaitImageCommandSettings Settings { get; } }
    public interface IClickImageCommand : ICommand { new IClickImageCommandSettings Settings { get; } }
    public interface IHotkeyCommand : ICommand { new IHotkeyCommandSettings Settings { get; } }
    public interface IClickCommand : ICommand { new IClickCommandSettings Settings { get; } }
    public interface IWaitCommand : ICommand { new IWaitCommandSettings Settings { get; } }
    public interface IIfImageExistCommand : ICommand, IIfCommand { new IIfImageCommandSettings Settings { get; } }
    public interface IIfImageNotExistCommand : ICommand, IIfCommand { new IIfImageCommandSettings Settings { get; } }
    public interface ILoopCommand : ICommand { new ILoopCommandSettings Settings { get; } }
    public interface IEndLoopCommand : ICommand { new ILoopEndCommandSettings Settings { get; } }
    public interface ILoopBreakCommand : ICommand { }
    public interface IIfImageExistAICommand : ICommand, IIfCommand { new IIfImageExistAISettings Settings { get; } }
    public interface IIfImageNotExistAICommand : ICommand, IIfCommand { new IIfImageNotExistAISettings Settings { get; } } // Use proper interface
    public interface IClickImageAICommand : ICommand { new IClickImageAICommandSettings Settings { get; } }
    public interface IExecuteCommand : ICommand { new IExecuteCommandSettings Settings { get; } }
    public interface ISetVariableCommand : ICommand { new ISetVariableCommandSettings Settings { get; } }
    public interface ISetVariableAICommand : ICommand { new ISetVariableAICommandSettings Settings { get; } }
    public interface IIfVariableCommand : ICommand, IIfCommand { new IIfVariableCommandSettings Settings { get; } }
    public interface IScreenshotCommand : ICommand { new IScreenshotCommandSettings Settings { get; } }
}
