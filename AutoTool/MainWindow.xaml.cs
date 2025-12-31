using System.Windows;
using AutoTool.Model;
using Wpf.Ui.Controls;

namespace AutoTool;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : FluentWindow
{
    private readonly WindowSettings _windowSettings;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = App.GetService<MainWindowViewModel>();
        
        _windowSettings = WindowSettings.Load();
        SourceInitialized += MainWindow_SourceInitialized;
        Closing += MainWindow_Closing;
    }

    private void MainWindow_SourceInitialized(object? sender, EventArgs e)
    {
        _windowSettings.ApplyToWindow(this);
    }

    private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        _windowSettings.UpdateFromWindow(this);
        _windowSettings.Save();
    }
}
