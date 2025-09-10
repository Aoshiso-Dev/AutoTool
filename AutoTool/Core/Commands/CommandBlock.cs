using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoTool.Core.Commands
{
    /// <summary>名前付きブロック（UIはこれをTreeViewで描画し、D&Dで編集）。</summary>
    public sealed class CommandBlock
    {
        public string Name { get; } // "Then", "Else", "Body", "Branch 1" など
        public ObservableCollection<IAutoToolCommand> Children { get; }
        public CommandBlock(string name, IEnumerable<IAutoToolCommand>? initial = null)
        {
            Name = name;
            Children = new ObservableCollection<IAutoToolCommand>(initial ?? Enumerable.Empty<IAutoToolCommand>());
        }
    }
}
