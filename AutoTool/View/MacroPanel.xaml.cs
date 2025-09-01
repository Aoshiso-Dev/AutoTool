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
    }
}