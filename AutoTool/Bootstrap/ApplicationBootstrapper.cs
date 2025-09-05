using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using AutoTool.Services;
using AutoTool.Model.CommandDefinition;

namespace AutoTool.Bootstrap
{
    /// <summary>
    /// �A�v���P�[�V�����N�������iDirectCommandRegistry�Ή��j
    /// </summary>
    public class ApplicationBootstrapper : IApplicationBootstrapper
    {
        private IHost? _host;
        private ILogger<ApplicationBootstrapper>? _logger;

        public IHost? Host => _host;

        public async Task<bool> InitializeAsync()
        {
            try
            {
                // �z�X�g�̍\�z
                _host = CreateHostBuilder().Build();

                // ���O�T�[�r�X�̎擾
                _logger = _host.Services.GetRequiredService<ILogger<ApplicationBootstrapper>>();
                _logger.LogInformation("ApplicationBootstrapper �������J�n");

                // �z�X�g�̊J�n
                await _host.StartAsync();

                // DirectCommandRegistry �̏�����
                DirectCommandRegistry.Initialize(_host.Services);
                _logger.LogInformation("DirectCommandRegistry ����������");

                _logger.LogInformation("ApplicationBootstrapper ����������");
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogCritical(ex, "ApplicationBootstrapper ���������ɒv���I�G���[");
                
                // �t�H�[���o�b�N�F�ً}�G���[�\��
                System.Windows.MessageBox.Show(
                    $"�A�v���P�[�V�����̏������Ɏ��s���܂���:\n{ex.Message}",
                    "�N���G���[",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                
                return false;
            }
        }

        public async Task ShutdownAsync()
        {
            try
            {
                _logger?.LogInformation("ApplicationBootstrapper �V���b�g�_�E���J�n");

                if (_host != null)
                {
                    await _host.StopAsync();
                    _host.Dispose();
                }

                _logger?.LogInformation("ApplicationBootstrapper �V���b�g�_�E������");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "ApplicationBootstrapper �V���b�g�_�E�����ɃG���[");
            }
        }

        private IHostBuilder CreateHostBuilder()
        {
            var exeDir = AppContext.BaseDirectory;
            var settingsPath = Path.Combine(exeDir, "Settings.json");

            return Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder()
                .ConfigureAppConfiguration((context, config) =>
                {
                    config.SetBasePath(exeDir);
                    if (File.Exists(settingsPath))
                    {
                        config.AddJsonFile("Settings.json", optional: true, reloadOnChange: true);
                    }
                })
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddDebug();
                    logging.AddConsole();
                    logging.SetMinimumLevel(LogLevel.Debug);
                })
                .ConfigureServices((context, services) =>
                {
                    // AutoTool�̂��ׂẴT�[�r�X��o�^
                    services.AddAutoToolServices();
                })
                .UseConsoleLifetime();
        }
    }

    /// <summary>
    /// �A�v���P�[�V�����N�������̃C���^�[�t�F�[�X
    /// </summary>
    public interface IApplicationBootstrapper
    {
        IHost? Host { get; }
        Task<bool> InitializeAsync();
        Task ShutdownAsync();
    }
}