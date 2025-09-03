using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using AutoTool.ViewModel;

namespace AutoTool
{
    /// <summary>
    /// MainWindow.xaml �̑��ݍ�p���W�b�N
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
                    DataContext = app._host.Services.GetRequiredService<MainWindowViewModel>();
                }
                else
                {
                    throw new InvalidOperationException("�A�v���P�[�V�����܂��̓z�X�g�T�[�r�X�����p�ł��܂���B");
                }
            }
            catch (Exception ex)
            {
                // �t�H�[���o�b�N�Ƃ��čŏ�����ViewModel���쐬
                // Note: ���̏ꍇ�͋@�\���������邱�Ƃ����[�U�[�ɒʒm���ׂ�
                MessageBox.Show(
                    $"�������G���[���������܂����B�ꕔ�̋@�\�����������\��������܂��B\n\n�G���[�ڍ�:\n{ex.Message}",
                    "�x��",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
                
                // �ŏ����̑��ViewModel��ݒ�i�T�[�r�X�����p�ł��Ȃ��ꍇ�j
                DataContext = null; // �G���[��ԂƂ��� null ��ݒ�
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
    }
}