using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MacroPanels.ViewModel;

namespace MacroPanels.View
{
    /// <summary>
    /// ButtonPanel.xaml の相互作用ロジック
    /// </summary>
    public partial class ButtonPanel : UserControl
    {
        public ButtonPanel()
        {
            InitializeComponent();
            
            // DataContextChangedイベントを監視
            DataContextChanged += ButtonPanel_DataContextChanged;
            Loaded += ButtonPanel_Loaded;
        }

        private void ButtonPanel_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"=== ButtonPanel DataContext Changed ===");
                System.Diagnostics.Debug.WriteLine($"Old: {e.OldValue?.GetType().Name ?? "null"}");
                System.Diagnostics.Debug.WriteLine($"New: {e.NewValue?.GetType().Name ?? "null"}");
                
                if (e.NewValue is ButtonPanelViewModel viewModel)
                {
                    System.Diagnostics.Debug.WriteLine($"ButtonPanelViewModel detected");
                    System.Diagnostics.Debug.WriteLine($"ItemTypes count: {viewModel.ItemTypes?.Count ?? 0}");
                    System.Diagnostics.Debug.WriteLine($"SelectedItemType: {viewModel.SelectedItemType?.DisplayName ?? "null"}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ButtonPanel DataContext change error: {ex.Message}");
            }
        }

        private void ButtonPanel_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"=== ButtonPanel Loaded ===");
                System.Diagnostics.Debug.WriteLine($"DataContext: {DataContext?.GetType().Name ?? "null"}");
                
                if (DataContext is ButtonPanelViewModel viewModel)
                {
                    System.Diagnostics.Debug.WriteLine($"ItemTypes count at load: {viewModel.ItemTypes?.Count ?? 0}");
                    
                    // コンボボックスの状態をチェック
                    if (CommandComboBox != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"ComboBox ItemsSource: {CommandComboBox.ItemsSource != null}");
                        System.Diagnostics.Debug.WriteLine($"ComboBox Items count: {CommandComboBox.Items.Count}");
                        System.Diagnostics.Debug.WriteLine($"ComboBox SelectedItem: {CommandComboBox.SelectedItem}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ButtonPanel Loaded error: {ex.Message}");
            }
        }
    }
}
