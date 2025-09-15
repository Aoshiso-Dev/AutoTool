using System;
using System.Windows;
using AutoTool.Desktop.ViewModels;
using AutoTool.Desktop.Views.Parts; // ButtonPanel, ListPanel, EditPanel

namespace AutoTool.Desktop.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // Designer 用のパラメータなしコンストラクタを残す
        public MainWindow()
        {
            InitializeComponent();
        }

        // DI 用コンストラクタ：必要な View / ViewModel を注入
        public MainWindow(
            MainViewModel mainViewModel,
            ButtonPanel buttonPanel,
            ListPanel listPanel,
            EditPanel editPanel)
        {
            InitializeComponent();

            // ViewModel を Window 全体にセット
            DataContext = mainViewModel;

            // DI で解決した View をプレースホルダに配置
            ButtonHost.Content = buttonPanel;
            ListHost.Content = listPanel;
            EditHost.Content = editPanel;
        }
    }
}