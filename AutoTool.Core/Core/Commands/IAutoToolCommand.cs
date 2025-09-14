using AutoTool.Core.Abstractions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoTool.Core.Commands
{
    /// <summary>
    /// AutoToolの実行可能な“1つのノード”。設定はスナップショットとして内包。
    /// 編集は別VMで行う前提。
    /// </summary>
    public interface IAutoToolCommand
    {
        Guid Id { get; }                // 安定ID（参照・ブレークポイント・差分用）
        string Type { get; }            // "if", "while"などの機械名
        string DisplayName { get; }     // UI表示名
        bool IsEnabled { get; set; }    // 無効化トグル

        /// <summary>コマンドを実行。ControlFlowで制御転送を表現。</summary>
        Task<ControlFlow> ExecuteAsync(CancellationToken ct);
    }
}
