using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;

namespace Panels.Model.List.Interface
{
    internal interface ICommandList
    {
        public ObservableCollection<ICommandListItem> Items { get; }
        public void Add(ICommandListItem item);
        public void Remove(ICommandListItem item);
        public void Clear();
        public void Move(int oldIndex, int newIndex);
        public void Copy(int oldIndex, int newIndex);
        public void Save(string fileName);
        public void Load(string fileName);
    }
}
