using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Panels.List.Interface;

namespace Panels.List.Class
{
    public partial class CommandList : ObservableObject, ICommandList
    {
        [ObservableProperty]
        private ObservableCollection<ICommandListItem> _items = new();

        public ICommandListItem this[int index]
        {
            get
            {
                if (index < 0 || index >= Items.Count)
                    throw new ArgumentOutOfRangeException(nameof(index), "Index is out of range.");

                return Items[index];
            }
            set
            {
                if (index < 0 || index >= Items.Count)
                    throw new ArgumentOutOfRangeException(nameof(index), "Index is out of range.");

                Items[index] = value;
                OnPropertyChanged(nameof(Items));
            }
        }

        public void Add(ICommandListItem item)
        {
            Items.Add(item);

            foreach (var i in Items)
            {
                i.LineNumber = Items.IndexOf(i) + 1;
            }

            OnPropertyChanged(nameof(Items));
        }

        public void Remove(ICommandListItem item)
        {
            Items.Remove(item);

            for(int i = 0; i < Items.Count; i++)
            {
                Items[i].LineNumber = i + 1;
            }
        }

        public void Clear()
        {
            Items.Clear();

            OnPropertyChanged(nameof(Items));
        }

        public void Move(int oldIndex, int newIndex)
        {
            if (oldIndex < 0 || oldIndex >= Items.Count || newIndex < 0 || newIndex >= Items.Count)
            {
                return;
            }

            var item = Items[oldIndex];
            Items.RemoveAt(oldIndex);
            Items.Insert(newIndex, item);

            foreach (var i in Items)
            {
                i.LineNumber = Items.IndexOf(i) + 1;
            }

            OnPropertyChanged(nameof(Items));
        }

        public void Copy(int oldIndex, int newIndex)
        {
            if (oldIndex < 0 || oldIndex >= Items.Count || newIndex < 0 || newIndex >= Items.Count)
            {
                return;
            }

            var item = Items[oldIndex];
            Items.Insert(newIndex, item);

            foreach (var i in Items)
            {
                i.LineNumber = Items.IndexOf(i) + 1;
            }

            OnPropertyChanged(nameof(Items));
        }

        public void Save()
        {
            // TODO
        }

        public void Load()
        {
            // TODO
        }
    }
}
