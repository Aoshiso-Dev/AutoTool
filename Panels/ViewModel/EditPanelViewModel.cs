using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using MacroPanels.Message;
using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.Input;
using MacroPanels.List.Class;
using MacroPanels.Model.List.Interface;
using MacroPanels.View;
using System.IO;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.Win32;
using ColorPickHelper;
using MacroPanels.ViewModel.Helpers;
using MacroPanels.Model.CommandDefinition;
using MacroPanels.ViewModel.Shared;
using MacroPanels.Helpers;
using Microsoft.Extensions.Logging;

namespace MacroPanels.ViewModel
{
    [Obsolete("Phase 3で統合版に移行。AutoTool.ViewModel.Panels.EditPanelViewModelを使用してください", false)]
    public partial class EditPanelViewModel : ObservableObject
    {
        private readonly ILogger<EditPanelViewModel> _logger;
        private readonly CommandHistoryManager _commandHistory;
        private readonly EditPanelPropertyManager _propertyManager;
        private ICommandListItem? _item = null;
        private bool _isUpdating;
        private readonly DispatcherTimer _refreshTimer = new() { Interval = TimeSpan.FromMilliseconds(120) };

        // プロパティ
        public ICommandListItem? Item
        {
            get => _item;
            set
            {
                if (SetProperty(ref _item, value))
                {
                    // Itemが変更された時に対応するSelectedItemTypeObjを更新
                    if (value != null)
                    {
                        var displayItem = ItemTypes.FirstOrDefault(x => x.TypeName == value.ItemType);
                        if (displayItem != null && _selectedItemTypeObj != displayItem)
                        {
                            _selectedItemTypeObj = displayItem;
                            OnPropertyChanged(nameof(SelectedItemTypeObj));
                        }
                    }
                    else
                    {
                        _selectedItemTypeObj = null;
                        OnPropertyChanged(nameof(SelectedItemTypeObj));
                    }

                    OnItemChanged();
                    UpdateProperties(); 
                    UpdateIsProperties(); 
                }
            }
        }

        [ObservableProperty]
        private CommandDisplayItem? _selectedItemTypeObj;

        [ObservableProperty]
        private bool _isRunning = false;

        [ObservableProperty]
        private int _listCount = 0;

        [ObservableProperty]
        private ObservableCollection<CommandDisplayItem> _itemTypes = new();

        [ObservableProperty]
        private ObservableCollection<MouseButton> _mouseButtons = new();

        [ObservableProperty]
        private ObservableCollection<OperatorItem> _operators = new();

        [ObservableProperty]
        private ObservableCollection<AIDetectModeItem> _aiDetectModes = new();

        // ViewModels用のDI対応コンストラクタ
        public EditPanelViewModel(
            ILogger<EditPanelViewModel> logger,
            CommandHistoryManager commandHistory)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _commandHistory = commandHistory ?? throw new ArgumentNullException(nameof(commandHistory));
            _propertyManager = new EditPanelPropertyManager();

            _logger.LogInformation("EditPanelViewModel を DI対応で初期化しています");

            _refreshTimer.Tick += (s, e) => { _refreshTimer.Stop(); WeakReferenceMessenger.Default.Send(new RefreshListViewMessage()); };

            InitializeItemTypes();
            InitializeOperators();
            InitializeAIDetectModes();
        }

        private void InitializeItemTypes()
        {
            // CommandRegistryを初期化
            CommandRegistry.Initialize();

            // 日本語表示名付きのアイテムを作成
            var displayItems = CommandRegistry.GetOrderedTypeNames()
                .Select(typeName => new CommandDisplayItem
                {
                    TypeName = typeName,
                    DisplayName = CommandRegistry.DisplayOrder.GetDisplayName(typeName),
                    Category = CommandRegistry.DisplayOrder.GetCategoryName(typeName)
                })
                .ToList();

            ItemTypes = new ObservableCollection<CommandDisplayItem>(displayItems);

            foreach (var button in Enum.GetValues(typeof(MouseButton)).Cast<MouseButton>())
                MouseButtons.Add(button);

            _logger.LogInformation("EditPanelViewModel の初期化が完了しました");
        }

        private void InitializeOperators()
        {
            Operators.Clear();
            Operators.Add(new OperatorItem { Key = "Equal", DisplayName = "等しい" });
            Operators.Add(new OperatorItem { Key = "NotEqual", DisplayName = "等しくない" });
            Operators.Add(new OperatorItem { Key = "GreaterThan", DisplayName = "より大きい" });
            Operators.Add(new OperatorItem { Key = "LessThan", DisplayName = "より小さい" });
        }

        private void InitializeAIDetectModes()
        {
            AiDetectModes.Clear();
            AiDetectModes.Add(new AIDetectModeItem { Key = "Fast", DisplayName = "高速" });
            AiDetectModes.Add(new AIDetectModeItem { Key = "Accurate", DisplayName = "高精度" });
            AiDetectModes.Add(new AIDetectModeItem { Key = "Balanced", DisplayName = "バランス" });
        }

        private void OnItemChanged()
        {
            try
            {
                _logger.LogDebug("アイテムが変更されました: {ItemType}", Item?.ItemType ?? "null");
                // プロパティ管理の処理は省略（必要に応じて追加）
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "アイテム変更処理中にエラーが発生しました");
            }
        }

        #region Item Type Detection Properties
        public bool IsListNotEmpty => ListCount > 0;
        public bool IsListEmpty => ListCount == 0;
        public bool IsListNotEmptyButNoSelection => ListCount > 0 && Item == null;
        public bool IsNotNullItem => Item != null;
        public bool IsWaitImageItem => Item is WaitImageItem;
        public bool IsClickImageItem => Item is ClickImageItem;
        public bool IsClickImageAIItem => Item is ClickImageAIItem;
        public bool IsHotkeyItem => Item is HotkeyItem;
        public bool IsClickItem => Item is ClickItem;
        public bool IsWaitItem => Item is WaitItem;
        public bool IsLoopItem => Item is LoopItem;
        public bool IsEndLoopItem => Item is LoopEndItem;
        public bool IsBreakItem => Item is LoopBreakItem;
        public bool IsIfImageExistItem => Item is IfImageExistItem;
        public bool IsIfImageNotExistItem => Item is IfImageNotExistItem;
        public bool IsIfImageExistAIItem => Item is IfImageExistAIItem;
        public bool IsIfImageNotExistAIItem => Item is IfImageNotExistAIItem;
        public bool IsEndIfItem => Item is IfEndItem;
        public bool IsExecuteProgramItem => Item is ExecuteItem;
        public bool IsSetVariableItem => Item is SetVariableItem;
        public bool IsSetVariableAIItem => Item is SetVariableAIItem;
        public bool IsIfVariableItem => Item is IfVariableItem;
        public bool IsScreenshotItem => Item is ScreenshotItem;
        #endregion

        #region Window Properties
        public string WindowTitleText => string.IsNullOrEmpty(WindowTitle) ? "指定なし" : WindowTitle;
        public string WindowTitle { get => _propertyManager.WindowTitle.GetValue(Item); set { _propertyManager.WindowTitle.SetValue(Item, value); UpdateProperties(); } }
        public string WindowClassNameText => string.IsNullOrEmpty(WindowClassName) ? "指定なし" : WindowClassName;
        public string WindowClassName { get => _propertyManager.WindowClassName.GetValue(Item); set { _propertyManager.WindowClassName.SetValue(Item, value); UpdateProperties(); } }
        #endregion

        #region Basic Properties
        public string Comment 
        { 
            get => Item?.Comment ?? string.Empty; 
            set 
            { 
                if (Item != null && Item.Comment != value)
                {
                    Item.Comment = value;
                    UpdateProperties();
                }
            } 
        }
        #endregion

        #region Property Update Management
        private void UpdateIsProperties()
        {
            var propertyNames = new[]
            {
                nameof(IsListNotEmpty), nameof(IsListEmpty), nameof(IsListNotEmptyButNoSelection), 
                nameof(IsNotNullItem), nameof(IsWaitImageItem), 
                nameof(IsClickImageItem), nameof(IsClickImageAIItem), nameof(IsHotkeyItem), 
                nameof(IsClickItem), nameof(IsWaitItem), nameof(IsLoopItem), 
                nameof(IsEndLoopItem), nameof(IsBreakItem), nameof(IsIfImageExistItem), 
                nameof(IsIfImageNotExistItem), nameof(IsEndIfItem), nameof(IsIfImageExistAIItem), 
                nameof(IsIfImageNotExistAIItem), nameof(IsExecuteProgramItem), nameof(IsSetVariableItem), 
                nameof(IsSetVariableAIItem), nameof(IsIfVariableItem), nameof(IsScreenshotItem)
            };

            foreach (var name in propertyNames)
                OnPropertyChanged(name);
        }

        void UpdateProperties()
        {
            if (_isUpdating) return;
            
            try
            {
                _isUpdating = true;
                
                var propertyNames = new[]
                {
                    nameof(WindowTitle), nameof(WindowTitleText), 
                    nameof(WindowClassName), nameof(WindowClassNameText), 
                    nameof(Comment)
                };
                
                foreach (var name in propertyNames)
                    OnPropertyChanged(name);
                
                _refreshTimer.Stop();
                _refreshTimer.Start();
            }
            finally
            {
                _isUpdating = false;
            }
        }
        #endregion

        /// <summary>
        /// アイテムを設定
        /// </summary>
        public void SetItem(ICommandListItem? item)
        {
            Item = item;
        }

        /// <summary>
        /// リストカウントを設定
        /// </summary>
        public void SetListCount(int count)
        {
            ListCount = count;
        }

        /// <summary>
        /// 実行状態を設定
        /// </summary>
        public void SetRunningState(bool isRunning)
        {
            IsRunning = isRunning;
            _logger.LogDebug("実行状態を設定: {IsRunning}", isRunning);
        }

        /// <summary>
        /// 準備処理
        /// </summary>
        public void Prepare()
        {
            _logger.LogDebug("EditPanelViewModel の準備処理を実行");
            // 必要に応じて準備処理を追加
        }
    }

    // 補助クラス
    public class OperatorItem
    {
        public string Key { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
    }

    public class AIDetectModeItem
    {
        public string Key { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
    }
}