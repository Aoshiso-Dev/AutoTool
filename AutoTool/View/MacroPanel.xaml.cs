using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Extensions.Logging;
using AutoTool.ViewModel;

namespace AutoTool.View
{
    /// <summary>
    /// Interaction logic for MacroPanel.xaml
    /// </summary>
    public partial class MacroPanel : UserControl
    {
        private readonly ILogger<MacroPanel>? _logger;

        // パラメータなしコンストラクタ（XAML用）
        public MacroPanel()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("=== MacroPanel パラメータなしコンストラクタ呼び出し ===");
                InitializeComponent();
                System.Diagnostics.Debug.WriteLine("=== MacroPanel InitializeComponent 完了 ===");

                // デバッグ用：イベントハンドラを追加
                DataContextChanged += MacroPanel_DataContextChanged;
                Loaded += MacroPanel_Loaded;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"MacroPanel 初期化エラー: {ex}");
                throw;
            }
        }

        // DI対応コンストラクタ（コードで使用）
        public MacroPanel(ILogger<MacroPanel> logger) : this()
        {
            _logger = logger;
            _logger.LogInformation("MacroPanel をDI対応で初期化しました");
        }

        private void MacroPanel_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("=== MacroPanel DataContext Changed ===");
                System.Diagnostics.Debug.WriteLine($"Old: {e.OldValue?.GetType().Name ?? "null"}");
                System.Diagnostics.Debug.WriteLine($"New: {e.NewValue?.GetType().Name ?? "null"}");
                
                if (e.NewValue is MacroPanelViewModel viewModel)
                {
                    System.Diagnostics.Debug.WriteLine($"MacroPanelViewModel detected!");
                    System.Diagnostics.Debug.WriteLine($"ButtonPanelViewModel: {viewModel.ButtonPanelViewModel?.GetType().Name ?? "null"}");
                    System.Diagnostics.Debug.WriteLine($"ListPanelViewModel: {viewModel.ListPanelViewModel?.GetType().Name ?? "null"}");
                    System.Diagnostics.Debug.WriteLine($"EditPanelViewModel: {viewModel.EditPanelViewModel?.GetType().Name ?? "null"}");
                    System.Diagnostics.Debug.WriteLine($"LogPanelViewModel: {viewModel.LogPanelViewModel?.GetType().Name ?? "null"}");
                    
                    // ButtonPanelViewModelの詳細情報
                    if (viewModel.ButtonPanelViewModel != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"ButtonPanelViewModel ItemTypes count: {viewModel.ButtonPanelViewModel.ItemTypes?.Count ?? 0}");
                        System.Diagnostics.Debug.WriteLine($"ButtonPanelViewModel SelectedItemType: {viewModel.ButtonPanelViewModel.SelectedItemType?.DisplayName ?? "null"}");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("DataContext is not MacroPanelViewModel!");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"MacroPanel DataContext change error: {ex.Message}");
                _logger?.LogError(ex, "MacroPanel DataContext変更中にエラーが発生しました");
            }
        }

        private void MacroPanel_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("=== MacroPanel Loaded ===");
                System.Diagnostics.Debug.WriteLine($"DataContext: {DataContext?.GetType().Name ?? "null"}");
                
                if (DataContext is MacroPanelViewModel viewModel)
                {
                    System.Diagnostics.Debug.WriteLine($"MacroPanelViewModel loaded successfully!");
                    
                    // 各パネルVMの状態をチェック
                    if (viewModel.ButtonPanelViewModel != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"ButtonPanelViewModel ItemTypes count at load: {viewModel.ButtonPanelViewModel.ItemTypes?.Count ?? 0}");
                        
                        // 最初の数個のアイテムをログ出力
                        var itemTypes = viewModel.ButtonPanelViewModel.ItemTypes;
                        if (itemTypes != null && itemTypes.Count > 0)
                        {
                            for (int i = 0; i < Math.Min(5, itemTypes.Count); i++)
                            {
                                var item = itemTypes[i];
                                System.Diagnostics.Debug.WriteLine($"  Item[{i}]: {item.TypeName} -> {item.DisplayName}");
                            }
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("  ButtonPanelViewModel.ItemTypes is empty or null!");
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("ButtonPanelViewModel is null!");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("DataContext is not MacroPanelViewModel at load time!");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"MacroPanel Loaded error: {ex.Message}");
                _logger?.LogError(ex, "MacroPanel Loaded中にエラーが発生しました");
            }
        }
    }
}