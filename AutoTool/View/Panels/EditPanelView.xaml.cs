using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using AutoTool.ViewModel.Panels;
using AutoTool.Command.Definition;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AutoTool.View.Panels
{
    /// <summary>
    /// EditPanelView.xaml の相互作用ロジック（サービス分離版）
    /// </summary>
    public partial class EditPanelView : System.Windows.Controls.UserControl
    {
        private ILogger<EditPanelView>? _logger;
        private EditPanelViewModel? ViewModel => DataContext as EditPanelViewModel;

        public EditPanelView()
        {
            InitializeComponent();
            
            // DataContextが設定されたときのイベント
            DataContextChanged += EditPanelView_DataContextChanged;
            Loaded += EditPanelView_Loaded;
        }

        private void EditPanelView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            try
            {
                if (_logger == null)
                {
                    // ロガー初期取得
                    if (System.Windows.Application.Current is App app && app.Services != null)
                    {
                        _logger = app.Services.GetService<ILogger<EditPanelView>>();
                    }
                }

                _logger?.LogDebug("EditPanelView DataContextChanged: {OldValue} -> {NewValue}",
                    e.OldValue?.GetType().FullName ?? "null",
                    e.NewValue?.GetType().FullName ?? "null");

                if (e.NewValue is EditPanelViewModel editVM)
                {
                    _logger?.LogInformation("EditPanelViewModelがDataContextに設定されました: {TypeName}", editVM.GetType().FullName);
                    
                    // プロパティ診断テストを実行
                    editVM.DiagnosticProperties();
                }
                else if (e.NewValue != null)
                {
                    _logger?.LogWarning("期待したEditPanelViewModel以外の型がDataContextに設定されました: {TypeName}", e.NewValue.GetType().FullName);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "DataContext変更処理中にエラー");
            }
        }

        private void EditPanelView_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_logger == null)
                {
                    // ロガー再取得
                    if (System.Windows.Application.Current is App app && app.Services != null)
                    {
                        _logger = app.Services.GetService<ILogger<EditPanelView>>();
                    }
                }

                _logger?.LogDebug("EditPanelView Loaded: DataContext = {DataContext}",
                    DataContext?.GetType().FullName ?? "null");

                if (ViewModel != null)
                {
                    _logger?.LogInformation("EditPanelView読み込み完了: ViewModelが利用可能");
                    
                    // バインディングテストを実行
                    ViewModel.TestPropertyNotification();
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Loaded処理中にエラー");
            }
        }

        #region 動的コントロール イベントハンドラー

        // TextBox関連
        private void DynamicTextBox_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.TextBox textBox && textBox.Tag is string propertyName && ViewModel != null)
            {
                var raw = ViewModel.GetDynamicProperty(propertyName);
                string value;

                // Special-case: avoid showing "False" for window title/class
                if ((propertyName == "WindowTitle" || propertyName == "WindowClassName") && raw is bool boolVal && boolVal == false)
                {
                    value = string.Empty;
                }
                else
                {
                    value = raw?.ToString() ?? string.Empty;
                }

                textBox.Text = value;
                _logger?.LogTrace("TextBox初期化: {Property} = {Value}", propertyName, value);
            }
        }

        private void DynamicTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (sender is System.Windows.Controls.TextBox textBox && textBox.Tag is string propertyName && ViewModel != null)
            {
                ViewModel.SetDynamicProperty(propertyName, textBox.Text);
                _logger?.LogTrace("TextBox変更: {Property} = {Value}", propertyName, textBox.Text);
            }
        }

        // PasswordBox関連
        private void DynamicPasswordBox_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is PasswordBox passwordBox && passwordBox.Tag is string propertyName && ViewModel != null)
            {
                var value = ViewModel.GetDynamicProperty(propertyName)?.ToString() ?? string.Empty;
                passwordBox.Password = value;
                _logger?.LogTrace("PasswordBox初期化: {Property} = [HIDDEN]", propertyName);
            }
        }

        private void DynamicPasswordBox_Changed(object sender, RoutedEventArgs e)
        {
            if (sender is PasswordBox passwordBox && passwordBox.Tag is string propertyName && ViewModel != null)
            {
                ViewModel.SetDynamicProperty(propertyName, passwordBox.Password);
                _logger?.LogTrace("PasswordBox変更: {Property} = [HIDDEN]", propertyName);
            }
        }

        // NumberBox関連
        private void DynamicNumberBox_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.TextBox numberBox && numberBox.Tag is string propertyName && ViewModel != null)
            {
                var value = ViewModel.GetDynamicProperty(propertyName);
                numberBox.Text = value?.ToString() ?? "0";
                _logger?.LogTrace("NumberBox初期化: {Property} = {Value}", propertyName, value);
            }
        }

        private void DynamicNumberBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (sender is System.Windows.Controls.TextBox numberBox && numberBox.Tag is string propertyName && ViewModel != null)
            {
                if (double.TryParse(numberBox.Text, out var doubleValue))
                {
                    ViewModel.SetDynamicProperty(propertyName, doubleValue);
                    _logger?.LogTrace("NumberBox変更: {Property} = {Value}", propertyName, doubleValue);
                }
                else if (int.TryParse(numberBox.Text, out var intValue))
                {
                    ViewModel.SetDynamicProperty(propertyName, intValue);
                    _logger?.LogTrace("NumberBox変更: {Property} = {Value}", propertyName, intValue);
                }
            }
        }

        // CheckBox関連
        private void DynamicCheckBox_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.CheckBox checkBox && checkBox.Tag is string propertyName && ViewModel != null)
            {
                var value = ViewModel.GetDynamicProperty(propertyName);
                checkBox.IsChecked = value is bool boolValue ? boolValue : false;
                _logger?.LogTrace("CheckBox初期化: {Property} = {Value}", propertyName, value);
            }
        }

        private void DynamicCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.CheckBox checkBox && checkBox.Tag is string propertyName && ViewModel != null)
            {
                ViewModel.SetDynamicProperty(propertyName, checkBox.IsChecked == true);
                _logger?.LogTrace("CheckBox変更: {Property} = {Value}", propertyName, checkBox.IsChecked);
            }
        }

        // ComboBox関連
        private void DynamicComboBox_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.ComboBox comboBox && comboBox.Tag is string propertyName && ViewModel != null)
            {
                var value = ViewModel.GetDynamicProperty(propertyName);
                comboBox.SelectedItem = value;
                _logger?.LogTrace("ComboBox初期化: {Property} = {Value}", propertyName, value);
            }
        }

        private void DynamicComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (sender is System.Windows.Controls.ComboBox comboBox && comboBox.Tag is string propertyName && ViewModel != null)
            {
                ViewModel.SetDynamicProperty(propertyName, comboBox.SelectedItem);
                _logger?.LogTrace("ComboBox変更: {Property} = {Value}", propertyName, comboBox.SelectedItem);
            }
        }

        // Slider関連
        private void DynamicSlider_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is Slider slider && slider.Tag is string propertyName && ViewModel != null)
            {
                var value = ViewModel.GetDynamicProperty(propertyName);
                if (value is double doubleValue)
                {
                    slider.Value = doubleValue;
                }
                else if (value is float floatValue)
                {
                    slider.Value = floatValue;
                }
                else if (value != null && double.TryParse(value.ToString(), out var parsedValue))
                {
                    slider.Value = parsedValue;
                }
                _logger?.LogTrace("Slider初期化: {Property} = {Value}", propertyName, slider.Value);
            }
        }

        private void DynamicSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (sender is Slider slider && slider.Tag is string propertyName && ViewModel != null)
            {
                ViewModel.SetDynamicProperty(propertyName, slider.Value);
                _logger?.LogTrace("Slider変更: {Property} = {Value}", propertyName, slider.Value);
            }
        }

        // ファイルパス関連
        private void DynamicFilePath_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.TextBox filePathBox && filePathBox.Tag is string propertyName && ViewModel != null)
            {
                var value = ViewModel.GetDynamicProperty(propertyName)?.ToString() ?? string.Empty;
                filePathBox.Text = value;
                _logger?.LogTrace("FilePath初期化: {Property} = {Value}", propertyName, value);
            }
        }

        private void DynamicBrowseFile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is System.Windows.Controls.Button button && button.Tag is SettingDefinition setting)
                {
                    var context = new ActionExecutionContext
                    {
                        SettingDefinition = setting,
                        ActionType = "Browse"
                    };
                    _ = ViewModel?.ExecuteActionCommand.ExecuteAsync(context);
                    _logger?.LogInformation("動的ファイル選択: {PropertyName}", setting.PropertyName);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "動的ファイル選択エラー");
            }
        }

        private void DynamicClearFile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is System.Windows.Controls.Button button && button.Tag is SettingDefinition setting)
                {
                    var context = new ActionExecutionContext
                    {
                        SettingDefinition = setting,
                        ActionType = "Clear"
                    };
                    _ = ViewModel?.ExecuteActionCommand.ExecuteAsync(context);
                    _logger?.LogInformation("動的ファイルクリア: {PropertyName}", setting.PropertyName);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "動的ファイルクリアエラー");
            }
        }

        // フォルダパス関連
        private void DynamicFolderPath_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.TextBox folderBox && folderBox.Tag is string propertyName && ViewModel != null)
            {
                var value = ViewModel.GetDynamicProperty(propertyName)?.ToString() ?? string.Empty;
                folderBox.Text = value;
                _logger?.LogTrace("FolderPath初期化: {Property} = {Value}", propertyName, value);
            }
        }

        private void DynamicBrowseFolder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is System.Windows.Controls.Button button && button.Tag is SettingDefinition setting)
                {
                    var context = new ActionExecutionContext
                    {
                        SettingDefinition = setting,
                        ActionType = "BrowseFolder"
                    };
                    _ = ViewModel?.ExecuteActionCommand.ExecuteAsync(context);
                    _logger?.LogInformation("動的フォルダ選択: {PropertyName}", setting.PropertyName);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "動的フォルダ選択エラー");
            }
        }

        private void DynamicClearFolder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is System.Windows.Controls.Button button && button.Tag is SettingDefinition setting)
                {
                    var context = new ActionExecutionContext
                    {
                        SettingDefinition = setting,
                        ActionType = "Clear"
                    };
                    _ = ViewModel?.ExecuteActionCommand.ExecuteAsync(context);
                    _logger?.LogInformation("動的フォルダクリア: {PropertyName}", setting.PropertyName);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "動的フォルダクリアエラー");
            }
        }

        // ONNXパス関連
        private void DynamicOnnxPath_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.TextBox onnxBox && onnxBox.Tag is string propertyName && ViewModel != null)
            {
                var value = ViewModel.GetDynamicProperty(propertyName)?.ToString() ?? string.Empty;
                onnxBox.Text = value;
                _logger?.LogTrace("OnnxPath初期化: {Property} = {Value}", propertyName, value);
            }
        }

        private void DynamicBrowseOnnx_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is System.Windows.Controls.Button button && button.Tag is SettingDefinition setting)
                {
                    var context = new ActionExecutionContext
                    {
                        SettingDefinition = setting,
                        ActionType = "Browse"
                    };
                    _ = ViewModel?.ExecuteActionCommand.ExecuteAsync(context);
                    _logger?.LogInformation("動的ONNX選択: {PropertyName}", setting.PropertyName);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "動的ONNX選択エラー");
            }
        }

        private void DynamicClearOnnx_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is System.Windows.Controls.Button button && button.Tag is SettingDefinition setting)
                {
                    var context = new ActionExecutionContext
                    {
                        SettingDefinition = setting,
                        ActionType = "Clear"
                    };
                    _ = ViewModel?.ExecuteActionCommand.ExecuteAsync(context);
                    _logger?.LogInformation("動的ONNXクリア: {PropertyName}", setting.PropertyName);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "動的ONNXクリアエラー");
            }
        }

        // 色関連
        private void DynamicColorDisplay_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is Border colorBorder && colorBorder.Tag is string propertyName && ViewModel != null)
            {
                var value = ViewModel.GetDynamicProperty(propertyName);
                if (value is System.Drawing.Color color)
                {
                    colorBorder.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(color.A, color.R, color.G, color.B));
                }
                _logger?.LogTrace("ColorDisplay初期化: {Property} = {Value}", propertyName, value);
            }
        }

        private void DynamicColorText_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.TextBox colorTextBox && colorTextBox.Tag is string propertyName && ViewModel != null)
            {
                var value = ViewModel.GetDynamicProperty(propertyName);
                if (value is System.Drawing.Color color)
                {
                    colorTextBox.Text = $"#{color.R:X2}{color.G:X2}{color.B:X2}";
                }
                _logger?.LogTrace("ColorText初期化: {Property} = {Value}", propertyName, value);
            }
        }

        private void DynamicColorText_Changed(object sender, TextChangedEventArgs e)
        {
            if (sender is System.Windows.Controls.TextBox colorTextBox && colorTextBox.Tag is string propertyName && ViewModel != null)
            {
                try
                {
                    var text = colorTextBox.Text;
                    if (text.StartsWith("#") && text.Length == 7)
                    {
                        var color = System.Drawing.ColorTranslator.FromHtml(text);
                        ViewModel.SetDynamicProperty(propertyName, color);
                        _logger?.LogTrace("ColorText変更: {Property} = {Value}", propertyName, text);
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogTrace("ColorText変更エラー: {Message}", ex.Message);
                }
            }
        }

        private void DynamicPickColor_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is System.Windows.Controls.Button button && button.Tag is SettingDefinition setting)
                {
                    var context = new ActionExecutionContext
                    {
                        SettingDefinition = setting,
                        ActionType = "PickColor"
                    };
                    _ = ViewModel?.ExecuteActionCommand.ExecuteAsync(context);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "動的色選択エラー");
            }
        }

        private void DynamicCaptureColor_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is System.Windows.Controls.Button button && button.Tag is SettingDefinition setting)
                {
                    var context = new ActionExecutionContext
                    {
                        SettingDefinition = setting,
                        ActionType = "CaptureColor"
                    };
                    _ = ViewModel?.ExecuteActionCommand.ExecuteAsync(context);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "動的色キャプチャエラー");
            }
        }

        // 日付関連
        private void DynamicDatePicker_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is DatePicker datePicker && datePicker.Tag is string propertyName && ViewModel != null)
            {
                var value = ViewModel.GetDynamicProperty(propertyName);
                if (value is DateTime dateTime)
                {
                    datePicker.SelectedDate = dateTime;
                }
                _logger?.LogTrace("DatePicker初期化: {Property} = {Value}", propertyName, value);
            }
        }

        private void DynamicDatePicker_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (sender is DatePicker datePicker && datePicker.Tag is string propertyName && ViewModel != null)
            {
                if (datePicker.SelectedDate.HasValue)
                {
                    ViewModel.SetDynamicProperty(propertyName, datePicker.SelectedDate.Value);
                    _logger?.LogTrace("DatePicker変更: {Property} = {Value}", propertyName, datePicker.SelectedDate.Value);
                }
            }
        }

        // 時間関連
        private void DynamicTimePicker_Loaded(object sender, RoutedEventArgs e)
        {
            // 簡易実装
        }

        private void DynamicTimePicker_Changed(object sender, SelectionChangedEventArgs e)
        {
            // 簡易実装
        }

        // キー関連
        private void DynamicKeyPicker_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.TextBox keyTextBox && keyTextBox.Tag is string propertyName && ViewModel != null)
            {
                var value = ViewModel.GetDynamicProperty(propertyName);
                keyTextBox.Text = value?.ToString() ?? "未設定";
                _logger?.LogTrace("KeyPicker初期化: {Property} = {Value}", propertyName, value);
            }
        }

        private void DynamicCaptureKey_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is System.Windows.Controls.Button button && button.Tag is SettingDefinition setting)
                {
                    var context = new ActionExecutionContext
                    {
                        SettingDefinition = setting,
                        ActionType = "CaptureKey"
                    };
                    _ = ViewModel?.ExecuteActionCommand.ExecuteAsync(context);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "動的キーキャプチャエラー");
            }
        }

        private void DynamicClearKey_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is System.Windows.Controls.Button button && button.Tag is SettingDefinition setting)
                {
                    var context = new ActionExecutionContext
                    {
                        SettingDefinition = setting,
                        ActionType = "Clear"
                    };
                    _ = ViewModel?.ExecuteActionCommand.ExecuteAsync(context);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "動的キークリアエラー");
            }
        }

        // 座標関連
        private void DynamicCoordinate_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.TextBox coordinateBox && coordinateBox.Tag is string coordinate && ViewModel != null)
            {
                var value = ViewModel.GetDynamicProperty(coordinate);
                coordinateBox.Text = value?.ToString() ?? "0";
                _logger?.LogTrace("Coordinate初期化: {Coordinate} = {Value}", coordinate, value);
            }
        }

        private void DynamicCoordinate_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (sender is System.Windows.Controls.TextBox coordinateBox && coordinateBox.Tag is string coordinate && ViewModel != null)
            {
                if (int.TryParse(coordinateBox.Text, out var intValue))
                {
                    ViewModel.SetDynamicProperty(coordinate, intValue);
                    _logger?.LogTrace("Coordinate変更: {Coordinate} = {Value}", coordinate, intValue);
                }
            }
        }

        private void DynamicGetMousePosition_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is System.Windows.Controls.Button button && button.Tag is SettingDefinition setting)
                {
                    var context = new ActionExecutionContext
                    {
                        SettingDefinition = setting,
                        ActionType = "GetMousePosition"
                    };
                    _ = ViewModel?.ExecuteActionCommand.ExecuteAsync(context);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "動的マウス位置取得エラー");
            }
        }

        private void DynamicGetCurrentPosition_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is System.Windows.Controls.Button button && button.Tag is SettingDefinition setting)
                {
                    var context = new ActionExecutionContext
                    {
                        SettingDefinition = setting,
                        ActionType = "GetCurrentPosition"
                    };
                    _ = ViewModel?.ExecuteActionCommand.ExecuteAsync(context);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "動的現在位置取得エラー");
            }
        }

        private void DynamicClearCoordinate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is System.Windows.Controls.Button button && button.Tag is SettingDefinition setting)
                {
                    var context = new ActionExecutionContext
                    {
                        SettingDefinition = setting,
                        ActionType = "Clear"
                    };
                    _ = ViewModel?.ExecuteActionCommand.ExecuteAsync(context);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "動的座標クリアエラー");
            }
        }

        // ウィンドウ関連
        private void DynamicWindowTitle_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.TextBox windowTitleBox && windowTitleBox.Tag is string propertyName && ViewModel != null)
            {
                var raw = ViewModel.GetDynamicProperty(propertyName);
                string value;

                // Avoid showing boolean false as "False" for window fields
                if (raw is bool boolVal && boolVal == false)
                {
                    value = string.Empty;
                }
                else
                {
                    value = raw?.ToString() ?? string.Empty;
                }

                windowTitleBox.Text = value;
                _logger?.LogTrace("WindowTitle初期化: {Property} = {Value}", propertyName, value);
            }
        }

        private void DynamicGetWindowInfo_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is System.Windows.Controls.Button button && button.Tag is SettingDefinition setting)
                {
                    var context = new ActionExecutionContext
                    {
                        SettingDefinition = setting,
                        ActionType = "GetWindowInfo"
                    };
                    _ = ViewModel?.ExecuteActionCommand.ExecuteAsync(context);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "動的ウィンドウ情報取得エラー");
            }
        }

        private void DynamicListWindows_Click(object sender, RoutedEventArgs e)
        {
            // 簡易実装
        }

        private void DynamicClearWindow_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is System.Windows.Controls.Button button && button.Tag is SettingDefinition setting)
                {
                    var context = new ActionExecutionContext
                    {
                        SettingDefinition = setting,
                        ActionType = "Clear"
                    };
                    _ = ViewModel?.ExecuteActionCommand.ExecuteAsync(context);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "動的ウィンドウクリアエラー");
            }
        }

        // 現在値表示
        private void DynamicCurrentValue_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is TextBlock textBlock && textBlock.Tag is string propertyName && ViewModel != null)
            {
                var value = ViewModel.GetDynamicProperty(propertyName);
                if (value != null)
                {
                    textBlock.Text = value.ToString();
                }
                _logger?.LogTrace("CurrentValue初期化: {Property} = {Value}", propertyName, value);
            }
        }

        // 動的設定UI操作ボタン
        private void SaveDynamicSettings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (ViewModel != null)
                {
                    var success = ViewModel.SaveDynamicSettings();
                    if (success)
                    {
                        System.Windows.MessageBox.Show("設定を保存しました。", "設定保存", 
                            MessageBoxButton.OK, MessageBoxImage.Information);
                        _logger?.LogInformation("動的設定保存成功");
                    }
                    else
                    {
                        System.Windows.MessageBox.Show("設定の保存に失敗しました。", "設定保存エラー", 
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        _logger?.LogWarning("動的設定保存失敗");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "動的設定保存エラー");
                System.Windows.MessageBox.Show($"設定保存中にエラーが発生しました: {ex.Message}", 
                    "設定保存エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ResetDynamicSettings_Click(object sender, RoutedEventArgs e)
        {
            // 簡易実装
        }

        private void DiagnoseDynamicSettings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (ViewModel != null)
                {
                    ViewModel.DiagnosticProperties();
                    
                    var diagnosticInfo = $"動的設定診断結果:\n\n" +
                        $"選択アイテム: {ViewModel.SelectedItem?.ItemType ?? "なし"}\n" +
                        $"動的アイテム: {ViewModel.IsDynamicItem}\n" +
                        $"従来アイテム: {ViewModel.IsLegacyItem}\n" +
                        $"設定グループ数: {ViewModel.SettingGroups.Count}\n" +
                        $"設定定義数: {ViewModel.SettingDefinitions.Count}\n" +
                        $"動的値数: {ViewModel.DynamicValues.Count}\n\n" +
                        "詳細はログを確認してください。";
                    
                    System.Windows.MessageBox.Show(diagnosticInfo, "動的設定診断", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    
                    _logger?.LogInformation("動的設定診断完了");
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "動的設定診断エラー");
                System.Windows.MessageBox.Show($"診断中にエラーが発生しました: {ex.Message}", 
                    "診断エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CopyDynamicValues_Click(object sender, RoutedEventArgs e)
        {
            // 簡易実装
        }

        private void TestDynamicSettings_Click(object sender, RoutedEventArgs e)
        {
            // 簡易実装
        }

        #endregion
    }
}