using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Win32;
using Panels.Model.List.Interface;
using Panels.Model.List.Type;

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
                return Items[index];
            }
            set
            {
                Items[index] = value;
            }
        }

        public void Add(ICommandListItem item)
        {
            Items.Add(item);

            ReorderItems();
            CalculateNestLevel();
            PairIfItems();
            PairLoopItems();
        }

        public void Remove(ICommandListItem item)
        {
            Items.Remove(item);

            ReorderItems();
            CalculateNestLevel();
            PairIfItems();
            PairLoopItems();
        }

        public void Insert(int index, ICommandListItem item)
        {
            Items.Insert(index, item);

            ReorderItems();
            CalculateNestLevel();
            PairIfItems();
            PairLoopItems();
        }

        public void Override(int index, ICommandListItem item)
        {
            if(item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            if(index < 0 || index >= Items.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            Items[index] = item;

            ReorderItems();
            CalculateNestLevel();
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
            CalculateNestLevel();
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
            CalculateNestLevel();
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

        public void CalculateNestLevel()
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
                if (item.ItemType == ItemType.IfImageExistAI)
                {
                    nestLevel++;
                }
                if (item.ItemType == ItemType.IfImageNotExistAI)
                {
                    nestLevel++;
                }
            }
        }

        public void PairIfItems()
        {
            var ifItems = Items.OfType<IIfItem>().Where(x => x.ItemType == ItemType.IfImageExist || x.ItemType == ItemType.IfImageNotExist || x.ItemType == ItemType.IfImageExistAI || x.ItemType == ItemType.IfImageNotExistAI).OrderBy(x => x.LineNumber).ToList();
            var endIfItems = Items.OfType<IEndIfItem>().Where(x => x.ItemType == ItemType.EndIf).OrderBy(x => x.LineNumber).ToList();

            foreach (var ifItem in ifItems)
            {
                if(ifItem.Pair != null)
                {
                    continue;
                }

                foreach(var endIfItem in endIfItems)
                {
                    if (endIfItem.Pair != null)
                    {
                        continue;
                    }

                    if (endIfItem.NestLevel == ifItem.NestLevel &&　endIfItem.LineNumber > ifItem.LineNumber)
                    {
                        ifItem.Pair = endIfItem;
                        endIfItem.Pair = ifItem;
                        break;
                    }
                }
            }
        }

        public void PairLoopItems()
        {
            var loopItems = Items.OfType<ILoopItem>().Where(x => x.ItemType == ItemType.Loop).OrderBy(x => x.LineNumber).ToList();
            var endLoopItems = Items.OfType<IEndLoopItem>().Where(x => x.ItemType == ItemType.EndLoop).OrderBy(x => x.LineNumber).ToList();

            foreach (var loopItem in loopItems)
            {
                if(loopItem.Pair != null)
                {
                    continue;
                }

                foreach (var endLoopItem in endLoopItems)
                {
                    if (endLoopItem.Pair != null)
                    {
                        continue;
                    }

                    if (endLoopItem.NestLevel == loopItem.NestLevel && endLoopItem.LineNumber > loopItem.LineNumber)
                    {
                        loopItem.Pair = endLoopItem;
                        endLoopItem.Pair = loopItem;
                        break;
                    }
                }
            }
        }

        public IEnumerable<ICommandListItem> Clone()
        {
            var clone = new List<ICommandListItem>();

            foreach (var item in Items)
            {
                clone.Add(item.Clone());
            }

            return clone;
        }

        public void Save(string filePath)
        {
            var cloneItems = Clone();

            JsonSerializerHelper.SerializeToFile(cloneItems, filePath);
        }

        public void Load(string filePath)
        {
            var deserializedItems = JsonSerializerHelper.DeserializeFromFile<ObservableCollection<ICommandListItem>>(filePath);
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
                        case nameof(ItemType.Break):
                            Add(new BreakItem(item as BreakItem));
                            break;
                        case nameof(ItemType.IfImageExist):
                            Add(new IfImageExistItem(item as IfImageExistItem));
                            break;
                        case nameof(ItemType.IfImageNotExist):
                            Add(new IfImageNotExistItem(item as IfImageNotExistItem));
                            break;
                        case nameof(ItemType.EndIf):
                            Add(new EndIfItem(item as EndIfItem));
                            break;
                        case nameof(ItemType.IfImageExistAI):
                            Add(new IfImageExistAIItem(item as IfImageExistAIItem));
                            break;
                        case nameof(ItemType.IfImageNotExistAI):
                            Add(new IfImageNotExistAIItem(item as IfImageNotExistAIItem));
                            break;
                    }

                }
            }

            CalculateNestLevel();
            PairIfItems();
            PairLoopItems();
        }
    }
}
