using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Win32;
using MacroPanels.Model.List.Interface;
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
                if(item.ItemType == ItemType.IfVariable)
                {
                    nestLevel++;
                }
            }
        }

        public void PairIfItems()
        {
            var ifItems = Items.OfType<IIfItem>().Where(x => x.ItemType == ItemType.IfImageExist || x.ItemType == ItemType.IfImageNotExist || x.ItemType == ItemType.IfImageExistAI || x.ItemType == ItemType.IfImageNotExistAI || x.ItemType == ItemType.IfVariable).OrderBy(x => x.LineNumber).ToList();
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
                            if (item is WaitImageItem)
                                Add(new WaitImageItem(item as WaitImageItem));
                            else
                                throw new InvalidDataException($"型不一致: {item.ItemType} を WaitImageItem にキャストできません。");
                            break;
                        case nameof(ItemType.ClickImage):
                            if (item is ClickImageItem)
                                Add(new ClickImageItem(item as ClickImageItem));
                            else
                                throw new InvalidDataException($"型不一致: {item.ItemType} を ClickImageItem にキャストできません。");
                            break;
                        case nameof(ItemType.Click):
                            if (item is ClickItem)
                                Add(new ClickItem(item as ClickItem));
                            else
                                throw new InvalidDataException($"型不一致: {item.ItemType} を ClickItem にキャストできません。");
                            break;
                        case nameof(ItemType.Hotkey):
                            if (item is HotkeyItem)
                                Add(new HotkeyItem(item as HotkeyItem));
                            else
                                throw new InvalidDataException($"型不一致: {item.ItemType} を HotkeyItem にキャストできません。");
                            break;
                        case nameof(ItemType.Wait):
                            if (item is WaitItem)
                                Add(new WaitItem(item as WaitItem));
                            else
                                throw new InvalidDataException($"型不一致: {item.ItemType} を WaitItem にキャストできません。");
                            break;
                        case nameof(ItemType.Loop):
                            if (item is LoopItem)
                                Add(new LoopItem(item as LoopItem));
                            else
                                throw new InvalidDataException($"型不一致: {item.ItemType} を LoopItem にキャストできません。");
                            break;
                        case nameof(ItemType.EndLoop):
                            if (item is EndLoopItem)
                                Add(new EndLoopItem(item as EndLoopItem));
                            else
                                throw new InvalidDataException($"型不一致: {item.ItemType} を EndLoopItem にキャストできません。");
                            break;
                        case nameof(ItemType.Break):
                            if (item is BreakItem)
                                Add(new BreakItem(item as BreakItem));
                            else
                                throw new InvalidDataException($"型不一致: {item.ItemType} を BreakItem にキャストできません。");
                            break;
                        case nameof(ItemType.IfImageExist):
                            if (item is IfImageExistItem)
                                Add(new IfImageExistItem(item as IfImageExistItem));
                            else
                                throw new InvalidDataException($"型不一致: {item.ItemType} を IfImageExistItem にキャストできません。");
                            break;
                        case nameof(ItemType.IfImageNotExist):
                            if (item is IfImageNotExistItem)
                                Add(new IfImageNotExistItem(item as IfImageNotExistItem));
                            else
                                throw new InvalidDataException($"型不一致: {item.ItemType} を IfImageNotExistItem にキャストできません。");
                            break;
                        case nameof(ItemType.EndIf):
                            if (item is EndIfItem)
                                Add(new EndIfItem(item as EndIfItem));
                            else
                                throw new InvalidDataException($"型不一致: {item.ItemType} を EndIfItem にキャストできません。");
                            break;
                        case nameof(ItemType.IfImageExistAI):
                            if (item is IfImageExistAIItem iea)
                                Add(new IfImageExistAIItem(iea));
                            else
                                throw new InvalidDataException($"型不一致: {item.ItemType} を IfImageExistAIItem にキャストできません。");
                            break;
                        case nameof(ItemType.IfImageNotExistAI):
                            if (item is IfImageNotExistAIItem ina)
                                Add(new IfImageNotExistAIItem(ina));
                            else
                                throw new InvalidDataException($"型不一致: {item.ItemType} を IfImageNotExistAIItem にキャストできません。");
                            break;
                        case nameof(ItemType.ExecuteProgram):
                            if (item is ExecuteProgramItem ep)
                                Add(new ExecuteProgramItem(ep));
                            else
                                throw new InvalidDataException($"型不一致: {item.ItemType} を ExecuteProgramItem にキャストできません。");
                            break;
                        case nameof(ItemType.SetVariable):
                            if (item is SetVariableItem sv)
                                Add(new SetVariableItem(sv));
                            else
                                throw new InvalidDataException($"型不一致: {item.ItemType} を SetVariableItem にキャストできません。");
                            break;
                        case nameof(ItemType.SetVariableAI):
                            if (item is SetVariableAIItem sva)
                                Add(new SetVariableAIItem(sva));
                            else
                                throw new InvalidDataException($"型不一致: {item.ItemType} を SetVariableAIItem にキャストできません。");
                            break;
                        case nameof(ItemType.IfVariable):
                            if (item is IfVariableItem iv)
                                Add(new IfVariableItem(iv));
                            else
                                throw new InvalidDataException($"型不一致: {item.ItemType} を IfVariableItem にキャストできません。");
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
