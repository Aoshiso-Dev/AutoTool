using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using AutoTool.Model.CommandDefinition;
using AutoTool.ViewModel.Shared;
using AutoTool.Message;

namespace AutoTool.ViewModel.Panels
{
    /// <summary>
    /// 統合されたButtonPanelViewModel（AutoTool.ViewModel名前空間）
    /// Phase 3: 完全統合実装版、統合CommandDisplayItem使用
    /// </summary>
    public partial class ButtonPanelViewModel : ObservableObject
    {
        private readonly ILogger<ButtonPanelViewModel> _logger;

        [ObservableProperty]
        private bool _isRunning = false;

        [ObservableProperty]
        private ObservableCollection<AutoTool.ViewModel.Shared.CommandDisplayItem> _itemTypes = new();

        [ObservableProperty]
        private AutoTool.ViewModel.Shared.CommandDisplayItem? _selectedItemType;

        // DI対応コンストラクタ（Phase 3完全統合版）
        public ButtonPanelViewModel(ILogger<ButtonPanelViewModel> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logger.LogInformation("Phase 3完全統合ButtonPanelViewModel をDI対応で初期化しています");
            
            InitializeItemTypes();
        }

        private void InitializeItemTypes()
        {
            try
            {
                _logger.LogDebug("アイテムタイプの初期化を開始します");

                // CommandRegistryを初期化
                AutoTool.Model.CommandDefinition.CommandRegistry.Initialize();
                _logger.LogDebug("CommandRegistry.Initialize() 完了");

                // 統合版CommandDisplayItemを使用してアイテムタイプを取得
                var orderedTypeNames = AutoTool.Model.CommandDefinition.CommandRegistry.GetOrderedTypeNames();
                _logger.LogDebug("GetOrderedTypeNames()で取得した型名数: {Count}", orderedTypeNames?.Count() ?? 0);

                var displayItems = orderedTypeNames?
                    .Select(typeName => new CommandDisplayItem // AutoTool.ViewModel.Shared版を使用
                    {
                        TypeName = typeName,
                        DisplayName = CommandRegistry.DisplayOrder.GetDisplayName(typeName),
                        Category = CommandRegistry.DisplayOrder.GetCategoryName(typeName)
                    })
                    .ToList() ?? new List<CommandDisplayItem>();
                
                _logger.LogDebug("作成したdisplayItems数: {Count}", displayItems.Count);
                
                ItemTypes = new ObservableCollection<CommandDisplayItem>(displayItems);
                SelectedItemType = ItemTypes.FirstOrDefault();
                
                _logger.LogDebug("Phase 3完全統合ButtonPanelViewModel初期化が完了しました: {Count}個", ItemTypes.Count);
                _logger.LogDebug("選択されたアイテム: {DisplayName}", SelectedItemType?.DisplayName ?? "なし");
                
                // デバッグ用：最初の数項目をログ出力
                for (int i = 0; i < Math.Min(5, ItemTypes.Count); i++)
                {
                    var item = ItemTypes[i];
                    _logger.LogDebug("ItemType[{Index}]: {DisplayName} ({TypeName})", i, item.DisplayName, item.TypeName);
                }

                // さらに詳細なデバッグ情報
                if (ItemTypes.Count == 0)
                {
                    _logger.LogError("ItemTypesが空です！CommandRegistryの状態を確認します。");
                    
                    // CommandRegistryの状態をチェック
                    var allTypes = CommandRegistry.GetOrderedTypeNames();
                    if (allTypes == null)
                    {
                        _logger.LogError("CommandRegistry.GetOrderedTypeNames() が null を返しました");
                    }
                    else
                    {
                        _logger.LogError("CommandRegistry.GetOrderedTypeNames() は {Count} 個の型を返しました", allTypes.Count());
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "アイテムタイプの初期化中にエラーが発生しました");
                throw;
            }
        }

        [RelayCommand]
        public void Run()
        {
            try
            {
                if (IsRunning)
                {
                    _logger.LogInformation("停止コマンドを送信します");
                    WeakReferenceMessenger.Default.Send(new StopMessage());
                }
                else
                {
                    _logger.LogInformation("実行コマンドを送信します");
                    WeakReferenceMessenger.Default.Send(new RunMessage());
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "実行/停止コマンドの処理中にエラーが発生しました");
            }
        }

        [RelayCommand]
        public void Save() 
        {
            _logger.LogDebug("保存コマンドを送信します");
            WeakReferenceMessenger.Default.Send(new SaveMessage());
        }

        [RelayCommand]
        public void Load() 
        {
            _logger.LogDebug("読み込みコマンドを送信します");
            WeakReferenceMessenger.Default.Send(new LoadMessage());
        }

        [RelayCommand]
        public void Clear() 
        {
            _logger.LogDebug("クリアコマンドを送信します");
            WeakReferenceMessenger.Default.Send(new ClearMessage());
        }

        [RelayCommand]
        public void Add() 
        {
            try
            {
                if (SelectedItemType != null)
                {
                    _logger.LogDebug("追加コマンドを送信します: {ItemType}", SelectedItemType.TypeName);
                    WeakReferenceMessenger.Default.Send(new AddMessage(SelectedItemType.TypeName));
                }
                else
                {
                    _logger.LogWarning("アイテムタイプが選択されていません");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "追加コマンドの処理中にエラーが発生しました");
            }
        }

        [RelayCommand]
        public void Up() 
        {
            _logger.LogDebug("上移動コマンドを送信します");
            WeakReferenceMessenger.Default.Send(new UpMessage());
        }

        [RelayCommand]
        public void Down() 
        {
            _logger.LogDebug("下移動コマンドを送信します");
            WeakReferenceMessenger.Default.Send(new DownMessage());
        }

        [RelayCommand]
        public void Delete() 
        {
            _logger.LogDebug("削除コマンドを送信します");
            WeakReferenceMessenger.Default.Send(new DeleteMessage());
        }

        [RelayCommand]
        public void Undo() 
        {
            try
            {
                _logger.LogDebug("元に戻すコマンドを送信します");
                WeakReferenceMessenger.Default.Send(new UndoMessage());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "元に戻すコマンドの処理中にエラーが発生しました");
            }
        }

        [RelayCommand]
        public void Redo() 
        {
            try
            {
                _logger.LogDebug("やり直しコマンドを送信します");
                WeakReferenceMessenger.Default.Send(new RedoMessage());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "やり直しコマンドの処理中にエラーが発生しました");
            }
        }

        public void SetRunningState(bool isRunning) 
        {
            IsRunning = isRunning;
            _logger.LogDebug("実行状態を設定: {IsRunning}", isRunning);
        }

        public void Prepare() 
        {
            _logger.LogDebug("Phase 3完全統合ButtonPanelViewModel の準備を実行します");
        }
    }
}