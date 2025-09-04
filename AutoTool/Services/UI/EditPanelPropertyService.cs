using AutoTool.Model.List.Interface;
using AutoTool.ViewModel.Shared;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using System.Windows.Media;
using AutoTool.Message;

namespace AutoTool.Services.UI
{
    /// <summary>
    /// EditPanelのプロパティを管理するサービス
    /// </summary>
    [Obsolete("標準MVVM方式に統一。AutoTool.ViewModel.Panels.EditPanelViewModelを使用してください", false)]
    public interface IEditPanelPropertyService
    {
        // 現在選択されているアイテム
        ICommandListItem? CurrentItem { get; }
        
        // プロパティ取得・設定メソッド
        T GetProperty<T>(string propertyName, T defaultValue = default);
        void SetProperty(string propertyName, object value);
        
        // 表示判定プロパティ
        bool IsWaitImageItem { get; }
        bool IsClickImageItem { get; }
        bool IsClickImageAIItem { get; }
        bool IsHotkeyItem { get; }
        bool IsClickItem { get; }
        bool IsWaitItem { get; }
        bool IsLoopItem { get; }
        bool IsLoopEndItem { get; }
        bool IsLoopBreakItem { get; }
        bool IsIfImageExistItem { get; }
        bool IsIfImageNotExistItem { get; }
        bool IsIfImageExistAIItem { get; }
        bool IsIfImageNotExistAIItem { get; }
        bool IsIfEndItem { get; }
        bool IsIfVariableItem { get; }
        bool IsExecuteItem { get; }
        bool IsSetVariableItem { get; }
        bool IsSetVariableAIItem { get; }
        bool IsScreenshotItem { get; }
        bool IsImageBasedItem { get; }
        bool IsAIBasedItem { get; }
        bool IsVariableItem { get; }
        bool IsLoopRelatedItem { get; }
        bool IsIfRelatedItem { get; }
        bool ShowWindowInfo { get; }
        bool ShowAdvancedSettings { get; }

        // コレクション取得メソッド
        ObservableCollection<MouseButton> MouseButtons { get; }
        ObservableCollection<Key> KeyList { get; }
        ObservableCollection<AutoTool.ViewModel.Shared.OperatorItem> Operators { get; }
        ObservableCollection<AutoTool.ViewModel.Shared.AIDetectModeItem> AiDetectModes { get; }
        ObservableCollection<AutoTool.ViewModel.Shared.BackgroundClickMethodItem> BackgroundClickMethods { get; }
        ObservableCollection<CommandDisplayItem> ItemTypes { get; }
        
        // イベント
        event EventHandler<ICommandListItem?> ItemChanged;
        event EventHandler<string> PropertyChanged;
    }

    /// <summary>
    /// EditPanelのプロパティ管理サービス実装（廃止予定）
    /// </summary>
    [Obsolete("標準MVVM方式に統一。AutoTool.ViewModel.Panels.EditPanelViewModelを使用してください", false)]
    public class EditPanelPropertyService : IEditPanelPropertyService
    {
        private readonly ILogger<EditPanelPropertyService> _logger;
        private readonly IMessenger _messenger;
        
        private ICommandListItem? _currentItem;
        private bool _isRunning = false;
        
        // コレクション
        private readonly ObservableCollection<MouseButton> _mouseButtons;
        private readonly ObservableCollection<Key> _keyList;
        private readonly ObservableCollection<AutoTool.ViewModel.Shared.OperatorItem> _operators;
        private readonly ObservableCollection<AutoTool.ViewModel.Shared.AIDetectModeItem> _aiDetectModes;
        private readonly ObservableCollection<AutoTool.ViewModel.Shared.BackgroundClickMethodItem> _backgroundClickMethods;
        private readonly ObservableCollection<CommandDisplayItem> _itemTypes;

        public ICommandListItem? CurrentItem 
        { 
            get => _currentItem;
            private set
            {
                if (_currentItem != value)
                {
                    _currentItem = value;
                    ItemChanged?.Invoke(this, value);
                    OnPropertyChanged(nameof(CurrentItem));
                }
            }
        }

        // 表示判定プロパティの実装
        public bool IsWaitImageItem => CurrentItem?.ItemType == "WaitImage";
        public bool IsClickImageItem => CurrentItem?.ItemType == "ClickImage";
        public bool IsClickImageAIItem => CurrentItem?.ItemType == "ClickImageAI";
        public bool IsHotkeyItem => CurrentItem?.ItemType == "Hotkey";
        public bool IsClickItem => CurrentItem?.ItemType == "Click";
        public bool IsWaitItem => CurrentItem?.ItemType == "Wait";
        public bool IsLoopItem => CurrentItem?.ItemType == "Loop";
        public bool IsLoopEndItem => CurrentItem?.ItemType == "LoopEnd";
        public bool IsLoopBreakItem => CurrentItem?.ItemType == "LoopBreak";
        public bool IsIfImageExistItem => CurrentItem?.ItemType == "IfImageExist";
        public bool IsIfImageNotExistItem => CurrentItem?.ItemType == "IfImageNotExist";
        public bool IsIfImageExistAIItem => CurrentItem?.ItemType == "IfImageExistAI";
        public bool IsIfImageNotExistAIItem => CurrentItem?.ItemType == "IfImageNotExistAI";
        public bool IsIfEndItem => CurrentItem?.ItemType == "IfEnd";
        public bool IsIfVariableItem => CurrentItem?.ItemType == "IfVariable";
        public bool IsExecuteItem => CurrentItem?.ItemType == "Execute";
        public bool IsSetVariableItem => CurrentItem?.ItemType == "SetVariable";
        public bool IsSetVariableAIItem => CurrentItem?.ItemType == "SetVariableAI";
        public bool IsScreenshotItem => CurrentItem?.ItemType == "Screenshot";

        // 複合条件判定
        public bool IsImageBasedItem => IsWaitImageItem || IsClickImageItem || IsIfImageExistItem || IsIfImageNotExistItem || IsScreenshotItem;
        public bool IsAIBasedItem => IsClickImageAIItem || IsIfImageExistAIItem || IsIfImageNotExistAIItem || IsSetVariableAIItem;
        public bool IsVariableItem => IsIfVariableItem || IsSetVariableItem || IsSetVariableAIItem;
        public bool IsLoopRelatedItem => IsLoopItem || IsLoopEndItem || IsLoopBreakItem;
        public bool IsIfRelatedItem => IsIfImageExistItem || IsIfImageNotExistItem || IsIfImageExistAIItem || IsIfImageNotExistAIItem || IsIfVariableItem || IsIfEndItem;

        // 表示制御プロパティ
        public bool ShowWindowInfo => IsWaitImageItem || IsClickImageItem || IsHotkeyItem || IsClickItem || IsScreenshotItem || IsAIBasedItem;
        public bool ShowAdvancedSettings => IsClickImageItem || IsClickItem || (IsAIBasedItem && !IsIfRelatedItem);

        // コレクションプロパティ
        public ObservableCollection<MouseButton> MouseButtons => _mouseButtons;
        public ObservableCollection<Key> KeyList => _keyList;
        public ObservableCollection<AutoTool.ViewModel.Shared.OperatorItem> Operators => _operators;
        public ObservableCollection<AutoTool.ViewModel.Shared.AIDetectModeItem> AiDetectModes => _aiDetectModes;
        public ObservableCollection<AutoTool.ViewModel.Shared.BackgroundClickMethodItem> BackgroundClickMethods => _backgroundClickMethods;
        public ObservableCollection<CommandDisplayItem> ItemTypes => _itemTypes;

        public event EventHandler<ICommandListItem?>? ItemChanged;
        public event EventHandler<string>? PropertyChanged;

        public EditPanelPropertyService(ILogger<EditPanelPropertyService> logger, IMessenger messenger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));

            _logger.LogWarning("EditPanelPropertyService は廃止予定です。AutoTool.ViewModel.Panels.EditPanelViewModel を使用してください。");

            // コレクションを初期化
            _mouseButtons = new ObservableCollection<MouseButton>(Enum.GetValues<MouseButton>());
            _keyList = new ObservableCollection<Key>(Enum.GetValues<Key>().OrderBy(k => k.ToString()));
            _operators = InitializeOperators();
            _aiDetectModes = InitializeAIDetectModes();
            _backgroundClickMethods = InitializeBackgroundClickMethods();
            _itemTypes = InitializeItemTypes();

            SetupMessaging();
        }

        public T GetProperty<T>(string propertyName, T defaultValue = default)
        {
            try
            {
                if (CurrentItem == null)
                    return defaultValue;

                // リフレクションでプロパティ値を取得
                var property = CurrentItem.GetType().GetProperty(propertyName);
                if (property != null)
                {
                    var value = property.GetValue(CurrentItem);
                    if (value is T typedValue)
                        return typedValue;
                }

                return defaultValue;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "プロパティ取得エラー: {PropertyName}", propertyName);
                return defaultValue;
            }
        }

        public void SetProperty(string propertyName, object value)
        {
            try
            {
                if (CurrentItem == null)
                    return;

                // リフレクションでプロパティ値を設定
                var property = CurrentItem.GetType().GetProperty(propertyName);
                if (property != null && property.CanWrite)
                {
                    property.SetValue(CurrentItem, value);
                    OnPropertyChanged(propertyName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "プロパティ設定エラー: {PropertyName} = {Value}", propertyName, value);
            }
        }

        private void SetupMessaging()
        {
            // アイテム変更メッセージを受信
            _messenger.Register<UpdateEditPanelItemMessage>(this, (r, m) =>
            {
                CurrentItem = m.Item;
                NotifyAllPropertiesChanged();
            });

            // プロパティ設定メッセージを受信
            _messenger.Register<SetEditPanelPropertyMessage>(this, (r, m) =>
            {
                SetProperty(m.PropertyName, m.Value);
            });

            // プロパティ要求メッセージを受信
            _messenger.Register<RequestEditPanelPropertyMessage>(this, (r, m) =>
            {
                var value = GetProperty<object>(m.PropertyName);
                _messenger.Send(new EditPanelPropertyResponseMessage(m.PropertyName, value));
            });

            // 実行状態変更メッセージを受信
            _messenger.Register<UpdateEditPanelRunningStateMessage>(this, (r, m) =>
            {
                _isRunning = m.IsRunning;
                OnPropertyChanged(nameof(_isRunning));
            });
        }

        private void NotifyAllPropertiesChanged()
        {
            // すべての判定プロパティの変更を通知
            var properties = new[]
            {
                nameof(IsWaitImageItem), nameof(IsClickImageItem), nameof(IsClickImageAIItem),
                nameof(IsHotkeyItem), nameof(IsClickItem), nameof(IsWaitItem),
                nameof(IsLoopItem), nameof(IsLoopEndItem), nameof(IsLoopBreakItem),
                nameof(IsIfImageExistItem), nameof(IsIfImageNotExistItem), nameof(IsIfImageExistAIItem),
                nameof(IsIfImageNotExistAIItem), nameof(IsIfEndItem), nameof(IsIfVariableItem),
                nameof(IsExecuteItem), nameof(IsSetVariableItem), nameof(IsSetVariableAIItem),
                nameof(IsScreenshotItem), nameof(IsImageBasedItem), nameof(IsAIBasedItem),
                nameof(IsVariableItem), nameof(IsLoopRelatedItem), nameof(IsIfRelatedItem),
                nameof(ShowWindowInfo), nameof(ShowAdvancedSettings)
            };

            foreach (var property in properties)
            {
                OnPropertyChanged(property);
            }
        }

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, propertyName);
        }

        private ObservableCollection<AutoTool.ViewModel.Shared.OperatorItem> InitializeOperators()
        {
            return new ObservableCollection<AutoTool.ViewModel.Shared.OperatorItem>
            {
                new AutoTool.ViewModel.Shared.OperatorItem { Key = "==", DisplayName = "等しい" },
                new AutoTool.ViewModel.Shared.OperatorItem { Key = "!=", DisplayName = "等しくない" },
                new AutoTool.ViewModel.Shared.OperatorItem { Key = ">", DisplayName = "より大きい" },
                new AutoTool.ViewModel.Shared.OperatorItem { Key = "<", DisplayName = "より小さい" },
                new AutoTool.ViewModel.Shared.OperatorItem { Key = ">=", DisplayName = "以上" },
                new AutoTool.ViewModel.Shared.OperatorItem { Key = "<=", DisplayName = "以下" },
                new AutoTool.ViewModel.Shared.OperatorItem { Key = "Contains", DisplayName = "を含む" },
                new AutoTool.ViewModel.Shared.OperatorItem { Key = "StartsWith", DisplayName = "で始まる" },
                new AutoTool.ViewModel.Shared.OperatorItem { Key = "EndsWith", DisplayName = "で終わる" },
                new AutoTool.ViewModel.Shared.OperatorItem { Key = "IsEmpty", DisplayName = "空である" },
                new AutoTool.ViewModel.Shared.OperatorItem { Key = "IsNotEmpty", DisplayName = "空でない" }
            };
        }

        private ObservableCollection<AutoTool.ViewModel.Shared.AIDetectModeItem> InitializeAIDetectModes()
        {
            return new ObservableCollection<AutoTool.ViewModel.Shared.AIDetectModeItem>
            {
                new AutoTool.ViewModel.Shared.AIDetectModeItem { Key = "Class", DisplayName = "クラス検出" },
                new AutoTool.ViewModel.Shared.AIDetectModeItem { Key = "Count", DisplayName = "個数カウント" }
            };
        }

        private ObservableCollection<AutoTool.ViewModel.Shared.BackgroundClickMethodItem> InitializeBackgroundClickMethods()
        {
            return new ObservableCollection<AutoTool.ViewModel.Shared.BackgroundClickMethodItem>
            {
                new AutoTool.ViewModel.Shared.BackgroundClickMethodItem { Value = 0, DisplayName = "SendMessage" },
                new AutoTool.ViewModel.Shared.BackgroundClickMethodItem { Value = 1, DisplayName = "PostMessage" },
                new AutoTool.ViewModel.Shared.BackgroundClickMethodItem { Value = 2, DisplayName = "子ウィンドウ検出" },
                new AutoTool.ViewModel.Shared.BackgroundClickMethodItem { Value = 3, DisplayName = "全方式試行" },
                new AutoTool.ViewModel.Shared.BackgroundClickMethodItem { Value = 4, DisplayName = "ゲーム(DirectInput)" },
                new AutoTool.ViewModel.Shared.BackgroundClickMethodItem { Value = 5, DisplayName = "ゲーム(フルスクリーン)" },
                new AutoTool.ViewModel.Shared.BackgroundClickMethodItem { Value = 6, DisplayName = "ゲーム(LowLevel)" },
                new AutoTool.ViewModel.Shared.BackgroundClickMethodItem { Value = 7, DisplayName = "ゲーム(VirtualMouse)" }
            };
        }

        private ObservableCollection<CommandDisplayItem> InitializeItemTypes()
        {
            try
            {
                AutoTool.Model.CommandDefinition.CommandRegistry.Initialize();
                
                var commandTypes = AutoTool.Model.CommandDefinition.CommandRegistry.GetOrderedTypeNames()
                    .Select(typeName => new CommandDisplayItem
                    {
                        TypeName = typeName,
                        DisplayName = AutoTool.Model.CommandDefinition.CommandRegistry.DisplayOrder.GetDisplayName(typeName),
                        Category = AutoTool.Model.CommandDefinition.CommandRegistry.DisplayOrder.GetCategoryName(typeName)
                    })
                    .ToList();

                return new ObservableCollection<CommandDisplayItem>(commandTypes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ItemTypes初期化中にエラー");
                
                // フォールバック
                return new ObservableCollection<CommandDisplayItem>
                {
                    new CommandDisplayItem { TypeName = "Wait", DisplayName = "待機", Category = "基本" }
                };
            }
        }
    }
}