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
using AutoTool.Model;

namespace AutoTool
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            
            // ウィンドウが表示される前に設定を適用
            this.SourceInitialized += MainWindow_SourceInitialized;
            
            // ウィンドウが完全に読み込まれた後に前回のファイルを開く
            this.Loaded += MainWindow_Loaded;
            
            // ウィンドウが閉じる時に設定を保存
            this.Closing += MainWindow_Closing;
        }

        private void MainWindow_SourceInitialized(object sender, EventArgs e)
        {
            // ViewModelからウィンドウ設定を取得して適用
            if (DataContext is MainWindowViewModel viewModel)
            {
                var windowSettings = viewModel.GetWindowSettings();
                windowSettings.ApplyToWindow(this);
            }
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // 起動時に前回のファイルを開く
            if (DataContext is MainWindowViewModel viewModel)
            {
                // デバッグ情報を出力
                viewModel.PrintDebugSettings();
                
                // UIが完全に初期化された後に遅延実行
                Dispatcher.BeginInvoke(() =>
                {
                    viewModel.LoadLastOpenedFileOnStartup();
                }, System.Windows.Threading.DispatcherPriority.ApplicationIdle);
            }
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // 現在のウィンドウ状態を設定に保存
            if (DataContext is MainWindowViewModel viewModel)
            {
                var windowSettings = viewModel.GetWindowSettings();
                windowSettings.UpdateFromWindow(this);
                windowSettings.Save();
            }
        }
    }
}