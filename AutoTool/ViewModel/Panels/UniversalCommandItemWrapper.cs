using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using AutoTool.Model.List.Interface;
using AutoTool.Model.CommandDefinition;

namespace AutoTool.ViewModel.Panels
{
    /// <summary>
    /// UniversalCommandItem�p��Wrapper�iDirectCommandRegistry�Ή��j
    /// </summary>
    public class UniversalCommandItemWrapper : ICommandListItem, INotifyPropertyChanged
    {
        private readonly UniversalCommandItem _innerItem;

        public UniversalCommandItemWrapper(UniversalCommandItem innerItem)
        {
            _innerItem = innerItem ?? throw new ArgumentNullException(nameof(innerItem));
            
            // �����A�C�e���̃v���p�e�B�ύX��]��
            _innerItem.PropertyChanged += (s, e) => PropertyChanged?.Invoke(this, e);
        }

        // ICommandListItem�̎����i�����A�C�e���ւ̈Ϗ��j
        public bool IsEnable
        {
            get => _innerItem.IsEnable;
            set => _innerItem.IsEnable = value;
        }

        public int LineNumber
        {
            get => _innerItem.LineNumber;
            set => _innerItem.LineNumber = value;
        }

        public bool IsRunning
        {
            get => _innerItem.IsRunning;
            set => _innerItem.IsRunning = value;
        }

        public bool IsSelected
        {
            get => _innerItem.IsSelected;
            set => _innerItem.IsSelected = value;
        }

        public string ItemType
        {
            get => _innerItem.ItemType;
            set => _innerItem.ItemType = value;
        }

        public string Description
        {
            get => _innerItem.Description;
            set => _innerItem.Description = value;
        }

        public string Comment
        {
            get => _innerItem.Comment;
            set => _innerItem.Comment = value;
        }

        public int NestLevel
        {
            get => _innerItem.NestLevel;
            set => _innerItem.NestLevel = value;
        }

        public bool IsInLoop
        {
            get => _innerItem.IsInLoop;
            set => _innerItem.IsInLoop = value;
        }

        public bool IsInIf
        {
            get => _innerItem.IsInIf;
            set => _innerItem.IsInIf = value;
        }

        public int Progress
        {
            get => _innerItem.Progress;
            set => _innerItem.Progress = value;
        }

        /// <summary>
        /// �\�����iDirectCommandRegistry ����擾�j
        /// </summary>
        [JsonIgnore]
        public string DisplayName
        {
            get
            {
                var displayName = DirectCommandRegistry.DisplayOrder.GetDisplayName(ItemType) ?? ItemType;
                return !string.IsNullOrEmpty(Comment) ? $"{displayName} - {Comment}" : displayName;
            }
        }

        /// <summary>
        /// �ڍׂȕ\�����i�s�ԍ��t���j
        /// </summary>
        [JsonIgnore]
        public string DetailedDisplayName
        {
            get
            {
                var displayName = DirectCommandRegistry.DisplayOrder.GetDisplayName(ItemType) ?? ItemType;
                var description = string.IsNullOrEmpty(Comment) ? "" : $" - {Comment}";
                return $"{LineNumber:D3}: {displayName}{description}";
            }
        }

        /// <summary>
        /// ������UniversalCommandItem�ւ̎Q��
        /// </summary>
        public UniversalCommandItem InnerItem => _innerItem;

        /// <summary>
        /// �ݒ�l���擾
        /// </summary>
        public T? GetSetting<T>(string key, T? defaultValue = default)
        {
            return _innerItem.GetSetting(key, defaultValue);
        }

        /// <summary>
        /// �ݒ�l��ݒ�
        /// </summary>
        public void SetSetting<T>(string key, T value)
        {
            _innerItem.SetSetting(key, value);
        }

        /// <summary>
        /// �ݒ莫���ւ̒��ڃA�N�Z�X
        /// </summary>
        public Dictionary<string, object?> Settings => _innerItem.Settings;

        /// <summary>
        /// �ݒ��`���X�g
        /// </summary>
        public List<SettingDefinition>? SettingDefinitions => _innerItem.SettingDefinitions;

        public event PropertyChangedEventHandler? PropertyChanged;

        public ICommandListItem Clone()
        {
            var clonedInner = _innerItem.Clone() as UniversalCommandItem;
            return clonedInner != null ? new UniversalCommandItemWrapper(clonedInner) : this;
        }
    }
}