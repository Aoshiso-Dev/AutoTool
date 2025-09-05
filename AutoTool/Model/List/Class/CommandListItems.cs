using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using AutoTool.Model.List.Interface;
using AutoTool.Model.CommandDefinition;

namespace AutoTool.Model.List.Class
{
    /// <summary>
    /// ��{�I��CommandListItem�����iDirectCommandRegistry�Ή��j
    /// </summary>
    public class CommandListItems : ICommandListItem, INotifyPropertyChanged
    {
        private bool _isEnable = true;
        private int _lineNumber;
        private bool _isRunning;
        private bool _isSelected;
        private string _itemType = string.Empty;
        private string _description = string.Empty;
        private string _comment = string.Empty;
        private int _nestLevel;
        private bool _isInLoop;
        private bool _isInIf;
        private int _progress;

        public bool IsEnable
        {
            get => _isEnable;
            set => SetProperty(ref _isEnable, value);
        }

        public int LineNumber
        {
            get => _lineNumber;
            set => SetProperty(ref _lineNumber, value);
        }

        public bool IsRunning
        {
            get => _isRunning;
            set => SetProperty(ref _isRunning, value);
        }

        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        public string ItemType
        {
            get => _itemType;
            set => SetProperty(ref _itemType, value);
        }

        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        public string Comment
        {
            get => _comment;
            set => SetProperty(ref _comment, value);
        }

        public int NestLevel
        {
            get => _nestLevel;
            set => SetProperty(ref _nestLevel, value);
        }

        public bool IsInLoop
        {
            get => _isInLoop;
            set => SetProperty(ref _isInLoop, value);
        }

        public bool IsInIf
        {
            get => _isInIf;
            set => SetProperty(ref _isInIf, value);
        }

        public int Progress
        {
            get => _progress;
            set => SetProperty(ref _progress, value);
        }

        /// <summary>
        /// �\�����iDirectCommandRegistry ����擾�j
        /// </summary>
        [JsonIgnore]
        public string DisplayName => DirectCommandRegistry.DisplayOrder.GetDisplayName(ItemType);

        /// <summary>
        /// �J�e�S�����iDirectCommandRegistry ����擾�j
        /// </summary>
        [JsonIgnore]
        public string CategoryName => DirectCommandRegistry.DisplayOrder.GetCategoryName(ItemType);

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;

            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            return true;
        }

        public virtual ICommandListItem Clone()
        {
            return new CommandListItems
            {
                IsEnable = this.IsEnable,
                LineNumber = this.LineNumber,
                IsRunning = this.IsRunning,
                IsSelected = this.IsSelected,
                ItemType = this.ItemType,
                Description = this.Description,
                Comment = this.Comment,
                NestLevel = this.NestLevel,
                IsInLoop = this.IsInLoop,
                IsInIf = this.IsInIf,
                Progress = this.Progress
            };
        }
    }
}