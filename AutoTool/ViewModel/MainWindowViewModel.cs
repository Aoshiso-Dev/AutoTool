using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using AutoTool.Services.Configuration;
using AutoTool.Services.Plugin;
using AutoTool.ViewModel.Shared;
using AutoTool.Model.List.Interface;
using AutoTool.Model.List.Type;
using AutoTool.List.Class;
using AutoTool.Model.CommandDefinition;

namespace AutoTool.ViewModel
{
    /// <summary>
    /// メインウィンドウのViewModel（統合版）
    /// </summary>
    public partial class MainWindowViewModel : ObservableObject
    {
        private readonly ILogger<MainWindowViewModel> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IPluginService _pluginService;

        // マクロ実行関連
        private CancellationTokenSource? _currentCancellationTokenSource;

        // 基本プロパティ（ObservablePropertyに変更）
        [ObservableProperty]
        private string _title = "AutoTool - 統合マクロ自動化ツール";
        
        [ObservableProperty]
        private double _windowWidth = 1200;
        
        [ObservableProperty]
        private double _windowHeight = 800;
        
        [ObservableProperty]
        private WindowState _windowState = WindowState.Normal;
        
        [ObservableProperty]
        private bool _isLoading = false;
        
        [ObservableProperty]
        private bool _isRunning = false;
        
        [ObservableProperty]
        private string _statusMessage = "準備完了";
        
        [ObservableProperty]
        private string _memoryUsage = "0 MB";
        
        [ObservableProperty]
        private string _cpuUsage = "0%";
        
        [ObservableProperty]
        private int _pluginCount = 0;
        
        [ObservableProperty]
        private int _commandCount = 0;
        
        [ObservableProperty]
        private string _menuItemHeader_SaveFile = "保存(_S)";
        
        [ObservableProperty]
        private string _menuItemHeader_SaveFileAs = "名前を付けて保存(_A)";
        
        [ObservableProperty]
        private ObservableCollection<object> _recentFiles = new();

        // 統合UI関連プロパティ（CommandListを使用）
        [ObservableProperty]
        private CommandList _commandList = new();
        
        [ObservableProperty]
        private ICommandListItem? _selectedItem;
        
        [ObservableProperty]
        private int _selectedLineNumber = -1;
        
        [ObservableProperty]
        private ObservableCollection<string> _logEntries = new();
        
        [ObservableProperty]
        private ObservableCollection<CommandTypeInfo> _itemTypes = new();
        
        [ObservableProperty]
        private CommandTypeInfo? _selectedItemType;

        /// <summary>
        /// リストが空かどうか
        /// </summary>
        public bool IsListEmpty => CommandList.Items.Count == 0;

        /// <summary>
        /// リストが空ではないが選択されていないかどうか
        /// </summary>
        public bool IsListNotEmptyButNoSelection => CommandList.Items.Count > 0 && SelectedItem == null;

        /// <summary>
        /// アイテムが選択されているかどうか
        /// </summary>
        public bool IsNotNullItem => SelectedItem != null;

        /// <summary>
        /// マクロ実行可能かどうか
        /// </summary>
        public bool CanRunMacro => !IsRunning && CommandList.Items.Count > 0;

        /// <summary>
        /// マクロ停止可能かどうか
        /// </summary>
        public bool CanStopMacro => IsRunning;

        /// <summary>
        /// コマンドタイプ情報クラス
        /// </summary>
        public class CommandTypeInfo
        {
            public string TypeName { get; set; } = string.Empty;
            public string DisplayName { get; set; } = string.Empty;
            public string Category { get; set; } = string.Empty;
            public Type? ItemType { get; set; }
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public MainWindowViewModel(
            ILogger<MainWindowViewModel> logger,
            IServiceProvider serviceProvider,
            IPluginService pluginService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _pluginService = pluginService ?? throw new ArgumentNullException(nameof(pluginService));

            try
            {
                // MacroFactoryの初期化
                AutoTool.Model.MacroFactory.MacroFactory.SetServiceProvider(_serviceProvider);
                AutoTool.Model.MacroFactory.MacroFactory.SetPluginService(_pluginService);

                InitializeItemTypes();
                InitializeSampleLog();
                _logger.LogInformation("統合UI初期化完了");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "統合UI初期化中にエラーが発生しました");
            }
        }

        private void InitializeItemTypes()
        {
            try
            {
                // CommandRegistryを初期化
                CommandRegistry.Initialize();

                // 利用可能なコマンドタイプを取得
                var commandTypes = new List<CommandTypeInfo>();
                
                foreach (var typeName in CommandRegistry.GetOrderedTypeNames())
                {
                    commandTypes.Add(new CommandTypeInfo
                    {
                        TypeName = typeName,
                        DisplayName = CommandRegistry.DisplayOrder.GetDisplayName(typeName),
                        Category = CommandRegistry.DisplayOrder.GetCategoryName(typeName),
                        ItemType = typeof(BasicCommandItem) // 基本型として設定
                    });
                }

                ItemTypes = new ObservableCollection<CommandTypeInfo>(commandTypes);
                SelectedItemType = ItemTypes.FirstOrDefault();
                _logger.LogDebug("ItemTypes初期化完了: {Count}個", ItemTypes.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ItemTypes初期化中にエラーが発生しました");
            }
        }

        private void InitializeSampleLog()
        {
            try
            {
                LogEntries.Add("[00:00:00] AutoTool統合UI初期化完了");
                LogEntries.Add("[00:00:01] コマンドシステム準備完了");
                LogEntries.Add("[00:00:02] 統合パネルUI表示完了");
                _logger.LogDebug("サンプルログ初期化完了");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "サンプルログ初期化中にエラーが発生しました");
            }
        }

        // コマンド実装（CommandListItem使用）
        [RelayCommand]
        public void AddCommand()
        {
            try
            {
                _logger.LogDebug("AddCommand開始 - SelectedItemType: {SelectedItemType}", SelectedItemType?.TypeName ?? "null");
                
                if (SelectedItemType != null)
                {
                    _logger.LogDebug("CommandRegistry.CreateCommandItemを呼び出し中 - TypeName: {TypeName}", SelectedItemType.TypeName);
                    
                    // CommandRegistryを使用してコマンドアイテムを作成
                    var newItem = CommandRegistry.CreateCommandItem(SelectedItemType.TypeName);
                    
                    if (newItem == null)
                    {
                        _logger.LogWarning("CommandRegistry.CreateCommandItemがnullを返しました。具体的なクラスで直接作成");
                        
                        // フォールバック：具体的なクラスを直接作成
                        newItem = CreateSpecificCommandItem(SelectedItemType.TypeName);
                    }
                    
                    if (newItem != null)
                    {
                        _logger.LogDebug("newItem作成成功 - Type: {ItemType}", newItem.GetType().Name);
                        
                        if (string.IsNullOrEmpty(newItem.Comment))
                        {
                            newItem.Comment = $"新しい{SelectedItemType.DisplayName}コマンド";
                        }
                        
                        _logger.LogDebug("CommandListにアイテム追加前 - 現在のアイテム数: {Count}", CommandList.Items.Count);
                        
                        CommandList.Add(newItem);
                        
                        _logger.LogDebug("CommandListにアイテム追加後 - 現在のアイテム数: {Count}", CommandList.Items.Count);
                        
                        UpdateProperties();
                        _logger.LogDebug("コマンド追加完了: {ItemType}", newItem.ItemType);
                        
                        // ログに追加
                        LogEntries.Add($"[{DateTime.Now:HH:mm:ss}] コマンド追加: {SelectedItemType.DisplayName}");
                        
                        // 新しく追加したアイテムを選択
                        SelectedLineNumber = CommandList.Items.Count - 1;
                        SelectedItem = newItem;
                    }
                }
                else
                {
                    _logger.LogWarning("SelectedItemTypeがnullです");
                    LogEntries.Add($"[{DateTime.Now:HH:mm:ss}] エラー: コマンドタイプが選択されていません");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "コマンド追加中にエラーが発生しました");
                LogEntries.Add($"[{DateTime.Now:HH:mm:ss}] エラー: コマンド追加失敗 - {ex.Message}");
            }
        }

        /// <summary>
        /// 具体的なCommandListItemクラスを直接作成
        /// </summary>
        private ICommandListItem? CreateSpecificCommandItem(string typeName)
        {
            try
            {
                ICommandListItem? item = typeName switch
                {
                    "Wait_Image" => new AutoTool.Model.List.Class.WaitImageItem(),
                    "Click_Image" => new AutoTool.Model.List.Class.ClickImageItem(),
                    "Click_Image_AI" => new AutoTool.Model.List.Class.ClickImageAIItem(),
                    "Hotkey" => new AutoTool.Model.List.Class.HotkeyItem(),
                    "Click" => new AutoTool.Model.List.Class.ClickItem(),
                    "Wait" => new AutoTool.Model.List.Class.WaitItem(),
                    "Loop" => new AutoTool.Model.List.Class.LoopItem(),
                    "Loop_End" => new AutoTool.Model.List.Class.LoopEndItem(),
                    "Loop_Break" => new AutoTool.Model.List.Class.LoopBreakItem(),
                    "IF_ImageExist" => new AutoTool.Model.List.Class.IfImageExistItem(),
                    "IF_ImageNotExist" => new AutoTool.Model.List.Class.IfImageNotExistItem(),
                    "IF_End" => new AutoTool.Model.List.Class.IfEndItem(),
                    "IF_ImageExist_AI" => new AutoTool.Model.List.Class.IfImageExistAIItem(),
                    "IF_ImageNotExist_AI" => new AutoTool.Model.List.Class.IfImageNotExistAIItem(),
                    "Execute" => new AutoTool.Model.List.Class.ExecuteItem(),
                    "SetVariable" => new AutoTool.Model.List.Class.SetVariableItem(),
                    "SetVariable_AI" => new AutoTool.Model.List.Class.SetVariableAIItem(),
                    "IF_Variable" => new AutoTool.Model.List.Class.IfVariableItem(),
                    "Screenshot" => new AutoTool.Model.List.Class.ScreenshotItem(),
                    _ => new BasicCommandItem { ItemType = typeName }
                };

                if (item != null)
                {
                    item.ItemType = typeName;
                    _logger.LogDebug("具体的なクラスで作成成功: {TypeName} -> {ActualType}", typeName, item.GetType().Name);
                }

                return item;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "具体的なクラスでの作成に失敗: {TypeName}", typeName);
                return new BasicCommandItem { ItemType = typeName };
            }
        }

        [RelayCommand]
        public void DeleteCommand()
        {
            try
            {
                if (SelectedItem != null && SelectedLineNumber >= 0 && SelectedLineNumber < CommandList.Items.Count)
                {
                    var itemType = SelectedItem.ItemType;
                    CommandList.RemoveAt(SelectedLineNumber);
                    SelectedItem = null;
                    SelectedLineNumber = -1;
                    UpdateProperties();
                    _logger.LogDebug("コマンド削除実行");
                    
                    // ログに追加
                    LogEntries.Add($"[{DateTime.Now:HH:mm:ss}] コマンド削除: {itemType}");
                }
            } 
            catch (Exception ex)
            {
                _logger.LogError(ex, "コマンド削除中にエラーが発生しました");
                LogEntries.Add($"[{DateTime.Now:HH:mm:ss}] エラー: コマンド削除失敗 - {ex.Message}");
            }
        }

        [RelayCommand]
        public void UpCommand()
        {
            try
            {
                if (SelectedLineNumber > 0 && SelectedLineNumber < CommandList.Items.Count)
                {
                    CommandList.Move(SelectedLineNumber, SelectedLineNumber - 1);
                    SelectedLineNumber--;
                    UpdateSelectedItem();
                    _logger.LogDebug("コマンド上移動実行");
                    
                    // ログに追加
                    LogEntries.Add($"[{DateTime.Now:HH:mm:ss}] コマンド上移動: 行{SelectedLineNumber + 1}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "コマンド上移動中にエラーが発生しました");
                LogEntries.Add($"[{DateTime.Now:HH:mm:ss}] エラー: コマンド上移動失敗 - {ex.Message}");
            }
        }

        [RelayCommand]
        public void DownCommand()
        {
            try
            {
                if (SelectedLineNumber >= 0 && SelectedLineNumber < CommandList.Items.Count - 1)
                {
                    CommandList.Move(SelectedLineNumber, SelectedLineNumber + 1);
                    SelectedLineNumber++;
                    UpdateSelectedItem();
                    _logger.LogDebug("コマンド下移動実行");
                    
                    // ログに追加
                    LogEntries.Add($"[{DateTime.Now:HH:mm:ss}] コマンド下移動: 行{SelectedLineNumber + 1}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "コマンド下移動中にエラーが発生しました");
                LogEntries.Add($"[{DateTime.Now:HH:mm:ss}] エラー: コマンド下移動失敗 - {ex.Message}");
            }
        }

        [RelayCommand]
        public void ClearCommand()
        {
            try
            {
                var count = CommandList.Items.Count;
                CommandList.Clear();
                SelectedItem = null;
                SelectedLineNumber = -1;
                UpdateProperties();
                _logger.LogDebug("コマンドクリア実行");
                
                // ログに追加
                LogEntries.Add($"[{DateTime.Now:HH:mm:ss}] 全コマンドクリア: {count}個削除");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "コマンドクリア中にエラーが発生しました");
                LogEntries.Add($"[{DateTime.Now:HH:mm:ss}] エラー: コマンドクリア失敗 - {ex.Message}");
            }
        }

        [RelayCommand]
        public void ClearLog()
        {
            try
            {
                LogEntries.Clear();
                _logger.LogDebug("ログクリア実行");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ログクリア中にエラーが発生しました");
            }
        }

        [RelayCommand]
        public async Task RunMacro()
        {
            try
            {
                if (IsRunning)
                {
                    // 実行中の場合は停止
                    await StopMacroInternal();
                    return;
                }

                if (CommandList.Items.Count == 0)
                {
                    StatusMessage = "実行するコマンドがありません";
                    LogEntries.Add($"[{DateTime.Now:HH:mm:ss}] エラー: 実行するコマンドがありません");
                    return;
                }

                IsRunning = true;
                StatusMessage = "マクロ実行中...";
                LogEntries.Add($"[{DateTime.Now:HH:mm:ss}] マクロ実行開始 ({CommandList.Items.Count}個のコマンド)");
                _logger.LogInformation("マクロ実行開始: {Count}個のコマンド", CommandList.Items.Count);

                // CancellationTokenSourceを作成
                using var cancellationTokenSource = new CancellationTokenSource();
                _currentCancellationTokenSource = cancellationTokenSource;

                try
                {
                    // MacroFactoryでCommandListItemからCommandを生成
                    _logger.LogDebug("MacroFactoryでコマンド生成開始");
                    LogEntries.Add($"[{DateTime.Now:HH:mm:ss}] コマンド生成中...");
                    
                    var rootCommand = await Task.Run(() => 
                    {
                        return AutoTool.Model.MacroFactory.MacroFactory.CreateMacro(CommandList.Items);
                    });

                    if (rootCommand == null)
                    {
                        throw new InvalidOperationException("マクロの生成に失敗しました");
                    }

                    _logger.LogDebug("MacroFactoryでコマンド生成完了: {Type}", rootCommand.GetType().Name);
                    LogEntries.Add($"[{DateTime.Now:HH:mm:ss}] コマンド生成完了");

                    // 実行前の準備
                    await PrepareForExecution();

                    // マクロを実行
                    LogEntries.Add($"[{DateTime.Now:HH:mm:ss}] マクロ実行開始");
                    _logger.LogInformation("マクロ実行開始");

                    var executionResult = await rootCommand.Execute(cancellationTokenSource.Token);

                    // 実行結果の処理
                    if (cancellationTokenSource.Token.IsCancellationRequested)
                    {
                        StatusMessage = "マクロ実行が中断されました";
                        LogEntries.Add($"[{DateTime.Now:HH:mm:ss}] マクロ実行中断");
                        _logger.LogInformation("マクロ実行中断");
                    }
                    else if (executionResult)
                    {
                        StatusMessage = "マクロ実行完了";
                        LogEntries.Add($"[{DateTime.Now:HH:mm:ss}] マクロ実行完了");
                        _logger.LogInformation("マクロ実行完了");
                    }
                    else
                    {
                        StatusMessage = "マクロ実行失敗";
                        LogEntries.Add($"[{DateTime.Now:HH:mm:ss}] マクロ実行失敗");
                        _logger.LogWarning("マクロ実行失敗");
                    }
                }
                catch (OperationCanceledException)
                {
                    StatusMessage = "マクロ実行がキャンセルされました";
                    LogEntries.Add($"[{DateTime.Now:HH:mm:ss}] マクロ実行キャンセル");
                    _logger.LogInformation("マクロ実行キャンセル");
                }
                catch (InvalidOperationException ex) when (ex.Message.Contains("ネストが深すぎます"))
                {
                    StatusMessage = "マクロ構造エラー";
                    LogEntries.Add($"[{DateTime.Now:HH:mm:ss}] エラー: ネスト構造が深すぎます");
                    _logger.LogError(ex, "マクロ構造エラー: ネストが深すぎます");
                    MessageBox.Show("マクロのネスト構造が深すぎます。\nLoop や If の入れ子を確認してください。", 
                        "マクロ構造エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch (InvalidOperationException ex) when (ex.Message.Contains("対応する") && ex.Message.Contains("がありません"))
                {
                    StatusMessage = "マクロ構造エラー";
                    LogEntries.Add($"[{DateTime.Now:HH:mm:ss}] エラー: {ex.Message}");
                    _logger.LogError(ex, "マクロ構造エラー: ペアリング不備");
                    MessageBox.Show($"マクロの構造に問題があります。\n\n{ex.Message}", 
                        "マクロ構造エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch (Exception ex)
                {
                    StatusMessage = "マクロ実行エラー";
                    LogEntries.Add($"[{DateTime.Now:HH:mm:ss}] エラー: マクロ実行失敗 - {ex.Message}");
                    _logger.LogError(ex, "マクロ実行中にエラーが発生しました");
                    MessageBox.Show($"マクロの実行中にエラーが発生しました。\n\n{ex.Message}", 
                        "実行エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    _currentCancellationTokenSource = null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RunMacro処理中に予期しないエラーが発生しました");
                StatusMessage = "予期しないエラー";
                LogEntries.Add($"[{DateTime.Now:HH:mm:ss}] エラー: 予期しないエラー - {ex.Message}");
            }
            finally
            {
                IsRunning = false;
                await CleanupAfterExecution();
            }
        }

        [RelayCommand]
        public async Task StopMacro()
        {
            await StopMacroInternal();
        }

        /// <summary>
        /// マクロ停止の内部実装
        /// </summary>
        private async Task StopMacroInternal()
        {
            try
            {
                if (_currentCancellationTokenSource != null)
                {
                    _logger.LogDebug("マクロ実行キャンセル要求");
                    _currentCancellationTokenSource.Cancel();
                    
                    // キャンセルの完了を少し待つ
                    await Task.Delay(100);
                }

                IsRunning = false;
                StatusMessage = "マクロ実行停止";
                LogEntries.Add($"[{DateTime.Now:HH:mm:ss}] マクロ実行停止");
                _logger.LogDebug("マクロ停止完了");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "マクロ停止中にエラーが発生しました");
                IsRunning = false;
                StatusMessage = "停止エラー";
            }
        }

        // ファイル操作コマンド（実際のCommandListを使用）
        [RelayCommand]
        public async Task OpenFile()
        {
            try
            {
                var openFileDialog = new Microsoft.Win32.OpenFileDialog
                {
                    Filter = "マクロファイル (*.json)|*.json|すべてのファイル (*.*)|*.*",
                    Title = "マクロファイルを開く"
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    IsLoading = true;
                    StatusMessage = "ファイル読み込み中...";
                    LogEntries.Add($"[{DateTime.Now:HH:mm:ss}] ファイル読み込み開始: {openFileDialog.FileName}");

                    try
                    {
                        // バックグラウンドスレッドで読み込み処理（ファイルI/O部分のみ）
                        // ObservableCollectionの操作はLoadメソッド内でDispatcher.Invokeで実行される
                        await Task.Run(() => 
                        {
                            CommandList.Load(openFileDialog.FileName);
                        });
                        
                        // UI更新はUIスレッドで実行
                        UpdateProperties();
                        StatusMessage = $"ファイルを読み込みました: {Path.GetFileName(openFileDialog.FileName)}";
                        LogEntries.Add($"[{DateTime.Now:HH:mm:ss}] ファイル読み込み完了: {CommandList.Items.Count}個のコマンド");
                        _logger.LogInformation("ファイル読み込み完了: {FileName}, {Count}個のコマンド", openFileDialog.FileName, CommandList.Items.Count);
                    }
                    catch (FileNotFoundException)
                    {
                        StatusMessage = "ファイルが見つかりません";
                        LogEntries.Add($"[{DateTime.Now:HH:mm:ss}] エラー: ファイルが見つかりません");
                        MessageBox.Show("指定されたファイルが見つかりません。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    catch (InvalidDataException ex)
                    {
                        StatusMessage = "ファイル形式エラー";
                        LogEntries.Add($"[{DateTime.Now:HH:mm:ss}] エラー: ファイル形式が無効 - {ex.Message}");
                        MessageBox.Show($"ファイル形式が無効です。\n\n詳細: {ex.Message}", "ファイル形式エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    catch (JsonException ex)
                    {
                        StatusMessage = "JSON解析エラー";
                        LogEntries.Add($"[{DateTime.Now:HH:mm:ss}] エラー: JSON解析失敗 - {ex.Message}");
                        MessageBox.Show($"JSONファイルの解析に失敗しました。\n\nファイルが破損している可能性があります。\n\n詳細: {ex.Message}", "JSON解析エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    catch (InvalidOperationException ex) when (ex.Message.Contains("Dispatcher"))
                    {
                        StatusMessage = "UI更新エラー";
                        LogEntries.Add($"[{DateTime.Now:HH:mm:ss}] エラー: UIスレッドエラー - {ex.Message}");
                        MessageBox.Show($"UIの更新中にエラーが発生しました。\n\nアプリケーションを再起動してください。\n\n詳細: {ex.Message}", "UIエラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                    finally
                    {
                        IsLoading = false;
                    }
                }
            }
            catch (Exception ex)
            {
                IsLoading = false;
                _logger.LogError(ex, "ファイル読み込み中に予期しないエラーが発生しました");
                StatusMessage = "読み込みエラー";
                LogEntries.Add($"[{DateTime.Now:HH:mm:ss}] エラー: 予期しないエラー - {ex.Message}");
                MessageBox.Show($"ファイルの読み込み中に予期しないエラーが発生しました。\n\n{ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        public async Task SaveFile()
        {
            try
            {
                var saveFileDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "マクロファイル (*.json)|*.json|すべてのファイル (*.*)|*.*",
                    Title = "マクロファイルを保存",
                    DefaultExt = "json"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    CommandList.Save(saveFileDialog.FileName);
                    StatusMessage = $"ファイルを保存しました: {Path.GetFileName(saveFileDialog.FileName)}";
                    LogEntries.Add($"[{DateTime.Now:HH:mm:ss}] ファイル保存: {saveFileDialog.FileName}");
                    _logger.LogInformation("ファイル保存完了: {FileName}", saveFileDialog.FileName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ファイル保存中にエラーが発生しました");
                StatusMessage = "ファイル保存エラー";
                LogEntries.Add($"[{DateTime.Now:HH:mm:ss}] エラー: ファイル保存失敗 - {ex.Message}");
                MessageBox.Show($"ファイルの保存に失敗しました。\n\n{ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand] 
        public async Task SaveFileAs() => await SaveFile(); // SaveFileと同じ処理

        [RelayCommand] public void Exit() => Application.Current?.Shutdown();
        [RelayCommand] public void Undo() => _logger.LogDebug("元に戻す（未実装）");
        [RelayCommand] public void Redo() => _logger.LogDebug("やり直し（未実装）");
        [RelayCommand] public void ChangeTheme(string theme) => _logger.LogDebug("テーマ変更: {Theme}（未実装）", theme);
        [RelayCommand] public void RefreshPerformance() => _logger.LogDebug("パフォーマンス更新（未実装）");
        [RelayCommand] public void LoadPluginFile() => _logger.LogDebug("プラグイン読み込み（未実装）");
        [RelayCommand] public void RefreshPlugins() => _logger.LogDebug("プラグイン再読み込み（未実装）");
        [RelayCommand] public void ShowPluginInfo() => _logger.LogDebug("プラグイン情報表示（未実装）");
        [RelayCommand] public void OpenAppDir() => _logger.LogDebug("アプリフォルダ開く（未実装）");
        [RelayCommand] public void ShowAbout() => _logger.LogDebug("バージョン情報表示（未実装）");

        /// <summary>
        /// ウィンドウ設定の保存
        /// </summary>
        public void SaveWindowSettings()
        {
            try
            {
                _logger.LogDebug("ウィンドウ設定保存（未実装）");
                // 今後実装予定
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ウィンドウ設定保存中にエラーが発生しました");
            }
        }

        /// <summary>
        /// クリーンアップ処理
        /// </summary>
        public void Cleanup()
        {
            try
            {
                _logger.LogDebug("クリーンアップ処理実行");
                // 今後実装予定
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "クリーンアップ処理中にエラーが発生しました");
            }
        }

        /// <summary>
        /// 選択されたアイテムを更新
        /// </summary>
        private void UpdateSelectedItem()
        {
            if (SelectedLineNumber >= 0 && SelectedLineNumber < CommandList.Items.Count)
            {
                SelectedItem = CommandList.Items[SelectedLineNumber];
            }
            else
            {
                SelectedItem = null;
            }
        }

        private void UpdateProperties()
        {
            OnPropertyChanged(nameof(IsListEmpty));
            OnPropertyChanged(nameof(IsListNotEmptyButNoSelection));
            OnPropertyChanged(nameof(IsNotNullItem));
            OnPropertyChanged(nameof(CanRunMacro));
            OnPropertyChanged(nameof(CanStopMacro));
            CommandCount = CommandList.Items.Count;
        }

        partial void OnSelectedLineNumberChanged(int value)
        {
            UpdateSelectedItem();
            UpdateProperties();
        }

        partial void OnCommandListChanged(CommandList value)
        {
            _logger.LogDebug("CommandListが変更されました");
            UpdateProperties();
        }

        partial void OnIsRunningChanged(bool value)
        {
            OnPropertyChanged(nameof(CanRunMacro));
            OnPropertyChanged(nameof(CanStopMacro));
            _logger.LogDebug("マクロ実行状態変更: {IsRunning}", value);
        }

        [RelayCommand]
        public void AddTestCommand()
        {
            try
            {
                // テスト用の簡単なコマンドを直接作成
                var testItem = new BasicCommandItem
                {
                    ItemType = "Test",
                    Comment = "テストコマンド",
                    Description = "テスト用のコマンドです"
                };
                
                CommandList.Add(testItem);
                UpdateProperties();
                LogEntries.Add($"[{DateTime.Now:HH:mm:ss}] テストコマンド追加");
                _logger.LogDebug("テストコマンド追加完了");
                
                // 新しく追加したアイテムを選択
                SelectedLineNumber = CommandList.Items.Count - 1;
                SelectedItem = testItem;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "テストコマンド追加中にエラーが発生しました");
                LogEntries.Add($"[{DateTime.Now:HH:mm:ss}] エラー: テストコマンド追加失敗 - {ex.Message}");
            }
        }

        /// <summary>
        /// マクロ実行前の準備処理
        /// </summary>
        private async Task PrepareForExecution()
        {
            try
            {
                _logger.LogDebug("マクロ実行前準備開始");
                
                // 全アイテムの実行状態をリセット
                foreach (var item in CommandList.Items)
                {
                    item.IsRunning = false;
                    item.Progress = 0;
                }

                // MacroFactoryにServiceProviderとPluginServiceを設定
                AutoTool.Model.MacroFactory.MacroFactory.SetServiceProvider(_serviceProvider);
                if (_pluginService != null)
                {
                    AutoTool.Model.MacroFactory.MacroFactory.SetPluginService(_pluginService);
                }

                // UIの更新（非同期）
                await Task.Delay(50);
                
                _logger.LogDebug("マクロ実行前準備完了");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "マクロ実行前準備中にエラーが発生しました");
                throw;
            }
        }

        /// <summary>
        /// マクロ実行後のクリーンアップ処理
        /// </summary>
        private async Task CleanupAfterExecution()
        {
            try
            {
                _logger.LogDebug("マクロ実行後クリーンアップ開始");
                
                // 全アイテムの実行状態をリセット
                foreach (var item in CommandList.Items)
                {
                    item.IsRunning = false;
                    item.Progress = 0;
                }

                // プロパティ更新
                UpdateProperties();

                // UIの更新（非同期）
                await Task.Delay(50);
                
                _logger.LogDebug("マクロ実行後クリーンアップ完了");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "マクロ実行後クリーンアップ中にエラーが発生しました");
            }
        }
    }
}
