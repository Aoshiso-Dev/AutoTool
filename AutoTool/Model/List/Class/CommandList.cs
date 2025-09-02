using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using AutoTool.Model.List.Interface;
using AutoTool.Model.CommandDefinition;
using System.IO;

namespace AutoTool.Model.List.Class
{
    /// <summary>
    /// Phase 5�����ŁF�R�}���h���X�g�N���X
    /// </summary>
    public partial class CommandList : ObservableObject, ICommandList
    {
        [ObservableProperty]
        private ObservableCollection<ICommandListItem> _items = new();

        // �C���^�[�t�F�[�X�̖����I�Ȏ���
        ObservableCollection<ICommandListItem> ICommandList.Items => Items;

        public ICommandListItem this[int index]
        {
            get => Items[index];
            set => Items[index] = value;
        }

        /// <summary>
        /// ���X�g�ύX��̋��ʏ���
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
        /// �w��C���f�b�N�X�̃A�C�e�����폜
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
            if(item == null)
                throw new ArgumentNullException(nameof(item));

            if(index < 0 || index >= Items.Count)
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
                // �l�X�g���x�������炷�R�}���h�i�I���n�j
                if (CommandRegistry.IsEndCommand(item.ItemType))
                {
                    nestLevel--;
                }

                item.NestLevel = nestLevel;

                // �l�X�g���x���𑝂₷�R�}���h�i�J�n�n�j
                if (CommandRegistry.IsStartCommand(item.ItemType))
                {
                    nestLevel++;
                }
            }
        }

        /// <summary>
        /// ���ʂ̃y�A�����O�����iPhase 5�����Łj
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

            // Phase 5: AutoTool�����ł�JSON�V���A���C�U�[���g�p
            var json = System.Text.Json.JsonSerializer.Serialize(cloneItems, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filePath, json);
        }

        public void Load(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    var json = File.ReadAllText(filePath);
                    var deserializedItems = System.Text.Json.JsonSerializer.Deserialize<ObservableCollection<ICommandListItem>>(json);
                    
                    if (deserializedItems != null)
                    {
                        Items.Clear();

                        foreach (var item in deserializedItems)
                        {
                            // CommandRegistry ���g�p���Ď����I�ɓK�؂Ȍ^���쐬
                            var newItem = CommandRegistry.CreateCommandItem(item.ItemType);
                            if (newItem != null)
                            {
                                // �v���p�e�B���R�s�[
                                newItem.Comment = item.Comment;
                                newItem.IsEnable = item.IsEnable;
                                // �K�v�ɉ����đ��̃v���p�e�B���R�s�[
                                Add(newItem);
                            }
                            else
                            {
                                throw new InvalidDataException($"�s���� ItemType: {item.ItemType}");
                            }
                        }
                    }

                    CalculateNestLevel();
                    PairIfItems();
                    PairLoopItems();
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"�t�@�C���ǂݍ��ݒ��ɃG���[���������܂���: {ex.Message}", ex);
            }
        }
    }

    /// <summary>
    /// Phase 5�����ŁF�R�}���h���X�g�C���^�[�t�F�[�X
    /// </summary>
    public interface ICommandList
    {
        ObservableCollection<ICommandListItem> Items { get; }
        ICommandListItem this[int index] { get; set; }
        
        void Add(ICommandListItem item);
        void Remove(ICommandListItem item);
        void RemoveAt(int index);
        void Insert(int index, ICommandListItem item);
        void Override(int index, ICommandListItem item);
        void Clear();
        void Move(int oldIndex, int newIndex);
        void Copy(int oldIndex, int newIndex);
        
        void ReorderItems();
        void CalculateNestLevel();
        void PairIfItems();
        void PairLoopItems();
        
        IEnumerable<ICommandListItem> Clone();
        void Save(string filePath);
        void Load(string filePath);
    }
}