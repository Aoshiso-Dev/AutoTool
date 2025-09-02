using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AutoTool
{
    /// <summary>
    /// MainWindow.xaml �̑��ݍ�p���W�b�N
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly ILogger<MainWindow>? _logger;

        public MainWindow()
        {
            InitializeComponent();

            try
            {
                // ���K�[�̎擾
                var app = Application.Current as App;
                if (app?._host != null)
                {
                    _logger = app._host.Services.GetRequiredService<ILogger<MainWindow>>();

                    // �r���[���f�����擾����DataContext�ɐݒ�
                    DataContext = app._host.Services.GetRequiredService<MainWindowViewModel>();

                    _logger?.LogDebug("MainWindow ����������");
                }
                else
                {
                    // DI�R���e�i�����p�ł��Ȃ��ꍇ�̃t�H�[���o�b�N
                    System.Diagnostics.Debug.WriteLine("DI�R���e�i�����p�ł��܂���B���K�V�[���[�h�ŏ��������܂��B");
                    
                    // ���K�V�[���[�h�� MainWindowViewModel ���쐬
                    #pragma warning disable CS0618 // Obsolete �x���𖳎�
                    DataContext = new MainWindowViewModel();
                    #pragma warning restore CS0618
                    
                    System.Diagnostics.Debug.WriteLine("���K�V�[���[�h�� MainWindow ������������܂����B");
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "MainWindow ���������ɃG���[���������܂���");
                
                // �G���[�����������ꍇ�ł��E�B���h�E��\�����邽�߁A�ŏ����̏����������s
                try
                {
                    System.Diagnostics.Debug.WriteLine($"MainWindow �������G���[: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine("�ً}�t�H�[���o�b�N���[�h�ŏ����������s���܂��B");
                    
                    #pragma warning disable CS0618 // Obsolete �x���𖳎�
                    DataContext = new MainWindowViewModel();
                    #pragma warning restore CS0618
                }
                catch (Exception fallbackEx)
                {
                    System.Diagnostics.Debug.WriteLine($"�ً}�t�H�[���o�b�N�����s: {fallbackEx.Message}");
                    MessageBox.Show($"MainWindow �̏������Ɏ��s���܂���:\n{ex.Message}\n\n�t�H�[���o�b�N�����s:\n{fallbackEx.Message}", 
                                    "�������G���[", MessageBoxButton.OK, MessageBoxImage.Error);
                }
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

                _logger?.LogDebug("MainWindow �I����������");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "MainWindow �I���������ɃG���[���������܂���");
            }
        }
    }
}