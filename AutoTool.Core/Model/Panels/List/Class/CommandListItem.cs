using CommunityToolkit.Mvvm.ComponentModel;
using AutoTool.Commands.Interface;
using AutoTool.Commands.Commands;
using AutoTool.Panels.Model.List.Interface;
using AutoTool.Panels.Attributes;
using CommandDef = AutoTool.Panels.Model.CommandDefinition;

namespace AutoTool.Panels.List.Class;

    public partial class CommandListItem : ObservableObject, ICommandListItem
    {
        public static string GetDisplayNameForType(string itemType)
        {
            return CommandDef.CommandMetadataCatalog.TryGetByTypeName(itemType, out var metadata)
                ? metadata.DisplayNameJa
                : itemType;
        }

        public static string GetCategoryNameForType(string itemType)
        {
            if (!CommandDef.CommandMetadataCatalog.TryGetByTypeName(itemType, out var metadata))
            {
                return itemType;
            }

            return metadata.Category switch
            {
                CommandDef.CommandCategory.Action => "クリック操作",
                CommandDef.CommandCategory.Control => "条件分岐",
                CommandDef.CommandCategory.AI => "AI",
                CommandDef.CommandCategory.System => "システム操作",
                CommandDef.CommandCategory.Variable => "変数操作",
                _ => "その他"
            };
        }

        public static int GetDisplayPriorityForType(string itemType)
        {
            return CommandDef.CommandMetadataCatalog.TryGetByTypeName(itemType, out var metadata)
                ? metadata.DisplayPriority
                : 9;
        }

        [ObservableProperty]
        protected bool _isEnable = true;
        [ObservableProperty]
        protected bool _isRunning = false;
        [ObservableProperty]
        protected bool _isSelected = false;
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        protected int _lineNumber = 0;
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(DisplayName))]
        protected string _itemType = "None";
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(FullDescription))]
        protected string _description = string.Empty;
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(FullDescription))]
        [property: CommandProperty("コメント", EditorType.MultiLineTextBox, Group = "その他", Order = 99,
                         Description = "このコマンドのメモ")]
        protected string _comment = string.Empty;
        [ObservableProperty]
        protected int _nestLevel = 0;
        [ObservableProperty]
        protected bool _isInLoop = false;
        [ObservableProperty]
        protected bool _isInIf = false;
        [ObservableProperty]
        protected int _progress = 0;

        /// <summary>
        /// UI表示用の日本語名を取得
        /// </summary>
        public string DisplayName => GetDisplayNameForType(ItemType);

        /// <summary>
        /// カテゴリ名を取得
        /// </summary>
        public string CategoryName => GetCategoryNameForType(ItemType);

        /// <summary>
        /// コメント付きの完全な説明を取得
        /// </summary>
        public string FullDescription
        {
            get
            {
                var baseDesc = Description;
                if (!string.IsNullOrWhiteSpace(Comment))
                {
                    return string.IsNullOrWhiteSpace(baseDesc) 
                        ? $"💬 {Comment}" 
                        : $"{baseDesc} 💬 {Comment}";
                }
                return baseDesc;
            }
        }

        /// <summary>
        /// コメントが設定されているかどうか
        /// </summary>
        public bool HasComment => !string.IsNullOrWhiteSpace(Comment);

        public CommandListItem() { }

        public CommandListItem(CommandListItem? item)
        {
            if (item is not null)
            {
                IsEnable = item.IsEnable;
                IsRunning = item.IsRunning;
                IsSelected = item.IsSelected;
                LineNumber = item.LineNumber;
                ItemType = item.ItemType;
                Comment = item.Comment;
                NestLevel = item.NestLevel;
                IsInLoop = item.IsInLoop;
                IsInIf = item.IsInIf;
                Progress = item.Progress;
            }
        }

        public ICommandListItem Clone()
        {
            return new CommandListItem(this);
        }
        
        /// <summary>
        /// Execute the command logic (override in derived classes)
        /// </summary>
        public virtual ValueTask<bool> ExecuteAsync(ICommandExecutionContext context, CancellationToken cancellationToken)
        {
            return ValueTask.FromResult(true);
        }

        protected void SetIsEnableWithPair(ICommandListItem? pair, bool value)
        {
            if (IsEnable == value)
            {
                return;
            }

            IsEnable = value;
            if (pair is not null)
            {
                pair.IsEnable = value;
            }
        }
    }


