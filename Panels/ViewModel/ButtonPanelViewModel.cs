using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using MacroPanels.Message;
using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.Input;
using System.Security.Cryptography.X509Certificates;
using MacroPanels.List.Class;
using MacroPanels.Model.CommandDefinition;
using MacroPanels.ViewModel.Shared;
using Microsoft.Extensions.Logging;

namespace MacroPanels.ViewModel
{
    public partial class ButtonPanelViewModel : ObservableObject
    {
        private readonly ILogger<ButtonPanelViewModel> _logger;
        private readonly ICommandRegistry _commandRegistry;

        [ObservableProperty]
        private bool _isRunning = false;

        [ObservableProperty]
        private ObservableCollection<CommandDisplayItem> _itemTypes = new();

        [ObservableProperty]
        private CommandDisplayItem? _selectedItemType;

        // DI対応コンストラクタ
        public ButtonPanelViewModel(ILogger<ButtonPanelViewModel> logger, ICommandRegistry commandRegistry)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _commandRegistry = commandRegistry ?? throw new ArgumentNullException(nameof(commandRegistry));
            _logger.LogInformation("ButtonPanelViewModel をDI対応で初期化しています");
            
            InitializeItemTypes();
        }

        private void InitializeItemTypes()
        {
            try
            {
                _logger.LogDebug("アイテムタイプの初期化を開始します");

                var displayItems = _commandRegistry.GetCommandDefinitions()
                    .Select(def => new CommandDisplayItem
                    {
                        TypeName = def.TypeName,
                        DisplayName = def.DisplayName
                    })
                    .ToList();
                
                ItemTypes = new ObservableCollection<CommandDisplayItem>(displayItems);
                SelectedItemType = ItemTypes.FirstOrDefault();
                
                _logger.LogDebug("アイテムタイプの初期化が完了しました: {Count}個", ItemTypes.Count);
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

        public void SetRunningState(bool isRunning) 
        {
            IsRunning = isRunning;
            _logger.LogDebug("実行状態を設定: {IsRunning}", isRunning);
        }

        public void Prepare() 
        {
            _logger.LogDebug("ButtonPanelViewModel の準備を実行します");
        }
    }
}