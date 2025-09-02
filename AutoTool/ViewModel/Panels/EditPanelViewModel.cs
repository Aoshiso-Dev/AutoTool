using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using AutoTool.Message;
using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.Input;
using AutoTool.Model.List.Interface;
using System.IO;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.Win32;
using ColorPickHelper;
using AutoTool.Model.CommandDefinition;
using Microsoft.Extensions.Logging;
using AutoTool.ViewModel.Helpers;
using AutoTool.ViewModel.Shared; // Phase 5統合版CommandHistoryManager
using CommandDisplayItem = AutoTool.ViewModel.Shared.CommandDisplayItem;

namespace AutoTool.ViewModel.Panels
{
    /// <summary>
    /// 統合されたEditPanelViewModel（AutoTool.ViewModel名前空間）
    /// Phase 5: 完全統合実装版、MacroPanels依存を完全排除
    /// </summary>
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
        private AutoTool.ViewModel.Shared.CommandDisplayItem? _selectedItemTypeObj;

        [ObservableProperty]
        private bool _isRunning = false;

        [ObservableProperty]
        private int _listCount = 0;

        [ObservableProperty]
        private ObservableCollection<AutoTool.ViewModel.Shared.CommandDisplayItem> _itemTypes = new();

        [ObservableProperty]
        private ObservableCollection<MouseButton> _mouseButtons = new();

        [ObservableProperty]
        private ObservableCollection<OperatorItem> _operators = new();

        [ObservableProperty]
        private ObservableCollection<AIDetectModeItem> _aiDetectModes = new();

        // ViewModels用のDI対応コンストラクタ（Phase 5完全統合版）
        public EditPanelViewModel(
            ILogger<EditPanelViewModel> logger,
            CommandHistoryManager commandHistory)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _commandHistory = commandHistory ?? throw new ArgumentNullException(nameof(commandHistory));
            _propertyManager = new EditPanelPropertyManager(); // AutoTool統合版を使用

            _logger.LogInformation("Phase 5完全統合EditPanelViewModel を DI対応で初期化しています");

            _refreshTimer.Tick += (s, e) => { _refreshTimer.Stop(); WeakReferenceMessenger.Default.Send(new RefreshListViewMessage()); };

            InitializeItemTypes();
            InitializeOperators();
            InitializeAIDetectModes();
        }

        private void InitializeItemTypes()
        {
            // AutoTool統合CommandRegistryを初期化
            CommandRegistry.Initialize();

            // 日本語表示名付きのアイテムを作成（完全統合版CommandDisplayItemを使用）
            var displayItems = CommandRegistry.GetOrderedTypeNames()
                .Select(typeName => new CommandDisplayItem
                {
                    TypeName = typeName,
                    DisplayName = CommandRegistry.DisplayOrder.GetDisplayName(typeName),
                    Category = CommandRegistry.DisplayOrder.GetCategoryName(typeName)
                })
                .ToList();

            ItemTypes = new ObservableCollection<AutoTool.ViewModel.Shared.CommandDisplayItem>(displayItems);

            foreach (var button in Enum.GetValues(typeof(MouseButton)).Cast<MouseButton>())
                MouseButtons.Add(button);

            _logger.LogInformation("Phase 5完全統合EditPanelViewModel の初期化が完了しました");
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
                // Phase 5: AutoTool統合版プロパティマネージャーによる完全な管理
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "アイテム変更処理中にエラーが発生しました");
            }
        }

        #region Item Type Detection Properties (Phase 5完全統合版)
        public bool IsListNotEmpty => ListCount > 0;
        public bool IsListEmpty => ListCount == 0;
        public bool IsListNotEmptyButNoSelection => ListCount > 0 && Item == null;
        public bool IsNotNullItem => Item != null;
        
        // Phase 5: ItemType文字列ベース判定に統一（型安全性を維持）
        public bool IsWaitImageItem => Item?.ItemType == CommandRegistry.CommandTypes.WaitImage;
        public bool IsClickImageItem => Item?.ItemType == CommandRegistry.CommandTypes.ClickImage;
        public bool IsClickImageAIItem => Item?.ItemType == CommandRegistry.CommandTypes.ClickImageAI;
        public bool IsHotkeyItem => Item?.ItemType == CommandRegistry.CommandTypes.Hotkey;
        public bool IsClickItem => Item?.ItemType == CommandRegistry.CommandTypes.Click;
        public bool IsWaitItem => Item?.ItemType == CommandRegistry.CommandTypes.Wait;
        public bool IsLoopItem => Item?.ItemType == CommandRegistry.CommandTypes.Loop;
        public bool IsEndLoopItem => Item?.ItemType == CommandRegistry.CommandTypes.LoopEnd;
        public bool IsBreakItem => Item?.ItemType == CommandRegistry.CommandTypes.LoopBreak;
        public bool IsIfImageExistItem => Item?.ItemType == CommandRegistry.CommandTypes.IfImageExist;
        public bool IsIfImageNotExistItem => Item?.ItemType == CommandRegistry.CommandTypes.IfImageNotExist;
        public bool IsIfImageExistAIItem => Item?.ItemType == CommandRegistry.CommandTypes.IfImageExistAI;
        public bool IsIfImageNotExistAIItem => Item?.ItemType == CommandRegistry.CommandTypes.IfImageNotExistAI;
        public bool IsEndIfItem => Item?.ItemType == CommandRegistry.CommandTypes.IfEnd;
        public bool IsExecuteProgramItem => Item?.ItemType == CommandRegistry.CommandTypes.Execute;
        public bool IsSetVariableItem => Item?.ItemType == CommandRegistry.CommandTypes.SetVariable;
        public bool IsSetVariableAIItem => Item?.ItemType == CommandRegistry.CommandTypes.SetVariableAI;
        public bool IsIfVariableItem => Item?.ItemType == CommandRegistry.CommandTypes.IfVariable;
        public bool IsScreenshotItem => Item?.ItemType == CommandRegistry.CommandTypes.Screenshot;
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
            _logger.LogDebug("Phase 5完全統合EditPanelViewModel の準備処理を実行");
        }
    }

    // 補助クラス（AutoTool.ViewModel.Panels名前空間に統合）
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