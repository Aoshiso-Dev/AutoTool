using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Win32;
using AutoTool.Panels.Model.List.Interface;
using AutoTool.Panels.Model.CommandDefinition;
using AutoTool.Panels.Serialization;

namespace AutoTool.Panels.List.Class
{
    public partial class CommandList : ObservableObject, ICommandList
    {
        private readonly ICommandDefinitionProvider _commandDefinitionProvider;
        private readonly IMacroFileSerializer _macroFileSerializer;

        [ObservableProperty]
        private ObservableCollection<ICommandListItem> _items = new();

        public CommandList(ICommandDefinitionProvider commandDefinitionProvider, IMacroFileSerializer macroFileSerializer)
        {
            _commandDefinitionProvider = commandDefinitionProvider ?? throw new ArgumentNullException(nameof(commandDefinitionProvider));
            _macroFileSerializer = macroFileSerializer ?? throw new ArgumentNullException(nameof(macroFileSerializer));
        }

        public ICommandListItem this[int index]
        {
            get => Items[index];
            set => Items[index] = value;
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

        /// <summary>
        /// 指定インデックスのアイテムを削除
        /// </summary>
        public void RemoveAt(int index)
        {
            if (index >= 0 && index < Items.Count)
            {
                Items.RemoveAt(index);

                ReorderItems();
                CalculateNestLevel();
                PairIfItems();
                PairLoopItems();
            }
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

            var item = Items[oldIndex].Clone();
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
                // ネストレベルを減らすコマンド（終了系）
                if (_commandDefinitionProvider.IsEndCommand(item.ItemType))
                {
                    nestLevel--;
                }

                item.NestLevel = nestLevel;

                // ネストレベルを増やすコマンド（開始系）
                if (_commandDefinitionProvider.IsStartCommand(item.ItemType))
                {
                    nestLevel++;
                }
            }
        }

        public void PairIfItems()
        {
            var ifItems = Items.OfType<IIfItem>()
                .Where(x => _commandDefinitionProvider.IsIfCommand(x.ItemType))
                .OrderBy(x => x.LineNumber)
                .ToList();
            var endIfItems = Items.OfType<IIfEndItem>()
                .OrderBy(x => x.LineNumber)
                .ToList();

            foreach (var ifItem in ifItems)
            {
                ifItem.Pair = null;
            }
            foreach (var endIfItem in endIfItems)
            {
                endIfItem.Pair = null;
            }

            foreach (var ifItem in ifItems)
            {
                foreach(var endIfItem in endIfItems)
                {
                    if (endIfItem.Pair != null)
                    {
                        continue;
                    }

                    if (endIfItem.NestLevel == ifItem.NestLevel && endIfItem.LineNumber > ifItem.LineNumber)
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
            var loopItems = Items.OfType<ILoopItem>()
                .Where(x => _commandDefinitionProvider.IsLoopCommand(x.ItemType))
                .OrderBy(x => x.LineNumber)
                .ToList();
            var endLoopItems = Items.OfType<ILoopEndItem>()
                .OrderBy(x => x.LineNumber)
                .ToList();

            foreach (var loopItem in loopItems)
            {
                loopItem.Pair = null;
            }
            foreach (var endLoopItem in endLoopItems)
            {
                endLoopItem.Pair = null;
            }

            foreach (var loopItem in loopItems)
            {
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

            _macroFileSerializer.SerializeToFile(cloneItems, filePath);
        }

        public void Load(string filePath)
        {
            var deserializedItems = _macroFileSerializer.DeserializeFromFile<ObservableCollection<ICommandListItem>>(filePath);
            if (deserializedItems != null)
            {
                Items.Clear();

                foreach (var item in deserializedItems)
                {
                    // 定義プロバイダーを使用して適切な型を作成
                    var itemType = _commandDefinitionProvider.GetItemType(item.ItemType);
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

