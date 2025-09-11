using System;
using System.Windows;
using AutoTool.Desktop.ViewModels;

namespace AutoTool.Desktop.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow(
            MainViewModel mainViewModel,
            ButtonPanelViewModel buttonPanelViewModel,
            EditPanelViewModel editPanelViewModel)
        {
            InitializeComponent();

            try
            {
                // ViewModelを設定
                DataContext = mainViewModel;
                ButtonPanel.DataContext = buttonPanelViewModel;
                EditPanel.DataContext = editPanelViewModel;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"MainWindow initialization error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }
        }

        // パラメータなしのコンストラクタ（デザイナー用）
        public MainWindow() : this(null!, null!, null!)
        {
            // デザイナー専用
        }
    }
}