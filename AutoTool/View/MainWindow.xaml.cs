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

        /// <summary>
        /// MainWindow�̃R���X�g���N�^
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            
            // Loaded�C�x���g�Ń����^�C���̂ݏ�����
            Loaded += MainWindow_Loaded;
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
                _logger?.LogError(ex, "MainWindow DI���������ɃG���[������");
                
                MessageBox.Show(
                    $"MainWindow���������ɃG���[���������܂����B\n\n�G���[�ڍ�:\n{ex.Message}",
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
            if (Application.Current is App app && app._host != null)
            {
                _serviceProvider = app._host.Services;
                _logger = _serviceProvider.GetService<ILogger<MainWindow>>();
                _dataContextLocator = _serviceProvider.GetService<IDataContextLocator>();

                _logger?.LogDebug("MainWindow DI����������");
            }
            else
            {
                throw new InvalidOperationException("DI�R���e�i�����p�ł��܂���");
            }
        }

        /// <summary>
        /// ViewModel��ݒ�
        /// </summary>
        private void SetupViewModels()
        {
            if (_serviceProvider == null) return;

            try
            {
                // MainWindow��ViewModel�ݒ�
                var mainViewModel = _serviceProvider.GetRequiredService<MainWindowViewModel>();
                DataContext = mainViewModel;
                _logger?.LogDebug("MainWindowViewModel�ݒ芮��");

                // ListPanel��ViewModel�ݒ�
                var listPanelViewModel = _serviceProvider.GetRequiredService<ListPanelViewModel>();
                CommandListPanel.DataContext = listPanelViewModel;
                _logger?.LogDebug("ListPanelViewModel�ݒ芮��");

                // EditPanel��ViewModel�ݒ�
                var editPanelViewModel = _serviceProvider.GetRequiredService<EditPanelViewModel>();
                EditPanelViewControl.DataContext = editPanelViewModel;
                _logger?.LogDebug("EditPanelViewModel�ݒ芮��");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "ViewModel�ݒ蒆�ɃG���[������");
                throw;
            }
        }

        /// <summary>
        /// VariableStore�̓���m�F
        /// </summary>
        private void TestVariableStore()
        {
            if (_serviceProvider == null) return;

            try
            {
                var variableStore = _serviceProvider.GetService<AutoTool.Services.IVariableStore>();
                if (variableStore == null)
                {
                    _logger?.LogWarning("AutoTool.Services.IVariableStore service is not registered in DI container");
                }
                else
                {
                    _logger?.LogDebug("AutoTool.Services.IVariableStore service successfully resolved from DI container");
                    
                    // ����e�X�g
                    variableStore.Set("TestVariable", "Hello AutoTool DI!");
                    var testValue = variableStore.Get("TestVariable");
                    _logger?.LogInformation("VariableStore����e�X�g: TestVariable = {Value} (Count: {Count})", 
                        testValue, variableStore.Count);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "VariableStore����m�F���ɃG���[������");
            }
        }

        /// <summary>
        /// �E�B���h�E�����ۂ̏���
        /// </summary>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                if (DataContext is MainWindowViewModel viewModel)
                {
                    viewModel.SaveWindowSettings();
                    viewModel.Cleanup();
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "�E�B���h�E�N���[�Y�������ɃG���[������");
                // ignore - �A�v���P�[�V�����I�����Ȃ̂ŃG���[�𖳎�
            }
        }

        private void DebugStateButton_Click(object sender, RoutedEventArgs e)
        {
            // �����̃f�o�b�O�n���h��������z��B�K�v�ł���Ύ����B
        }
    }
}