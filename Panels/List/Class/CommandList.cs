using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Win32;
using Panels.List.Interface;
using Panels.List.Type;

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
            PairIfItems();
            PairLoopItems();
        }

        public void Remove(ICommandListItem item)
        {
            Items.Remove(item);

            ReorderItems();
            CalcurateNestLevel();
            PairIfItems();
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
            PairIfItems();
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
            PairIfItems();
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
                if(item.ItemType == ItemType.EndIf)
                {
                    nestLevel--;
                }

                item.NestLevel = nestLevel;

                if (item.ItemType == ItemType.Loop)
                {
                    nestLevel++;
                }
                if (item.ItemType == ItemType.IfImageExist)
                {
                    nestLevel++;
                }
                if (item.ItemType == ItemType.IfImageNotExist)
                {
                    nestLevel++;
                }
            }
        }

        public void PairIfItems()
        {
            var ifItems = Items.OfType<IIfItem>().Where(x => x.ItemType == ItemType.IfImageExist || x.ItemType == ItemType.IfImageNotExist).ToList();
            var endIfItems = Items.OfType<IEndIfItem>().Where(x => x.ItemType == ItemType.EndIf).ToList();

            foreach (var ifItem in ifItems)
            {
                var endIfItem = endIfItems.FirstOrDefault(x => x.NestLevel == ifItem.NestLevel);

                if (endIfItem != null)
                {
                    ifItem.Pair = endIfItem;
                }
            }
        }

        public void PairLoopItems()
        {
            var loopItems = Items.OfType<ILoopItem>().Where(x => x.ItemType == ItemType.Loop).ToList();
            var endLoopItems = Items.OfType<IEndLoopItem>().Where(x => x.ItemType == ItemType.EndLoop).ToList();

            foreach (var loopItem in loopItems)
            {
                var endLoopItem = endLoopItems.FirstOrDefault(x => x.NestLevel == loopItem.NestLevel);

                if (endLoopItem != null)
                {
                    loopItem.Pair = endLoopItem;
                }
            }
        }

        public void Save()
        {
            var dialog = new SaveFileDialog();
            dialog.Filter = "Macro files (*.macro)|*.macro|All files (*.*)|*.*";
            dialog.FilterIndex = 1;
            dialog.RestoreDirectory = true;
            dialog.FileName = "CommandList.macro";
            dialog.DefaultExt = ".macro";
            dialog.Title = "Save Macro File";
            dialog.ShowDialog();

            if (dialog.FileName == "")
            {
                return;
            }

            JsonSerializerHelper.SerializeToFile(Items, dialog.FileName);
        }

        public void Load()
        {
            var dialog = new OpenFileDialog();
            dialog.Filter = "Macro files (*.macro)|*.macro|All files (*.*)|*.*";
            dialog.FilterIndex = 1;
            dialog.RestoreDirectory = true;
            dialog.FileName = "CommandList.macro";
            dialog.DefaultExt = ".macro";
            dialog.Title = "Load Macro File";
            dialog.ShowDialog();

            if (dialog.FileName == "")
            {
                return;
            }

            var deserializedItems = JsonSerializerHelper.DeserializeFromFile<ObservableCollection<ICommandListItem>>(dialog.FileName);
            if (deserializedItems != null)
            {
                Items.Clear();

                foreach (var item in deserializedItems)
                {
                    switch(item.ItemType)
                    {
                        case nameof(ItemType.WaitImage):
                            Add(new WaitImageItem(item as WaitImageItem));
                            break;
                        case nameof(ItemType.ClickImage):
                            Add(new ClickImageItem(item as ClickImageItem));
                            break;
                        case nameof(ItemType.Click):
                            Add(new ClickItem(item as ClickItem));
                            break;
                        case nameof(ItemType.Hotkey):
                            Add(new HotkeyItem(item as HotkeyItem));
                            break;
                        case nameof(ItemType.Wait):
                            Add(new WaitItem(item as WaitItem));
                            break;
                        case nameof(ItemType.Loop):
                            Add(new LoopItem(item as LoopItem));
                            break;
                        case nameof(ItemType.EndLoop):
                            Add(new EndLoopItem(item as EndLoopItem));
                            break;
                    }

                }
            }
        }
    }
}
