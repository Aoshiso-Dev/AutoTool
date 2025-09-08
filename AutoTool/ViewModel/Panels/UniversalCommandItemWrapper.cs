using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using AutoTool.Command.Definition;
using AutoTool.ViewModel.Shared;

namespace AutoTool.ViewModel.Panels
{
    /// <summary>
    /// UniversalCommandItem用のWrapper（DirectCommandRegistry対応）
    /// 基本的なプロパティインターフェースを提供
    /// </summary>
    public class UniversalCommandItemWrapper : INotifyPropertyChanged
    {
        private readonly UniversalCommandItem _innerItem;

        public UniversalCommandItemWrapper(UniversalCommandItem innerItem)
        {
            _innerItem = innerItem ?? throw new ArgumentNullException(nameof(innerItem));
            
            // 内部アイテムのプロパティ変更を転送
            _innerItem.PropertyChanged += (s, e) => PropertyChanged?.Invoke(this, e);
        }

        // 基本プロパティの実装（内部アイテムへの委譲）
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
        /// 表示名（DirectCommandRegistry から取得）
        /// </summary>
        [JsonIgnore]
        public string DisplayName
        {
            get
            {
                var displayName = AutoToolCommandRegistry.DisplayOrder.GetDisplayName(ItemType) ?? ItemType;
                return !string.IsNullOrEmpty(Comment) ? $"{displayName} - {Comment}" : displayName;
            }
        }

        /// <summary>
        /// 詳細な表示名（行番号付き）
        /// </summary>
        [JsonIgnore]
        public string DetailedDisplayName
        {
            get
            {
                var displayName = AutoToolCommandRegistry.DisplayOrder.GetDisplayName(ItemType) ?? ItemType;
                var description = string.IsNullOrEmpty(Comment) ? "" : $" - {Comment}";
                return $"{LineNumber:D3}: {displayName}{description}";
            }
        }

        /// <summary>
        /// 内部のUniversalCommandItemへの参照
        /// </summary>
        public UniversalCommandItem InnerItem => _innerItem;

        /// <summary>
        /// 設定値を取得
        /// </summary>
        public T? GetSetting<T>(string key, T? defaultValue = default)
        {
            return _innerItem.GetSetting(key, defaultValue);
        }

        /// <summary>
        /// 設定値を設定
        /// </summary>
        public void SetSetting<T>(string key, T value)
        {
            _innerItem.SetSetting(key, value);
        }

        /// <summary>
        /// 設定辞書への直接アクセス
        /// </summary>
        public Dictionary<string, object?> Settings => _innerItem.Settings;

        /// <summary>
        /// 設定定義リスト
        /// </summary>
        //public List<SettingDefinition>? SettingDefinitions => _innerItem.SettingDefinitions;

        public event PropertyChangedEventHandler? PropertyChanged;

        public UniversalCommandItemWrapper Clone()
        {
            var clonedInner = _innerItem.Clone() as UniversalCommandItem;
            return clonedInner != null ? new UniversalCommandItemWrapper(clonedInner) : this;
        }
    }
}