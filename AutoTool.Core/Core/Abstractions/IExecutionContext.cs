using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoTool.Core.Abstractions
{
    /// <summary>
    /// 実行時のサービス/状態の入口。DIルートを渡すのではなく、必要な機能に限定。
    /// </summary>
    public interface IExecutionContext
    {
        IValueResolver ValueResolver { get; }
        IVariableScope Variables { get; }
        ILogger Logger { get; }                // 任意のロガー抽象
        CancellationToken ShutdownToken { get; } // 全体停止と連動
        Task DelayAsync(TimeSpan delay, CancellationToken ct);
    }
}
