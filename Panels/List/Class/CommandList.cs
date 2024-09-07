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
            }
        }

        public void Add(ICommandListItem item)
        {
            Items.Add(item);

            ReorderItems();
            CalcurateNestLevel();
            PairLoopItems();
        }

        public void Remove(ICommandListItem item)
        {
            Items.Remove(item);

            ReorderItems();
            CalcurateNestLevel();
            PairLoopItems();
        }

        public void Clear()
        {
            Items.Clear();
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

            ReorderItems();
            CalcurateNestLevel();
            PairLoopItems();
        }

        public void Copy(int oldIndex, int newIndex)
        {
            if (oldIndex < 0 || oldIndex >= Items.Count || newIndex < 0 || newIndex >= Items.Count)
            {
                return;
            }

            var item = Items[oldIndex];
            Items.Insert(newIndex, item);

            ReorderItems();
            CalcurateNestLevel();
            PairLoopItems();
        }

        public void ReorderItems()
        {
            foreach (var item in Items)
            {
                item.LineNumber = Items.IndexOf(item) + 1;
            }
        }

        public void CalcurateNestLevel()
        {
            var nestLevel = 0;

            foreach (var item in Items)
            {
                if (item.ItemType == ItemType.EndLoop)
                {
                    nestLevel--;
                }

                item.NestLevel = nestLevel;

                if (item.ItemType == ItemType.Loop)
                {
                    nestLevel++;
                }

            }
            /*
            nestLevel = 0;
            var startLoopItems = Items.OfType<ILoopItem>().Where(x => x.ItemType == ItemType.Loop).ToList();
            foreach (var startLoopItem in startLoopItems)
            {
                startLoopItem.NestLevel = nestLevel;
                nestLevel++;
            }

            nestLevel = 0;
            var endLoopItems = Items.OfType<IEndLoopItem>().Where(x => x.ItemType == ItemType.EndLoop).Reverse().ToList();
            foreach (var endLoopItem in endLoopItems)
            {
                endLoopItem.NestLevel = nestLevel;
                nestLevel++;
            }
            */
        }

        public void PairLoopItems()
        {
            var loopItems = Items.OfType<ILoopItem>().Where(x => x.ItemType == ItemType.Loop).ToList();
            var endLoopItems = Items.OfType<IEndLoopItem>().Where(x => x.ItemType == ItemType.EndLoop).ToList();

            foreach (var loopItem in loopItems)
            {
                var endLoopItem = endLoopItems.FirstOrDefault(x => x.NestLevel == loopItem.NestLevel && x.Pair == null);

                if (endLoopItem != null)
                {
                    loopItem.Pair = endLoopItem;
                    endLoopItem.Pair = loopItem;
                }
            }
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
