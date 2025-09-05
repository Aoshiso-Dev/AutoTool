using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using AutoTool.ViewModel.Panels;
using AutoTool.Model.CommandDefinition;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AutoTool.View.Panels
{
    /// <summary>
    /// EditPanelView.xaml �̑��ݍ�p���W�b�N�i�T�[�r�X�����Łj
    /// </summary>
    public partial class EditPanelView : System.Windows.Controls.UserControl
    {
        private ILogger<EditPanelView>? _logger;
        private EditPanelViewModel? ViewModel => DataContext as EditPanelViewModel;

        public EditPanelView()
        {
            InitializeComponent();
            
            // DataContext���ݒ肳�ꂽ�Ƃ��̃C�x���g
            DataContextChanged += EditPanelView_DataContextChanged;
            Loaded += EditPanelView_Loaded;
        }

        private void EditPanelView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            try
            {
                if (_logger == null)
                {
                    // ���K�[�����擾
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
                    _logger?.LogInformation("EditPanelViewModel��DataContext�ɐݒ肳��܂���: {TypeName}", editVM.GetType().FullName);
                    
                    // �v���p�e�B�f�f�e�X�g�����s
                    editVM.DiagnosticProperties();
                }
                else if (e.NewValue != null)
                {
                    _logger?.LogWarning("���҂���EditPanelViewModel�ȊO�̌^��DataContext�ɐݒ肳��܂���: {TypeName}", e.NewValue.GetType().FullName);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "DataContext�ύX�������ɃG���[");
            }
        }

        private void EditPanelView_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_logger == null)
                {
                    // ���K�[�Ď擾
                    if (System.Windows.Application.Current is App app && app.Services != null)
                    {
                        _logger = app.Services.GetService<ILogger<EditPanelView>>();
                    }
                }

                _logger?.LogDebug("EditPanelView Loaded: DataContext = {DataContext}",
                    DataContext?.GetType().FullName ?? "null");

                if (ViewModel != null)
                {
                    _logger?.LogInformation("EditPanelView�ǂݍ��݊���: ViewModel�����p�\");
                    
                    // �o�C���f�B���O�e�X�g�����s
                    ViewModel.TestPropertyNotification();
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Loaded�������ɃG���[");
            }
        }

        #region ���I�R���g���[�� �C�x���g�n���h���[

        // TextBox�֘A
        private void DynamicTextBox_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.TextBox textBox && textBox.Tag is string propertyName && ViewModel != null)
            {
                var value = ViewModel.GetDynamicProperty(propertyName)?.ToString() ?? string.Empty;
                textBox.Text = value;
                _logger?.LogTrace("TextBox������: {Property} = {Value}", propertyName, value);
            }
        }

        private void DynamicTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (sender is System.Windows.Controls.TextBox textBox && textBox.Tag is string propertyName && ViewModel != null)
            {
                ViewModel.SetDynamicProperty(propertyName, textBox.Text);
                _logger?.LogTrace("TextBox�ύX: {Property} = {Value}", propertyName, textBox.Text);
            }
        }

        // PasswordBox�֘A
        private void DynamicPasswordBox_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is PasswordBox passwordBox && passwordBox.Tag is string propertyName && ViewModel != null)
            {
                var value = ViewModel.GetDynamicProperty(propertyName)?.ToString() ?? string.Empty;
                passwordBox.Password = value;
                _logger?.LogTrace("PasswordBox������: {Property} = [HIDDEN]", propertyName);
            }
        }

        private void DynamicPasswordBox_Changed(object sender, RoutedEventArgs e)
        {
            if (sender is PasswordBox passwordBox && passwordBox.Tag is string propertyName && ViewModel != null)
            {
                ViewModel.SetDynamicProperty(propertyName, passwordBox.Password);
                _logger?.LogTrace("PasswordBox�ύX: {Property} = [HIDDEN]", propertyName);
            }
        }

        // NumberBox�֘A
        private void DynamicNumberBox_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.TextBox numberBox && numberBox.Tag is string propertyName && ViewModel != null)
            {
                var value = ViewModel.GetDynamicProperty(propertyName);
                numberBox.Text = value?.ToString() ?? "0";
                _logger?.LogTrace("NumberBox������: {Property} = {Value}", propertyName, value);
            }
        }

        private void DynamicNumberBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (sender is System.Windows.Controls.TextBox numberBox && numberBox.Tag is string propertyName && ViewModel != null)
            {
                if (double.TryParse(numberBox.Text, out var doubleValue))
                {
                    ViewModel.SetDynamicProperty(propertyName, doubleValue);
                    _logger?.LogTrace("NumberBox�ύX: {Property} = {Value}", propertyName, doubleValue);
                }
                else if (int.TryParse(numberBox.Text, out var intValue))
                {
                    ViewModel.SetDynamicProperty(propertyName, intValue);
                    _logger?.LogTrace("NumberBox�ύX: {Property} = {Value}", propertyName, intValue);
                }
            }
        }

        // CheckBox�֘A
        private void DynamicCheckBox_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.CheckBox checkBox && checkBox.Tag is string propertyName && ViewModel != null)
            {
                var value = ViewModel.GetDynamicProperty(propertyName);
                checkBox.IsChecked = value is bool boolValue ? boolValue : false;
                _logger?.LogTrace("CheckBox������: {Property} = {Value}", propertyName, value);
            }
        }

        private void DynamicCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.CheckBox checkBox && checkBox.Tag is string propertyName && ViewModel != null)
            {
                ViewModel.SetDynamicProperty(propertyName, checkBox.IsChecked == true);
                _logger?.LogTrace("CheckBox�ύX: {Property} = {Value}", propertyName, checkBox.IsChecked);
            }
        }

        // ComboBox�֘A
        private void DynamicComboBox_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.ComboBox comboBox && comboBox.Tag is string propertyName && ViewModel != null)
            {
                var value = ViewModel.GetDynamicProperty(propertyName);
                comboBox.SelectedItem = value;
                _logger?.LogTrace("ComboBox������: {Property} = {Value}", propertyName, value);
            }
        }

        private void DynamicComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (sender is System.Windows.Controls.ComboBox comboBox && comboBox.Tag is string propertyName && ViewModel != null)
            {
                ViewModel.SetDynamicProperty(propertyName, comboBox.SelectedItem);
                _logger?.LogTrace("ComboBox�ύX: {Property} = {Value}", propertyName, comboBox.SelectedItem);
            }
        }

        // Slider�֘A
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
                _logger?.LogTrace("Slider������: {Property} = {Value}", propertyName, slider.Value);
            }
        }

        private void DynamicSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (sender is Slider slider && slider.Tag is string propertyName && ViewModel != null)
            {
                ViewModel.SetDynamicProperty(propertyName, slider.Value);
                _logger?.LogTrace("Slider�ύX: {Property} = {Value}", propertyName, slider.Value);
            }
        }

        // �t�@�C���p�X�֘A
        private void DynamicFilePath_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.TextBox filePathBox && filePathBox.Tag is string propertyName && ViewModel != null)
            {
                var value = ViewModel.GetDynamicProperty(propertyName)?.ToString() ?? string.Empty;
                filePathBox.Text = value;
                _logger?.LogTrace("FilePath������: {Property} = {Value}", propertyName, value);
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
                    _logger?.LogInformation("���I�t�@�C���I��: {PropertyName}", setting.PropertyName);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "���I�t�@�C���I���G���[");
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
                    _logger?.LogInformation("���I�t�@�C���N���A: {PropertyName}", setting.PropertyName);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "���I�t�@�C���N���A�G���[");
            }
        }

        // �t�H���_�p�X�֘A
        private void DynamicFolderPath_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.TextBox folderBox && folderBox.Tag is string propertyName && ViewModel != null)
            {
                var value = ViewModel.GetDynamicProperty(propertyName)?.ToString() ?? string.Empty;
                folderBox.Text = value;
                _logger?.LogTrace("FolderPath������: {Property} = {Value}", propertyName, value);
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
                    _logger?.LogInformation("���I�t�H���_�I��: {PropertyName}", setting.PropertyName);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "���I�t�H���_�I���G���[");
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
                    _logger?.LogInformation("���I�t�H���_�N���A: {PropertyName}", setting.PropertyName);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "���I�t�H���_�N���A�G���[");
            }
        }

        // ONNX�p�X�֘A
        private void DynamicOnnxPath_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.TextBox onnxBox && onnxBox.Tag is string propertyName && ViewModel != null)
            {
                var value = ViewModel.GetDynamicProperty(propertyName)?.ToString() ?? string.Empty;
                onnxBox.Text = value;
                _logger?.LogTrace("OnnxPath������: {Property} = {Value}", propertyName, value);
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
                    _logger?.LogInformation("���IONNX�I��: {PropertyName}", setting.PropertyName);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "���IONNX�I���G���[");
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
                    _logger?.LogInformation("���IONNX�N���A: {PropertyName}", setting.PropertyName);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "���IONNX�N���A�G���[");
            }
        }

        // �F�֘A
        private void DynamicColorDisplay_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is Border colorBorder && colorBorder.Tag is string propertyName && ViewModel != null)
            {
                var value = ViewModel.GetDynamicProperty(propertyName);
                if (value is System.Drawing.Color color)
                {
                    colorBorder.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(color.A, color.R, color.G, color.B));
                }
                _logger?.LogTrace("ColorDisplay������: {Property} = {Value}", propertyName, value);
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
                _logger?.LogTrace("ColorText������: {Property} = {Value}", propertyName, value);
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
                        _logger?.LogTrace("ColorText�ύX: {Property} = {Value}", propertyName, text);
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogTrace("ColorText�ύX�G���[: {Message}", ex.Message);
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
                _logger?.LogError(ex, "���I�F�I���G���[");
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
                _logger?.LogError(ex, "���I�F�L���v�`���G���[");
            }
        }

        // ���t�֘A
        private void DynamicDatePicker_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is DatePicker datePicker && datePicker.Tag is string propertyName && ViewModel != null)
            {
                var value = ViewModel.GetDynamicProperty(propertyName);
                if (value is DateTime dateTime)
                {
                    datePicker.SelectedDate = dateTime;
                }
                _logger?.LogTrace("DatePicker������: {Property} = {Value}", propertyName, value);
            }
        }

        private void DynamicDatePicker_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (sender is DatePicker datePicker && datePicker.Tag is string propertyName && ViewModel != null)
            {
                if (datePicker.SelectedDate.HasValue)
                {
                    ViewModel.SetDynamicProperty(propertyName, datePicker.SelectedDate.Value);
                    _logger?.LogTrace("DatePicker�ύX: {Property} = {Value}", propertyName, datePicker.SelectedDate.Value);
                }
            }
        }

        // ���Ԋ֘A
        private void DynamicTimePicker_Loaded(object sender, RoutedEventArgs e)
        {
            // �ȈՎ���
        }

        private void DynamicTimePicker_Changed(object sender, SelectionChangedEventArgs e)
        {
            // �ȈՎ���
        }

        // �L�[�֘A
        private void DynamicKeyPicker_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.TextBox keyTextBox && keyTextBox.Tag is string propertyName && ViewModel != null)
            {
                var value = ViewModel.GetDynamicProperty(propertyName);
                keyTextBox.Text = value?.ToString() ?? "���ݒ�";
                _logger?.LogTrace("KeyPicker������: {Property} = {Value}", propertyName, value);
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
                _logger?.LogError(ex, "���I�L�[�L���v�`���G���[");
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
                _logger?.LogError(ex, "���I�L�[�N���A�G���[");
            }
        }

        // ���W�֘A
        private void DynamicCoordinate_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.TextBox coordinateBox && coordinateBox.Tag is string coordinate && ViewModel != null)
            {
                var value = ViewModel.GetDynamicProperty(coordinate);
                coordinateBox.Text = value?.ToString() ?? "0";
                _logger?.LogTrace("Coordinate������: {Coordinate} = {Value}", coordinate, value);
            }
        }

        private void DynamicCoordinate_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (sender is System.Windows.Controls.TextBox coordinateBox && coordinateBox.Tag is string coordinate && ViewModel != null)
            {
                if (int.TryParse(coordinateBox.Text, out var intValue))
                {
                    ViewModel.SetDynamicProperty(coordinate, intValue);
                    _logger?.LogTrace("Coordinate�ύX: {Coordinate} = {Value}", coordinate, intValue);
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
                _logger?.LogError(ex, "���I�}�E�X�ʒu�擾�G���[");
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
                _logger?.LogError(ex, "���I���݈ʒu�擾�G���[");
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
                _logger?.LogError(ex, "���I���W�N���A�G���[");
            }
        }

        // �E�B���h�E�֘A
        private void DynamicWindowTitle_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.TextBox windowTitleBox && windowTitleBox.Tag is string propertyName && ViewModel != null)
            {
                var value = ViewModel.GetDynamicProperty(propertyName)?.ToString() ?? string.Empty;
                windowTitleBox.Text = value;
                _logger?.LogTrace("WindowTitle������: {Property} = {Value}", propertyName, value);
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
                _logger?.LogError(ex, "���I�E�B���h�E���擾�G���[");
            }
        }

        private void DynamicListWindows_Click(object sender, RoutedEventArgs e)
        {
            // �ȈՎ���
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
                _logger?.LogError(ex, "���I�E�B���h�E�N���A�G���[");
            }
        }

        // ���ݒl�\��
        private void DynamicCurrentValue_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is TextBlock textBlock && textBlock.Tag is string propertyName && ViewModel != null)
            {
                var value = ViewModel.GetDynamicProperty(propertyName);
                if (value != null)
                {
                    textBlock.Text = value.ToString();
                }
                _logger?.LogTrace("CurrentValue������: {Property} = {Value}", propertyName, value);
            }
        }

        // ���I�ݒ�UI����{�^��
        private void SaveDynamicSettings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (ViewModel != null)
                {
                    var success = ViewModel.SaveDynamicSettings();
                    if (success)
                    {
                        System.Windows.MessageBox.Show("�ݒ��ۑ����܂����B", "�ݒ�ۑ�", 
                            MessageBoxButton.OK, MessageBoxImage.Information);
                        _logger?.LogInformation("���I�ݒ�ۑ�����");
                    }
                    else
                    {
                        System.Windows.MessageBox.Show("�ݒ�̕ۑ��Ɏ��s���܂����B", "�ݒ�ۑ��G���[", 
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        _logger?.LogWarning("���I�ݒ�ۑ����s");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "���I�ݒ�ۑ��G���[");
                System.Windows.MessageBox.Show($"�ݒ�ۑ����ɃG���[���������܂���: {ex.Message}", 
                    "�ݒ�ۑ��G���[", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ResetDynamicSettings_Click(object sender, RoutedEventArgs e)
        {
            // �ȈՎ���
        }

        private void DiagnoseDynamicSettings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (ViewModel != null)
                {
                    ViewModel.DiagnosticProperties();
                    
                    var diagnosticInfo = $"���I�ݒ�f�f����:\n\n" +
                        $"�I���A�C�e��: {ViewModel.SelectedItem?.ItemType ?? "�Ȃ�"}\n" +
                        $"���I�A�C�e��: {ViewModel.IsDynamicItem}\n" +
                        $"�]���A�C�e��: {ViewModel.IsLegacyItem}\n" +
                        $"�ݒ�O���[�v��: {ViewModel.SettingGroups.Count}\n" +
                        $"�ݒ��`��: {ViewModel.SettingDefinitions.Count}\n" +
                        $"���I�l��: {ViewModel.DynamicValues.Count}\n\n" +
                        "�ڍׂ̓��O���m�F���Ă��������B";
                    
                    System.Windows.MessageBox.Show(diagnosticInfo, "���I�ݒ�f�f", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    
                    _logger?.LogInformation("���I�ݒ�f�f����");
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "���I�ݒ�f�f�G���[");
                System.Windows.MessageBox.Show($"�f�f���ɃG���[���������܂���: {ex.Message}", 
                    "�f�f�G���[", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CopyDynamicValues_Click(object sender, RoutedEventArgs e)
        {
            // �ȈՎ���
        }

        private void TestDynamicSettings_Click(object sender, RoutedEventArgs e)
        {
            // �ȈՎ���
        }

        #endregion
    }
}