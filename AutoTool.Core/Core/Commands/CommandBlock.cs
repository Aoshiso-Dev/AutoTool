using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AutoTool.Core.Commands
{
    /// <summary>名前付きブロック（UIはこれをTreeViewで描画し、D&Dで編集）。</summary>
    public sealed class CommandBlock : ObservableObject
    {
        public string Name { get; } // "Then", "Else", "Body", "Branch 1" など

        public ObservableCollection<IAutoToolCommand> Children { get; set; }

        public CommandBlock(string name, IEnumerable<IAutoToolCommand>? initial = null)
        {
            Name = name;
            Children = new ObservableCollection<IAutoToolCommand>(initial ?? Enumerable.Empty<IAutoToolCommand>());
            
            // Subscribe to collection changes to notify UI
            Children.CollectionChanged += (s, e) => 
            {
                // Notify that Children property has changed
                OnPropertyChanged(nameof(Children));
                
                // Also notify with empty string to force complete refresh of the object
                OnPropertyChanged("");
            };
        }
    }
}
