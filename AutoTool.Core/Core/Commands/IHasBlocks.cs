using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoTool.Core.Commands
{
    /// <summary>
    /// 複合コマンド（IF/ループ/並列など）が持つ“ブロック”の集合。
    /// Then/ElseやBodyなど、名前付きの子コレクションを統一的に扱う。
    /// </summary>
    public interface IHasBlocks
    {
        ObservableCollection<CommandBlock> Blocks { get; }
    }
}
