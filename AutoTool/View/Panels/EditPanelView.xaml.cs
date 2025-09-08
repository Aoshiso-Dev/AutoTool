using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using AutoTool.ViewModel.Panels;
using AutoTool.Command.Definition;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using Xceed.Wpf.Toolkit.PropertyGrid;

namespace AutoTool.View.Panels
{
    /// <summary>
    /// EditPanelView.xaml の相互作用ロジック（サービス分離版）
    /// </summary>
    public partial class EditPanelView : System.Windows.Controls.UserControl
    {
        private ILogger<EditPanelView>? _logger;

        public EditPanelView()
        {
            InitializeComponent();
        }

        public EditPanelView(ILogger<EditPanelView> logger)
        {
            InitializeComponent();

            _logger = logger;
        }

        // DataTemplate 内の Button.Click から来る
        private void OnBrowseImagePath_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is PropertyItem item)
            {
                var dlg = new OpenFileDialog
                {
                    Filter = "画像 (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp|すべて (*.*)|*.*"
                };
                if (dlg.ShowDialog() == true)
                    item.Value = dlg.FileName;
            }
        }

        private void OnBrowseOnnxPath_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is PropertyItem item)
            {
                var dlg = new OpenFileDialog
                {
                    Filter = "ONNX (*.onnx)|*.onnx|すべて (*.*)|*.*"
                };
                if (dlg.ShowDialog() == true)
                    item.Value = dlg.FileName;
            }
        }

        private void OnPickPoint_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is PropertyItem item)
            {
                // TODO: ここで独自の座標取得UIを呼ぶ
                // 仮値：
                var p = new System.Windows.Point(123, 456);
                item.Value = p; // PropertyGrid が X/Y に反映してくれる
            }
        }

        private void OnPickWindowTitle_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is PropertyItem item)
            {
                // TODO: あなたのウィンドウ選択ダイアログをここで呼ぶ
                // 仮値：
                item.Value = "選択したウィンドウのタイトル";
            }
        }

        private void OnPickWindowClassName_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is PropertyItem item)
            {
                // TODO: あなたのウィンドウ選択ダイアログをここで呼ぶ
                // 仮値：
                item.Value = "選択したウィンドウのクラス名";
            }
        }
    }
}