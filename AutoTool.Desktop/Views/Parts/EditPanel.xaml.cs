using AutoTool.Core.Abstractions;
using AutoTool.Desktop.ViewModels;
using AutoTool.Services.Abstractions;
using AutoTool.Services.Implementations;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Xceed.Wpf.Toolkit.PropertyGrid; // xctk PropertyItem 型を扱うため

namespace AutoTool.Desktop.Views.Parts
{
    /// <summary>
    /// EditPanel.xaml の相互作用ロジック
    /// </summary>
    public partial class EditPanel : UserControl
    {
        private readonly IWindowCaptureService? _windowCaptureService;
        private readonly IMouseService? _mouseService;

        public EditPanel(EditPanelViewModel editPanelViewModel, 
                        IWindowCaptureService? windowCaptureService,
                        IMouseService? mouseService)
        {
            DataContext = editPanelViewModel ?? throw new ArgumentNullException(nameof(editPanelViewModel));
            _windowCaptureService = windowCaptureService ?? throw new ArgumentNullException(nameof(windowCaptureService));
            _mouseService = mouseService ?? throw new ArgumentNullException(nameof(mouseService));

            InitializeComponent();
        }

        private void SetValue(object sender, object value)
        {
            if (sender is not FrameworkElement fe) return;
            if (fe.DataContext is not PropertyItem propertyItem) return;

            var settings = propertyItem.Instance as IAutoToolCommandSettings;
            var propName = propertyItem.PropertyName;
            if (settings == null || string.IsNullOrEmpty(propName)) return;

            // PropertyDescriptor を使って安全に設定を試みる
            propertyItem.PropertyDescriptor?.SetValue(propertyItem.Instance, value);
        }

        private void OnBrowseImagePath_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Filter = "画像 (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp|すべて (*.*)|*.*"
            };
            if (dlg.ShowDialog() == true)
            {
                SetValue(sender, dlg.FileName);
            }
        }

        private void OnBrowseOnnxPath_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Filter = "ONNX (*.onnx)|*.onnx|すべて (*.*)|*.*"
            };
            if (dlg.ShowDialog() == true)
            {
                SetValue(sender, dlg.FileName);
            }
        }

        private void OnPickPoint_Click(object sender, RoutedEventArgs e)
        {
            _mouseService!.WaitForRightClickAsync().ContinueWith(t =>
            {
                var result = t.Result;
                Application.Current.Dispatcher.Invoke(() =>
                {
                    SetValue(sender, result);
                });
            });
        }

        private void OnPickWindowTitle_Click(object sender, RoutedEventArgs e)
        {
            _windowCaptureService!.CaptureWindowInfoAtRightClickAsync().ContinueWith(t =>
            {
                var result = t.Result;
                if (result != null)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        SetValue(sender, result.WindowTitle);
                    });
                }
            });
        }

        private void OnPickWindowClassName_Click(object sender, RoutedEventArgs e)
        {
            _windowCaptureService!.CaptureWindowInfoAtRightClickAsync().ContinueWith(t =>
            {
                var result = t.Result;
                if (result != null)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        SetValue(sender, result.WindowClassName);
                    });
                }
            });
        }
    }
}