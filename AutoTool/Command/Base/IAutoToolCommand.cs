using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AutoTool.Command.Base
{
    /// <summary>
    /// コマンドの基底インターフェース
    /// </summary>
    public interface IAutoToolCommand
    {
        /// <summary>
        /// 行番号
        /// </summary>
        int LineNumber { get; set; }

        /// <summary>
        /// 親コマンド
        /// </summary>
        IAutoToolCommand? Parent { get; }

        /// <summary>
        /// 子コマンド
        /// </summary>
        IEnumerable<IAutoToolCommand> Children { get; }

        /// <summary>
        /// ネストレベル
        /// </summary>
        int NestLevel { get; set; }

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
        void AddChild(IAutoToolCommand child);

        /// <summary>
        /// 子コマンドを削除
        /// </summary>
        void RemoveChild(IAutoToolCommand child);

        /// <summary>
        /// 子コマンドを取得
        /// </summary>
        IEnumerable<IAutoToolCommand> GetChildren();

        /// <summary>
        /// コマンド開始時のイベント
        /// </summary>
        event EventHandler? OnStartCommand;
    }

    // 基本的なマーカーインターフェース
    public interface IRootCommand : IAutoToolCommand { }
    public interface IIfCommand : IAutoToolCommand { }
}
