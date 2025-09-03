using AutoTool.Model.CommandDefinition;
using AutoTool.Model.List.Class;
using CommunityToolkit.Mvvm.ComponentModel;
using AutoTool.Command.Class;
using AutoTool.Command.Interface;
using AutoTool.List.Class;
using AutoTool.Model.List.Interface;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoTool.Model.MacroFactory;
using AutoTool.Services.Configuration;
using AutoTool.Helpers;

namespace AutoTool.List.Class
{
    public partial class CommandList : ObservableObject
    {
        [ObservableProperty]
        private ObservableCollection<ICommandListItem> _items = new();

        public ICommandListItem this[int index]
        {
            get => Items[index];
            set => Items[index] = value;
        }

        /// <summary>
        /// リスト変更後の共通処理
        /// </summary>
        private void RefreshListState()
        {
            ReorderItems();
            CalculateNestLevel();
            PairIfItems();
            PairLoopItems();
        }

        public void Add(ICommandListItem item)
        {
            Items.Add(item);
            RefreshListState();
        }

        public void Remove(ICommandListItem item)
        {
            Items.Remove(item);
            RefreshListState();
        }

        /// <summary>
        /// 指定インデックスのアイテムを削除
        /// </summary>
        public void RemoveAt(int index)
        {
            if (index >= 0 && index < Items.Count)
            {
                Items.RemoveAt(index);
                RefreshListState();
            }
        }

        public void Insert(int index, ICommandListItem item)
        {
            Items.Insert(index, item);
            RefreshListState();
        }

        public void Override(int index, ICommandListItem item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            if (index < 0 || index >= Items.Count)
                throw new ArgumentOutOfRangeException(nameof(index));

            Items[index] = item;
            RefreshListState();
        }

        public void Clear()
        {
            Items.Clear();
        }

        public void Move(int oldIndex, int newIndex)
        {
            if (oldIndex < 0 || oldIndex >= Items.Count || newIndex < 0 || newIndex >= Items.Count)
                return;

            var item = Items[oldIndex];
            Items.RemoveAt(oldIndex);
            Items.Insert(newIndex, item);
            RefreshListState();
        }

        public void Copy(int oldIndex, int newIndex)
        {
            if (oldIndex < 0 || oldIndex >= Items.Count || newIndex < 0 || newIndex >= Items.Count)
                return;

            var item = Items[oldIndex];
            Items.Insert(newIndex, item);
            RefreshListState();
        }

        public void ReorderItems()
        {
            for (int i = 0; i < Items.Count; i++)
            {
                Items[i].LineNumber = i + 1;
            }
        }

        public void CalculateNestLevel()
        {
            var nestLevel = 0;

            foreach (var item in Items)
            {
                // ネストレベルを減らすコマンド（終了系）
                if (CommandRegistry.IsEndCommand(item.ItemType))
                {
                    nestLevel--;
                }

                item.NestLevel = nestLevel;

                // ネストレベルを増やすコマンド（開始系）
                if (CommandRegistry.IsStartCommand(item.ItemType))
                {
                    nestLevel++;
                }
            }
        }

        /// <summary>
        /// 共通のペアリング処理
        /// </summary>
        private void PairItems<TStart, TEnd>(Func<ICommandListItem, bool> startPredicate, Func<ICommandListItem, bool> endPredicate)
            where TStart : class
            where TEnd : class
        {
            var startItems = Items.OfType<TStart>().Cast<ICommandListItem>()
                .Where(startPredicate)
                .OrderBy(x => x.LineNumber)
                .ToList();

            var endItems = Items.OfType<TEnd>().Cast<ICommandListItem>()
                .Where(endPredicate)
                .OrderBy(x => x.LineNumber)
                .ToList();

            foreach (var startItem in startItems)
            {
                var startPairItem = startItem as dynamic;
                if (startPairItem?.Pair != null) continue;

                foreach (var endItem in endItems)
                {
                    var endPairItem = endItem as dynamic;
                    if (endPairItem?.Pair != null) continue;

                    if (endItem.NestLevel == startItem.NestLevel && endItem.LineNumber > startItem.LineNumber)
                    {
                        startPairItem.Pair = endItem;
                        endPairItem.Pair = startItem;
                        break;
                    }
                }
            }
        }

        public void PairIfItems()
        {
            PairItems<IIfItem, IIfEndItem>(
                x => CommandRegistry.IsIfCommand(x.ItemType),
                x => x.ItemType == CommandRegistry.CommandTypes.IfEnd
            );
        }

        public void PairLoopItems()
        {
            PairItems<ILoopItem, ILoopEndItem>(
                x => CommandRegistry.IsLoopCommand(x.ItemType),
                x => x.ItemType == CommandRegistry.CommandTypes.LoopEnd
            );
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
