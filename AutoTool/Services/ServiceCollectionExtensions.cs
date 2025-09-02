using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using AutoTool.Services.Configuration;
using AutoTool.Services.Plugin;
using AutoTool.Services.Theme;
using AutoTool.Services.Performance;
using AutoTool.ViewModel.Shared;
using AutoTool.ViewModel.Panels;

namespace AutoTool.Services
{
    /// <summary>
    /// Phase 5���S�����ŁF�T�[�r�X�R���N�V�����g��
    /// MacroPanels�ˑ����폜���AAutoTool�����ł̂ݎg�p
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Phase 5���S�����ŁFAutoTool�T�[�r�X��o�^
        /// </summary>
        public static IServiceCollection AddAutoToolServices(this IServiceCollection services)
        {
            // �R�A �T�[�r�X
            services.AddSingleton<IConfigurationService, JsonConfigurationService>();
            services.AddSingleton<IThemeService, WpfThemeService>();
            services.AddSingleton<IPerformanceService, PerformanceService>();
            
            // Phase 5�����Ńv���O�C���V�X�e��
            services.AddSingleton<AutoTool.Services.Plugin.IPluginService, AutoTool.Services.Plugin.PluginService>();
            
            // Phase 5�����ŋ��L�T�[�r�X
            services.AddSingleton<CommandHistoryManager>();
            
            // Phase 5�ǉ��F���b�Z�[�W�T�[�r�X
            services.AddSingleton<IMessageService, MessageBoxService>();
            
            // �t�@�N�g���[�T�[�r�X
            services.AddSingleton<AutoTool.ViewModel.IViewModelFactory, ViewModelFactory>();
            
            // Phase 5�����Ń��C��ViewModel
            services.AddTransient<ViewModel.MacroPanelViewModel>();
            
            // Phase 5�����Ńp�l��ViewModels
            services.AddTransient<AutoTool.ViewModel.Panels.ButtonPanelViewModel>();
            services.AddTransient<AutoTool.ViewModel.Panels.ListPanelViewModel>();
            services.AddTransient<AutoTool.ViewModel.Panels.EditPanelViewModel>();
            services.AddTransient<AutoTool.ViewModel.Panels.LogPanelViewModel>();
            services.AddTransient<AutoTool.ViewModel.Panels.FavoritePanelViewModel>();
            
            return services;
        }
    }
}