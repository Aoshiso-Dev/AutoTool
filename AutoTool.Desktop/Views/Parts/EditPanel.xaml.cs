using AutoTool.Core.Abstractions;
using AutoTool.Desktop.ViewModels;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Xceed.Wpf.Toolkit.PropertyGrid; // xctk PropertyItem 型を扱うため
using CommunityToolkit.Mvvm.Messaging;

namespace AutoTool.Desktop.Views.Parts
{
    /// <summary>
    /// EditPanel.xaml の相互作用ロジック
    /// </summary>
    public partial class EditPanel : UserControl
    {
        public EditPanel()
        {
            InitializeComponent();
        }

        // ヘルパ: Button の DataContext から SettingsAdapter とプロパティ名を取得
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
            var sample = new System.Windows.Point(100,200);
            SetValue(sender, sample);
        }

        private void OnPickWindowTitle_Click(object sender, RoutedEventArgs e)
        {
            var sample = "ExampleWindowTitle";
            SetValue(sender, sample);
        }

        private void OnPickWindowClassName_Click(object sender, RoutedEventArgs e)
        {
            var sample = "ExampleWindowClass";
            SetValue(sender, sample);
        }
    }
}