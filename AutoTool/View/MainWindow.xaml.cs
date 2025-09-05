using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using AutoTool.ViewModel;
using AutoTool.ViewModel.Panels;
using AutoTool.Services.UI;

namespace AutoTool
{
    /// <summary>
    /// MainWindow.xaml �̑��ݍ�p���W�b�N(DI + Messaging�Ή�)
    /// </summary>
    public partial class MainWindow : Window
    {
        private ILogger<MainWindow>? _logger;
        private IServiceProvider? _serviceProvider;
        private IDataContextLocator? _dataContextLocator;
        private IMainWindowButtonService? _buttonService;

        /// <summary>
        /// MainWindow�̃R���X�g���N�^
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            
            // Loaded event �� runtime �̂ݏ���
            Loaded += MainWindow_Loaded;
            
            // �L�[�{�[�h�C�x���g������
            KeyDown += MainWindow_KeyDown;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                InitializeDI();
                SetupViewModels();
                TestVariableStore();
                
                _logger?.LogInformation("MainWindow DI���������� - All services resolved successfully");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "MainWindow DI�������ɃG���[����");
                
                System.Windows.MessageBox.Show(
                    $"MainWindow�������ɃG���[���������܂����B\n\n�G���[�ڍ�:\n{ex.Message}",
                    "�x��",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
            }
        }

        /// <summary>
        /// DI������
        /// </summary>
        private void InitializeDI()
        {
            if (System.Windows.Application.Current is App app && app.Services != null)
            {
                _serviceProvider = app.Services;
                _logger = _serviceProvider.GetService<ILogger<MainWindow>>();
                _dataContextLocator = _serviceProvider.GetService<IDataContextLocator>();
                _buttonService = _serviceProvider.GetService<IMainWindowButtonService>();

                _logger?.LogDebug("MainWindow DI����������");
            }
            else
            {
                throw new InvalidOperationException("DI�R���e�i�����p�ł��܂���");
            }
        }

        /// <summary>
        /// ViewModel�ݒ�
        /// </summary>
        private void SetupViewModels()
        {
            if (_serviceProvider == null) return;

            try
            {
                // MainWindow�pViewModel�ݒ�
                var mainViewModel = _serviceProvider.GetRequiredService<MainWindowViewModel>();
                DataContext = mainViewModel;
                _logger?.LogDebug("MainWindowViewModel�ݒ芮��");

                // ButtonPanel�pViewModel�ݒ�
                var buttonPanelViewModel = _serviceProvider.GetRequiredService<ButtonPanelViewModel>();
                ButtonPanelViewControl.DataContext = buttonPanelViewModel;
                _logger?.LogDebug("ButtonPanelViewModel�ݒ芮��");

                // ListPanel�pViewModel�ݒ�
                var listPanelViewModel = _serviceProvider.GetRequiredService<ListPanelViewModel>();
                CommandListPanel.DataContext = listPanelViewModel;
                _logger?.LogDebug("ListPanelViewModel�ݒ芮��");

                // EditPanel�pViewModel�ݒ�
                var editPanelViewModel = _serviceProvider.GetRequiredService<EditPanelViewModel>();
                EditPanelViewControl.DataContext = editPanelViewModel;
                _logger?.LogDebug("EditPanelViewModel�ݒ芮��");

                _logger?.LogInformation("�SViewModel�̐ݒ肪�������܂���");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "ViewModel�ݒ蒆�ɃG���[");
                throw;
            }
        }

        /// <summary>
        /// VariableStore�̃e�X�g
        /// </summary>
        private void TestVariableStore()
        {
            try
            {
                var variableStore = _serviceProvider?.GetService<AutoTool.Services.IVariableStore>();
                if (variableStore != null)
                {
                    variableStore.Set("TestVariable", "Hello World");
                    var value = variableStore.Get("TestVariable");
                    _logger?.LogDebug("VariableStore�e�X�g����: {Value}", value);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "VariableStore�e�X�g���ɃG���[");
            }
        }

        /// <summary>
        /// Window closing event handler
        /// </summary>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                _logger?.LogInformation("MainWindow�I�������J�n");
                
                // ���s���̏ꍇ�͌x��
                if (DataContext is MainWindowViewModel viewModel && (_buttonService?.IsRunning ?? false))
                {
                    var result = System.Windows.MessageBox.Show(
                        "�}�N�������s���ł��B�I�����܂����H",
                        "�m�F",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);
                    
                    if (result == MessageBoxResult.No)
                    {
                        e.Cancel = true;
                        return;
                    }
                }
                
                _logger?.LogInformation("MainWindow����I��");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "MainWindow�I���������ɃG���[");
            }
        }

        /// <summary>
        /// Debug State Button Click Handler
        /// </summary>
        private void DebugStateButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _logger?.LogInformation("=== DEBUG: ��Ԋm�F�J�n ===");
                
                if (DataContext is MainWindowViewModel mainViewModel)
                {
                    _logger?.LogInformation("MainViewModel��:");
                    _logger?.LogInformation("  CommandCount: {CommandCount}", mainViewModel.CommandCount);
                    _logger?.LogInformation("  StatusMessage: {StatusMessage}", mainViewModel.StatusMessage);
                }

                // ButtonPanel�I��Ԋm�F
                if (ButtonPanelViewControl.DataContext is ButtonPanelViewModel buttonViewModel)
                {
                    _logger?.LogInformation("ButtonPanelViewModel��:");
                    _logger?.LogInformation("  IsRunning: {IsRunning}", buttonViewModel.IsRunning);
                    _logger?.LogInformation("  SelectedItemType: {SelectedItemType}", buttonViewModel.SelectedItemType?.DisplayName ?? "null");
                    _logger?.LogInformation("  ItemTypes.Count: {Count}", buttonViewModel.ItemTypes.Count);
                    _logger?.LogInformation("  StatusText: {StatusText}", buttonViewModel.StatusText);
                    
                    var stats = buttonViewModel.GetCommandTypeStats();
                    _logger?.LogInformation("  ���v: ���^�C�v{TotalTypes}, �ŋ�{RecentCount}, ���C�ɓ���{FavoriteCount}", 
                        stats.TotalTypes, stats.RecentCount, stats.FavoriteCount);
                }

                // EditPanel�I��Ԋm�F
                if (EditPanelViewControl.DataContext is EditPanelViewModel editViewModel)
                {
                    _logger?.LogInformation("EditPanelViewModel��:");
                    _logger?.LogInformation("  SelectedItem: {SelectedItem}", editViewModel.SelectedItem?.ItemType ?? "null");
                    _logger?.LogInformation("  IsDynamicItem: {IsDynamicItem}", editViewModel.IsDynamicItem);
                    _logger?.LogInformation("  IsLegacyItem: {IsLegacyItem}", editViewModel.IsLegacyItem);
                    _logger?.LogInformation("  SettingGroups.Count: {Count}", editViewModel.SettingGroups.Count);
                    
                    // �ڍאf�f���s
                    editViewModel.DiagnosticProperties();
                }

                // ListPanel�I��Ԋm�F
                if (CommandListPanel.DataContext is ListPanelViewModel listViewModel)
                {
                    _logger?.LogInformation("ListPanelViewModel��:");
                    _logger?.LogInformation("  Items.Count: {Count}", listViewModel.Items.Count);
                    _logger?.LogInformation("  SelectedItem: {SelectedItem}", listViewModel.SelectedItem?.ItemType ?? "null");
                    _logger?.LogInformation("  IsRunning: {IsRunning}", listViewModel.IsRunning);
                }

                _logger?.LogInformation("=== DEBUG: ��Ԋm�F�I�� ===");
                
                System.Windows.MessageBox.Show("��Ԋm�F�����B�ڍׂ̓��O���m�F���Ă��������B", "�f�o�b�O���", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "��Ԋm�F���ɃG���[");
                System.Windows.MessageBox.Show($"��Ԋm�F���ɃG���[: {ex.Message}", "�G���[", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// �L�[�{�[�h�V���[�g�J�b�g���������ă��b�Z�[�W�𑗐M
        /// </summary>
        private void MainWindow_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            try
            {
                var key = e.Key;
                var modifiers = System.Windows.Input.Keyboard.Modifiers;

                string shortcutKey = "";

                // Ctrl+S: �ۑ�
                if (modifiers == System.Windows.Input.ModifierKeys.Control && key == System.Windows.Input.Key.S)
                {
                    shortcutKey = "Ctrl+S";
                }
                // Ctrl+Z: ���ɖ߂�
                else if (modifiers == System.Windows.Input.ModifierKeys.Control && key == System.Windows.Input.Key.Z)
                {
                    shortcutKey = "Ctrl+Z";
                }
                // Ctrl+Y: ��蒼��
                else if (modifiers == System.Windows.Input.ModifierKeys.Control && key == System.Windows.Input.Key.Y)
                {
                    shortcutKey = "Ctrl+Y";
                }

                if (!string.IsNullOrEmpty(shortcutKey))
                {
                    // �ꎞ�I�Ƀ��O�݂̂ŏ���
                    _logger?.LogDebug("�L�[�{�[�h�V���[�g�J�b�g����: {ShortcutKey}", shortcutKey);
                    e.Handled = true; // �C�x���g�������ς݂Ƀ}�[�N
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "�L�[�{�[�h�V���[�g�J�b�g�����G���[");
            }
        }
    }
}