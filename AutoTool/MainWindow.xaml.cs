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
        private WindowSettings _windowSettings;

        public MainWindow()
        {
            InitializeComponent();
            
            // ウィンドウ設定を読み込み
            _windowSettings = WindowSettings.Load();
            
            // ウィンドウが表示される前に設定を適用
            this.SourceInitialized += MainWindow_SourceInitialized;
            
            // ウィンドウが閉じる時に設定を保存
            this.Closing += MainWindow_Closing;
        }

        private void MainWindow_SourceInitialized(object sender, EventArgs e)
        {
            // ウィンドウ設定を適用
            _windowSettings.ApplyToWindow(this);
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // 現在のウィンドウ状態を設定に保存
            _windowSettings.UpdateFromWindow(this);
            _windowSettings.Save();
        }
    }
}