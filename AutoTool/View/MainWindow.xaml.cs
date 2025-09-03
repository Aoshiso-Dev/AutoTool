using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using AutoTool.ViewModel;
using AutoTool.ViewModel.Panels;

namespace AutoTool
{
    /// <summary>
    /// MainWindow.xaml �̑��ݍ�p���W�b�N�iDI + Messaging�Ή��Łj
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly ILogger<MainWindow>? _logger;

        /// <summary>
        /// MainWindow�̃R���X�g���N�^
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();

            try
            {
                // App.xaml����T�[�r�X���擾����ViewModel��ݒ�
                if (Application.Current is App app && app._host != null)
                {
                    // DI����MainWindowViewModel���擾
                    var mainViewModel = app._host.Services.GetRequiredService<MainWindowViewModel>();
                    DataContext = mainViewModel;

                    // DI����ListPanelViewModel���擾����ListPanelView�ɐݒ�
                    var listPanelViewModel = app._host.Services.GetRequiredService<ListPanelViewModel>();
                    CommandListPanel.DataContext = listPanelViewModel;

                    // ���K�[���擾
                    _logger = app._host.Services.GetService<ILogger<MainWindow>>();
                    
                    _logger?.LogInformation("MainWindow DI + Messaging����������");
                }
                else
                {
                    throw new InvalidOperationException("�A�v���P�[�V�����܂��̓z�X�g�T�[�r�X�����p�ł��܂���B");
                }
            }
            catch (Exception ex)
            {
                // �t�H�[���o�b�N�Ƃ��čŏ�����ViewModel���쐬
                MessageBox.Show(
                    $"�������G���[���������܂����B�ꕔ�̋@�\�����������\��������܂��B\n\n�G���[�ڍ�:\n{ex.Message}",
                    "�x��",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
                
                // �G���[��ԂƂ��� null ��ݒ�
                DataContext = null;
            }
        }

        /// <summary>
        /// �E�B���h�E����鎞�̏���
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

                _logger?.LogDebug("MainWindow�I����������");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"�I�������ŃG���[���������܂���: {ex.Message}", "�G���[", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        /// <summary>
        /// �e�X�g�{�^���̃N���b�N�C�x���g�i�f�o�b�O�p�j
        /// </summary>
        private void TestButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (DataContext is MainWindowViewModel viewModel)
                {
                    // ����ViewModel�̃��\�b�h���Ăяo��
                    viewModel.AddTestCommand();
                }
                else
                {
                    MessageBox.Show("ViewModel��������܂���", "�G���[", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"�e�X�g�{�^���N���b�N�ŃG���[: {ex.Message}", "�G���[", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// �ł��V���v���ȃe�X�g
        /// </summary>
        private void SimpleTest_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                MessageBox.Show("�V���v���e�X�g�{�^�������삵�Ă��܂��I", "�e�X�g", MessageBoxButton.OK, MessageBoxImage.Information);
                
                if (DataContext is MainWindowViewModel viewModel)
                {
                    viewModel.LogEntries.Add($"[{DateTime.Now:HH:mm:ss}] �V���v���e�X�g�{�^���N���b�N");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"�V���v���e�X�g�ŃG���[: {ex.Message}", "�G���[", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// �f�o�b�O�p�F��Ԋm�F�{�^���̃N���b�N�C�x���g
        /// </summary>
        private void DebugStateButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Application.Current is App app && app._host != null)
                {
                    var listPanelViewModel = app._host.Services.GetService<ListPanelViewModel>();
                    if (listPanelViewModel != null)
                    {
                        // listPanelViewModel.DebugItemStates(); // �ꎞ�I�ɃR�����g�A�E�g
                        // listPanelViewModel.TestExecutionStateDisplay(); // �ꎞ�I�ɃR�����g�A�E�g
                        MessageBox.Show("��Ԋm�F�@�\�͌��ݖ����ł��B", "�f�o�b�O", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("ListPanelViewModel��������܂���", "�G���[", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"��Ԋm�F���ɃG���[: {ex.Message}", "�G���[", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}