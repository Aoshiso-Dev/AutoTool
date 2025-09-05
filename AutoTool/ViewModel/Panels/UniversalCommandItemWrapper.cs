using AutoTool.Model.List.Interface;
using AutoTool.Model.CommandDefinition;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AutoTool.ViewModel.Panels
{
    /// <summary>
    /// UniversalCommandItemをICommandListItemとして使用するためのWrapper
    /// </summary>
    public class UniversalCommandItemWrapper : ICommandListItem, INotifyPropertyChanged
    {
        private readonly UniversalCommandItem _universalItem;
        private bool _isRunning = false;
        private int _progress = 0;

        public UniversalCommandItemWrapper(UniversalCommandItem universalItem)
        {
            _universalItem = universalItem ?? throw new ArgumentNullException(nameof(universalItem));
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // ICommandListItemインターフェースの実装
        public int LineNumber
        {
            get => _universalItem.LineNumber;
            set
            {
                if (_universalItem.LineNumber != value)
                {
                    _universalItem.LineNumber = value;
                    OnPropertyChanged();
                }
            }
        }

        public string ItemType
        {
            get => _universalItem.ItemType;
            set
            {
                if (_universalItem.ItemType != value)
                {
                    _universalItem.ItemType = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Comment
        {
            get => _universalItem.Comment;
            set
            {
                if (_universalItem.Comment != value)
                {
                    _universalItem.Comment = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsEnable
        {
            get => _universalItem.IsEnable;
            set
            {
                if (_universalItem.IsEnable != value)
                {
                    _universalItem.IsEnable = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsRunning
        {
            get => _isRunning;
            set
            {
                if (_isRunning != value)
                {
                    _isRunning = value;
                    OnPropertyChanged();
                }
            }
        }

        public int Progress
        {
            get => _progress;
            set
            {
                if (_progress != value)
                {
                    _progress = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsSelected { get; set; } = false;

        public string Description
        {
            get
            {
                var displayName = AutoTool.Model.CommandDefinition.CommandRegistry.DisplayOrder.GetDisplayName(ItemType) ?? ItemType;
                return string.IsNullOrEmpty(Comment) ? displayName : $"{displayName} - {Comment}";
            }
            set
            {
                // Description は Comment に反映（表示名は変更不可）
                var displayName = AutoTool.Model.CommandDefinition.CommandRegistry.DisplayOrder.GetDisplayName(ItemType) ?? ItemType;
                if (value.StartsWith(displayName))
                {
                    var remainder = value.Substring(displayName.Length).TrimStart(' ', '-').Trim();
                    Comment = remainder;
                }
                else
                {
                    Comment = value;
                }
                OnPropertyChanged();
            }
        }

        public int NestLevel { get; set; } = 0;

        public bool IsInLoop { get; set; } = false;

        public bool IsInIf { get; set; } = false;

        // UniversalCommandItemへのアクセス
        public UniversalCommandItem UniversalItem => _universalItem;

        public ICommandListItem Clone()
        {
            var clonedUniversalItem = new UniversalCommandItem
            {
                ItemType = _universalItem.ItemType,
                Comment = _universalItem.Comment,
                IsEnable = _universalItem.IsEnable,
                LineNumber = _universalItem.LineNumber,
                Settings = new Dictionary<string, object?>(_universalItem.Settings)
            };

            var wrapper = new UniversalCommandItemWrapper(clonedUniversalItem)
            {
                IsRunning = this.IsRunning,
                Progress = this.Progress
            };

            return wrapper;
        }

        public override string ToString()
        {
            return $"[動的] {ItemType} (Line: {LineNumber})";
        }
    }
}