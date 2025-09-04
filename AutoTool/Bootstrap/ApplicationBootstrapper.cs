using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using AutoTool.Services;
using AutoTool.Services.Safety;
using AutoTool.ViewModel;
using AutoTool.Helpers;
using AutoTool.Logging;
using AutoTool.List.Class; // CommandListService�p

namespace AutoTool.Bootstrap
{
    /// <summary>
    /// �A�v���P�[�V�������������Ǘ�����T�[�r�X
    /// </summary>
    public interface IApplicationBootstrapper
    {
        Task<bool> InitializeAsync();
        Task ShutdownAsync();
        IHost Host { get; }
    }

    /// <summary>
    /// �A�v���P�[�V�����������̎���
    /// </summary>
    public class ApplicationBootstrapper : IApplicationBootstrapper
    {
        private IHost? _host;
        private readonly ILogger<ApplicationBootstrapper> _logger;

        public IHost Host => _host ?? throw new InvalidOperationException("Host is not initialized");

        public ApplicationBootstrapper()
        {
            // �ꎞ�I�ȃ��K�[���쐬�i��Œu��������j
            using var loggerFactory = LoggerFactory.Create(builder => builder.AddDebug().AddConsole());
            _logger = loggerFactory.CreateLogger<ApplicationBootstrapper>();
        }

        public async Task<bool> InitializeAsync()
        {
            try
            {
                _logger.LogInformation("ApplicationBootstrapper �������J�n");

                // ���S�ȋN���`�F�b�N
                if (!SafeActivator.CanActivateApplication())
                {
                    _logger.LogError("�A�v���P�[�V�����̋N�������𖞂����Ă��܂���");
                    return false;
                }

                _logger.LogDebug("�N�������`�F�b�N����");

                // DI�R���e�i�̍\�z
                _logger.LogDebug("DI�R���e�i�\�z�J�n");
                _host = CreateHost();
                _logger.LogDebug("DI�R���e�i�\�z����");
                
                // ���K�[�𐳎��Ȃ��̂ɍX�V
                var realLogger = _host.Services.GetRequiredService<ILogger<ApplicationBootstrapper>>();
                
                realLogger.LogInformation("AutoTool �A�v���P�[�V�����������J�n");

                // �z�X�g���J�n
                realLogger.LogDebug("Host�J�n");
                await _host.StartAsync();
                realLogger.LogDebug("Host�J�n����");

                // �e��T�[�r�X�̏�����
                realLogger.LogDebug("�T�[�r�X�������J�n");
                await InitializeServicesAsync(_host.Services);
                realLogger.LogDebug("�T�[�r�X����������");

                // �o�^����Ă���T�[�r�X�ꗗ�����O�o�́i�f�o�b�O�p�j
                LogRegisteredServices(_host.Services, realLogger);

                realLogger.LogInformation("AutoTool �A�v���P�[�V��������������");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "�A�v���P�[�V�����������Ɏ��s���܂���");
                return false;
            }
        }

        private void LogRegisteredServices(IServiceProvider services, ILogger logger)
        {
            try
            {
                logger.LogDebug("=== �o�^�T�[�r�X�m�F�J�n ===");
                
                var serviceTypes = new[]
                {
                    typeof(ILogger<MainWindowViewModel>),
                    typeof(ILogger<CommandListService>), // CommandListService�p��ILogger��ǉ�
                    typeof(CommandListService), // CommandListService��ǉ�
                    typeof(AutoTool.Services.IRecentFileService),
                    typeof(AutoTool.Services.Plugin.IPluginService),
                    typeof(AutoTool.Services.UI.IMainWindowMenuService),
                    typeof(AutoTool.Services.UI.IMainWindowButtonService),
                    typeof(AutoTool.Services.UI.IMainWindowCommandService),
                    typeof(MainWindowViewModel),
                    typeof(AutoTool.ViewModel.Panels.EditPanelViewModel),
                    typeof(AutoTool.ViewModel.Panels.ListPanelViewModel),
                };

                foreach (var serviceType in serviceTypes)
                {
                    var service = services.GetService(serviceType);
                    if (service != null)
                    {
                        logger.LogDebug("? {ServiceType} -> {ImplementationType}", 
                            serviceType.Name, service.GetType().Name);
                    }
                    else
                    {
                        logger.LogWarning("? {ServiceType} -> �T�[�r�X��������܂���", serviceType.Name);
                    }
                }
                
                logger.LogDebug("=== �o�^�T�[�r�X�m�F�I�� ===");
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "�T�[�r�X�m�F���ɃG���[");
            }
        }

        public async Task ShutdownAsync()
        {
            try
            {
                if (_host != null)
                {
                    _logger.LogInformation("AutoTool �A�v���P�[�V�����I���J�n");
                    await _host.StopAsync();
                    _host.Dispose();
                    _logger.LogInformation("AutoTool �A�v���P�[�V�����I������");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�A�v���P�[�V�����I�����ɃG���[���������܂���");
            }
        }

        private IHost CreateHost()
        {
            try
            {
                _logger.LogDebug("Host�쐬�J�n");
                
                var host = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder()
                    .ConfigureAppConfiguration((ctx, cfg) =>
                    {
                        _logger.LogDebug("�ݒ�\�z�J�n");
                        cfg.SetBasePath(AppContext.BaseDirectory);
                        cfg.AddJsonFile("Settings.json", optional: true, reloadOnChange: true);
                        cfg.AddEnvironmentVariables(prefix: "AUTOTOOL_");
                        _logger.LogDebug("�ݒ�\�z����");
                    })
                    .ConfigureLogging((ctx, logging) =>
                    {
                        _logger.LogDebug("���O�ݒ�J�n");
                        logging.ClearProviders();
                        logging.SetMinimumLevel(LogLevel.Trace);
                        logging.AddConfiguration(ctx.Configuration.GetSection("Logging"));
                        logging.AddConsole();
                        logging.AddDebug();
                        logging.AddSimpleFile();
                        _logger.LogDebug("���O�ݒ芮��");
                    })
                    .ConfigureServices((context, services) =>
                    {
                        _logger.LogDebug("�T�[�r�X�o�^�J�n");
                        
                        // AutoTool�T�[�r�X�o�^
                        services.AddAutoToolServices();
                        
                        // �A�v���P�[�V�����ŗL�̃T�[�r�X
                        services.AddSingleton<IApplicationBootstrapper, ApplicationBootstrapper>();
                        
                        _logger.LogDebug("�T�[�r�X�o�^����");
                    })
                    .Build();
                
                _logger.LogDebug("Host�쐬����");
                return host;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Host�쐬���ɃG���[");
                throw;
            }
        }

        private async Task InitializeServicesAsync(IServiceProvider serviceProvider)
        {
            var logger = serviceProvider.GetRequiredService<ILogger<ApplicationBootstrapper>>();

            try
            {
                // �v���O�C���V�X�e����������
                logger.LogDebug("�v���O�C���T�[�r�X�������J�n");
                var pluginService = serviceProvider.GetService<AutoTool.Services.Plugin.IPluginService>();
                if (pluginService != null)
                {
                    await pluginService.LoadAllPluginsAsync();
                    logger.LogInformation("�v���O�C���V�X�e������������");
                }
                else
                {
                    logger.LogWarning("�v���O�C���T�[�r�X��������܂���");
                }

                // �t�@�N�g���[�T�[�r�X�̏�����
                logger.LogDebug("�t�@�N�g���[�T�[�r�X�������J�n");
                AutoTool.Model.MacroFactory.MacroFactory.SetServiceProvider(serviceProvider);
                AutoTool.Model.CommandDefinition.CommandRegistry.Initialize();
                logger.LogInformation("�t�@�N�g���[�T�[�r�X����������");

                // Helper�T�[�r�X�̏������i�x�����s�j
                logger.LogDebug("�w���p�[�T�[�r�X�������J�n");
                try
                {
                    var helperInitializer = serviceProvider.GetService<Action<IServiceProvider>>();
                    if (helperInitializer != null)
                    {
                        helperInitializer(serviceProvider);
                        logger.LogDebug("JsonSerializerHelper����������");
                    }

                    ViewModelLocator.Initialize(serviceProvider);
                    logger.LogDebug("ViewModelLocator����������");
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Helper�T�[�r�X�������Ōx�����������܂������A�p�����܂�");
                }
                
                logger.LogInformation("�w���p�[�T�[�r�X����������");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "�T�[�r�X���������ɃG���[���������܂���");
                throw;
            }
        }
    }
}