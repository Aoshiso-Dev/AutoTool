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
                    switch(item.ItemType)
                    {
                        case nameof(ItemType.Click):
                            if (item is ClickItem ci)
                                Add(new ClickItem(ci));
                            else
                                throw new InvalidDataException($"型不一致: {item.ItemType} を ClickItem にキャストできません。");
                            break;
                        case nameof(ItemType.Click_Image):
                            if (item is ClickImageItem cii)
                                Add(new ClickImageItem(cii));
                            else
                                throw new InvalidDataException($"型不一致: {item.ItemType} を ClickImageItem にキャストできません。");
                            break;
                        case nameof(ItemType.Hotkey):
                            if (item is HotkeyItem hi)
                                Add(new HotkeyItem(hi));
                            else
                                throw new InvalidDataException($"型不一致: {item.ItemType} を HotkeyItem にキャストできません。");
                            break;
                        case nameof(ItemType.Wait):
                            if (item is WaitItem wi)
                                Add(new WaitItem(wi));
                            else
                                throw new InvalidDataException($"型不一致: {item.ItemType} を WaitItem にキャストできません。");
                            break;
                        case nameof(ItemType.Wait_Image):
                            if (item is WaitImageItem wii)
                                Add(new WaitImageItem(wii));
                            else
                                throw new InvalidDataException($"型不一致: {item.ItemType} を WaitImageItem にキャストできません。");
                            break;
                        case nameof(ItemType.Execute):
                            if (item is ExecuteItem epi)
                                Add(new ExecuteItem(epi));
                            else
                                throw new InvalidDataException($"型不一致: {item.ItemType} を ExecuteProgramItem にキャストできません。");
                            break;
                        case nameof(ItemType.Screenshot):
                            if (item is ScreenshotItem si)
                                Add(new ScreenshotItem(si));
                            else
                                throw new InvalidDataException($"型不一致: {item.ItemType} を ScreenshotItem にキャストできません。");
                            break;
                        case nameof(ItemType.Loop):
                            if (item is LoopItem li)
                                Add(new LoopItem(li));
                            else
                                throw new InvalidDataException($"型不一致: {item.ItemType} を LoopItem にキャストできません。");
                            break;
                        case nameof(ItemType.Loop_End):
                            if (item is LoopEndItem eli)
                                Add(new LoopEndItem(eli));
                            else
                                throw new InvalidDataException($"型不一致: {item.ItemType} を EndLoopItem にキャストできません。");
                            break;
                        case nameof(ItemType.Loop_Break):
                            if (item is LoopBreakItem bi)
                                Add(new LoopBreakItem(bi));
                            else
                                throw new InvalidDataException($"型不一致: {item.ItemType} を BreakItem にキャストできません。");
                            break;
                        case nameof(ItemType.IF_ImageExist):
                            if (item is IfImageExistItem ifei)
                                Add(new IfImageExistItem(ifei));
                            else
                                throw new InvalidDataException($"型不一致: {item.ItemType} を IfImageExistItem にキャストできません。");
                            break;
                        case nameof(ItemType.IF_ImageNotExist):
                            if (item is IfImageNotExistItem ifnei)
                                Add(new IfImageNotExistItem(ifnei));
                            else
                                throw new InvalidDataException($"型不一致: {item.ItemType} を IfImageNotExistItem にキャストできません。");
                            break;
                        case nameof(ItemType.IF_ImageExist_AI):
                            if (item is IfImageExistAIItem ifeai)
                                Add(new IfImageExistAIItem(ifeai));
                            else
                                throw new InvalidDataException($"型不一致: {item.ItemType} を IfImageExistAIItem にキャストできません。");
                            break;
                        case nameof(ItemType.IF_ImageNotExist_AI):
                            if (item is IfImageNotExistAIItem ifneai)
                                Add(new IfImageNotExistAIItem(ifneai));
                            else
                                throw new InvalidDataException($"型不一致: {item.ItemType} を IfImageNotExistAIItem にキャストできません。");
                            break;
                        case nameof(ItemType.IF_Variable):
                            if (item is IfVariableItem ivi)
                                Add(new IfVariableItem(ivi));
                            else
                                throw new InvalidDataException($"型不一致: {item.ItemType} を IfVariableItem にキャストできません。");
                            break;
                        case nameof(ItemType.IF_End):
                            if (item is IfEndItem eii)
                                Add(new IfEndItem(eii));
                            else
                                throw new InvalidDataException($"型不一致: {item.ItemType} を EndIfItem にキャストできません。");
                            break;
                        case nameof(ItemType.SetVariable):
                            if (item is SetVariableItem svi)
                                Add(new SetVariableItem(svi));
                            else
                                throw new InvalidDataException($"型不一致: {item.ItemType} を SetVariableItem にキャストできません。");
                            break;
                        case nameof(ItemType.SetVariable_AI):
                            if (item is SetVariableAIItem svai)
                                Add(new SetVariableAIItem(svai));
                            else
                                throw new InvalidDataException($"型不一致: {item.ItemType} を SetVariableAIItem にキャストできません。");
                            break;
                        default:
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
