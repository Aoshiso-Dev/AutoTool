using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoTool.Core.Abstractions
{
    public enum ControlFlow
    {
        Next,       // 次のコマンドへ
        Break,      // ループを1段抜ける
        Continue,   // ループの次反復へ
        Stop,       // 実行全体を中断（ユーザー停止など）
        Error       // 失敗（以降中止）
    }
}
