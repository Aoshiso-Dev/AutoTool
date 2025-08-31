using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Win32;
using MacroPanels.Command.Interface;
using MacroPanels.Command.Class;
using MacroPanels.List.Class;
using System.Diagnostics;
using MacroPanels.Model.List.Interface;
using MacroPanels.Model.CommandDefinition;
using MacroPanels.Model.List.Type;
using System.IO;

namespace MacroPanels.List.Class
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
                if (item.ItemType == ItemType.Loop_End)
                {
                    nestLevel--;
                }
                if(item.ItemType == ItemType.IF_End)
                {
                    nestLevel--;
                }

                item.NestLevel = nestLevel;

                if (item.ItemType == ItemType.Loop)
                {
                    nestLevel++;
                }
                if (item.ItemType == ItemType.IF_ImageExist)
                {
                    nestLevel++;
                }
                if (item.ItemType == ItemType.IF_ImageNotExist)
                {
                    nestLevel++;
                }
                if (item.ItemType == ItemType.IF_ImageExist_AI)
                {
                    nestLevel++;
                }
                if (item.ItemType == ItemType.IF_ImageNotExist_AI)
                {
                    nestLevel++;
                }
                if(item.ItemType == ItemType.IF_Variable)
                {
                    nestLevel++;
                }
            }
        }

        public void PairIfItems()
        {
            var ifItems = Items.OfType<IIfItem>().Where(x => ItemType.IsIfItem(x.ItemType)).OrderBy(x => x.LineNumber).ToList();
            var endIfItems = Items.OfType<IIfEndItem>().Where(x => x.ItemType == ItemType.IF_End).OrderBy(x => x.LineNumber).ToList();

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
            var loopItems = Items.OfType<ILoopItem>().Where(x => ItemType.IsLoopItem(x.ItemType)).OrderBy(x => x.LineNumber).ToList();
            var endLoopItems = Items.OfType<ILoopEndItem>().Where(x => x.ItemType == ItemType.Loop_End).OrderBy(x => x.LineNumber).ToList();

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
                    // CommandRegistry を使用して自動的に適切な型を作成
                    var itemType = CommandRegistry.GetItemType(item.ItemType);
                    if (itemType != null)
                    {
                        try
                        {
                            // デシリアライズされたデータから新しいアイテムを作成
                            var newItem = (ICommandListItem)Activator.CreateInstance(itemType, item)!;
                            Add(newItem);
                        }
                        catch (Exception ex)
                        {
                            throw new InvalidDataException($"型 {item.ItemType} のアイテム作成に失敗しました: {ex.Message}");
                        }
                    }
                    else
                    {
                        throw new InvalidDataException($"不明な ItemType: {item.ItemType}");
                    }
                }
            }

            CalculateNestLevel();
            PairIfItems();
            PairLoopItems();
        }
    }
}
