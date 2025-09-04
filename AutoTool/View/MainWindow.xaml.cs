using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using AutoTool.ViewModel;
using AutoTool.ViewModel.Panels;
using AutoTool.Services.UI;

namespace AutoTool
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック(DI + Messaging対応)
    /// </summary>
    public partial class MainWindow : Window
    {
        private ILogger<MainWindow>? _logger;
        private IServiceProvider? _serviceProvider;
        private IDataContextLocator? _dataContextLocator;

        /// <summary>
        /// MainWindowのコンストラクタ
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            
            // Loadedイベントでワンタイムのみ初期化
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                InitializeDI();
                SetupViewModels();
                TestVariableStore();
                
                _logger?.LogInformation("MainWindow DI初期化完了 - All services resolved successfully");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "MainWindow DI初期化中にエラーが発生");
                
                MessageBox.Show(
                    $"MainWindow初期化中にエラーが発生しました。\n\nエラー詳細:\n{ex.Message}",
                    "警告",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
            }
        }

        /// <summary>
        /// DI初期化
        /// </summary>
        private void InitializeDI()
        {
            if (Application.Current is App app && app._host != null)
            {
                _serviceProvider = app._host.Services;
                _logger = _serviceProvider.GetService<ILogger<MainWindow>>();
                _dataContextLocator = _serviceProvider.GetService<IDataContextLocator>();

                _logger?.LogDebug("MainWindow DI初期化完了");
            }
            else
            {
                throw new InvalidOperationException("DIコンテナが利用できません");
            }
        }

        /// <summary>
        /// ViewModelを設定
        /// </summary>
        private void SetupViewModels()
        {
            if (_serviceProvider == null) return;

            try
            {
                // MainWindowのViewModel設定
                var mainViewModel = _serviceProvider.GetRequiredService<MainWindowViewModel>();
                DataContext = mainViewModel;
                _logger?.LogDebug("MainWindowViewModel設定完了");

                // ListPanelのViewModel設定
                var listPanelViewModel = _serviceProvider.GetRequiredService<ListPanelViewModel>();
                CommandListPanel.DataContext = listPanelViewModel;
                _logger?.LogDebug("ListPanelViewModel設定完了");

                // EditPanelのViewModel設定
                var editPanelViewModel = _serviceProvider.GetRequiredService<EditPanelViewModel>();
                EditPanelViewControl.DataContext = editPanelViewModel;
                _logger?.LogDebug("EditPanelViewModel設定完了");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "ViewModel設定中にエラーが発生");
                throw;
            }
        }

        /// <summary>
        /// VariableStoreの動作確認
        /// </summary>
        private void TestVariableStore()
        {
            if (_serviceProvider == null) return;

            try
            {
                var variableStore = _serviceProvider.GetService<AutoTool.Services.IVariableStore>();
                if (variableStore == null)
                {
                    _logger?.LogWarning("AutoTool.Services.IVariableStore service is not registered in DI container");
                }
                else
                {
                    _logger?.LogDebug("AutoTool.Services.IVariableStore service successfully resolved from DI container");
                    
                    // 動作テスト
                    variableStore.Set("TestVariable", "Hello AutoTool DI!");
                    var testValue = variableStore.Get("TestVariable");
                    _logger?.LogInformation("VariableStore動作テスト: TestVariable = {Value} (Count: {Count})", 
                        testValue, variableStore.Count);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "VariableStore動作確認中にエラーが発生");
            }
        }

        /// <summary>
        /// ウィンドウを閉じる際の処理
        /// </summary>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                if (DataContext is MainWindowViewModel viewModel)
                {
                    viewModel.SaveWindowSettings();
                    viewModel.Cleanup();
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "ウィンドウクローズ処理中にエラーが発生");
                // ignore - アプリケーション終了時なのでエラーを無視
            }
        }

        private void DebugStateButton_Click(object sender, RoutedEventArgs e)
        {
            // 既存のデバッグハンドラがある想定。必要であれば実装。
        }
    }
}