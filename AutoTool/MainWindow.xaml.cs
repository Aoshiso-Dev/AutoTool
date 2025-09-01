using System;
using System.Windows;
using Microsoft.Extensions.Logging;

namespace AutoTool
{
    /// <summary>
    /// MainWindow.xaml �̑��ݍ�p���W�b�N
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly ILogger<MainWindow> _logger;

        public MainWindow()
        {
            InitializeComponent();

            try
            {
                // ���K�[�̎擾
                var app = Application.Current as App;
                if (app?._host != null)
                {
                    _logger = app._host.Services.GetService<ILogger<MainWindow>>();

                    // �r���[���f�����擾����DataContext�ɐݒ�
                    DataContext = app._host.Services.GetService<MainWindowViewModel>();

                    _logger.LogDebug("MainWindow ����������");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MainWindow ���������ɃG���[���������܂���");
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                // �r���[���f���̃N���[���A�b�v
                if (DataContext is MainWindowViewModel viewModel)
                {
                    viewModel.SaveWindowSettings();
                    viewModel.Cleanup();
                }

                _logger.LogDebug("MainWindow �I����������");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MainWindow �I���������ɃG���[���������܂���");
            }
        }
    }
}