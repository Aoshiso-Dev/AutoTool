using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using AutoTool.Services.Configuration;
using AutoTool.Services.Theme;
using AutoTool.Services.Performance;
using Microsoft.Extensions.Logging;
using MacroPanels.Plugin;
using System.Linq;
using System.Threading.Tasks;
using System;
using MacroPanels.Model.CommandDefinition;
using MacroPanels.ViewModel;
using AutoTool.ViewModel;

namespace AutoTool.Services
{
    /// <summary>
    /// �T�[�r�X�R���N�V�����g�����\�b�h
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// AutoTool�̃T�[�r�X��o�^
        /// </summary>
        public static IServiceCollection AddAutoToolServices(this IServiceCollection services)
        {
            // �ݒ�T�[�r�X
            services.AddSingleton<IConfigurationService, JsonConfigurationService>();
            
            // �e�[�}�T�[�r�X
            services.AddSingleton<IThemeService, WpfThemeService>();
            
            // �v���O�C���T�[�r�X�iMacroPanels���g�p�j
            services.AddSingleton<MacroPanels.Plugin.IPluginService, MacroPanels.Plugin.PluginService>();
            
            // �p�t�H�[�}���X�T�[�r�X
            services.AddSingleton<IPerformanceService, PerformanceService>();

            // �R�}���h���W�X�g��
            services.AddSingleton<ICommandRegistry, CommandRegistryAdapter>();

            // ViewModel�t�@�N�g��
            services.AddSingleton<IViewModelFactory, ViewModelFactory>();

            // ���b�Z�[�W�T�[�r�X
            services.AddSingleton<IMessageService, MessageBoxService>();

            // MacroPanels��ViewModels
            services.AddTransient<ButtonPanelViewModel>();
            services.AddTransient<ListPanelViewModel>();
            services.AddTransient<EditPanelViewModel>();
            services.AddTransient<LogPanelViewModel>();
            services.AddTransient<FavoritePanelViewModel>();
            
            // AutoTool��ViewModels
            services.AddTransient<MacroPanelViewModel>();
            
            return services;
        }

        /// <summary>
        /// �v���O�C���V�X�e����������
        /// </summary>
        public static async Task InitializePluginSystemAsync(this IServiceProvider serviceProvider)
        {
            var pluginService = serviceProvider.GetRequiredService<MacroPanels.Plugin.IPluginService>();
            var logger = serviceProvider.GetRequiredService<ILogger<MacroPanels.Plugin.PluginService>>();
            
            try
            {
                // �v���O�C����ǂݍ��� - DI,Plugin�u�����`�̓��e���g�p
                await pluginService.LoadAllPluginsAsync();
                
                logger.LogInformation("�v���O�C���V�X�e�������������܂���");
                
                // �ǂݍ��ݍς݃v���O�C���̏������O�o��
                var loadedPlugins = pluginService.GetLoadedPlugins();
                var availableCommands = pluginService.GetAvailablePluginCommands();
                
                logger.LogInformation("�ǂݍ��ݍς݃v���O�C��: {PluginCount}��", loadedPlugins.Count());
                logger.LogInformation("���p�\�ȃv���O�C���R�}���h: {CommandCount}��", availableCommands.Count());
                
                foreach (var plugin in loadedPlugins)
                {
                    logger.LogDebug("�v���O�C��: {PluginId} - {PluginName} (v{Version})", 
                        plugin.Id, plugin.Name, plugin.Version);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "�v���O�C���V�X�e�����������ɃG���[���������܂���");
            }
        }
    }
}