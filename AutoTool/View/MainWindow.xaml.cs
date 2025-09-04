using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using AutoTool.ViewModel;
using AutoTool.ViewModel.Panels;

namespace AutoTool
{
    /// <summary>
    /// MainWindow.xaml �̑��ݍ�p���W�b�N(DI + Messaging�Ή�)
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

                    // DI����EditPanelViewModel���擾����EditPanelView�ɐݒ�
                    var editPanelViewModel = app._host.Services.GetRequiredService<EditPanelViewModel>();
                    EditPanelViewControl.DataContext = mainViewModel; // �p��: MainWindowVM�o�R�Ńo�C���h
                    // �܂��͒���VM�����蓖�Ă�ꍇ�͈ȉ�
                    // EditPanelViewControl.DataContext = editPanelViewModel;

                    // ���K�[�擾
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
                    $"���������ɃG���[���������܂����B�K�v�ȃT�[�r�X�����p�ł��Ȃ��\��������܂��B\n\n�G���[�ڍ�:\n{ex.Message}",
                    "�x��",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
                
                // �G���[���Ƃ��� null ��ݒ�
                DataContext = null;
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
            catch (Exception)
            {
                // ignore
            }
        }

        private void DebugStateButton_Click(object sender, RoutedEventArgs e)
        {
            // �����̃f�o�b�O�n���h��������z��B�K�v�ł���Ύ����B
        }
    }
}