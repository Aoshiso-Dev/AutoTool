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
    /// 条件評価のインターフェース
    /// </summary>
    public interface ICondition
    {
        /// <summary>
        /// 条件を評価
        /// </summary>
        /// <param name="cancellationToken">キャンセレーショントークン</param>
        /// <returns>条件が真の場合true、偽の場合false</returns>
        Task<bool> Evaluate(CancellationToken cancellationToken);
    }
}
