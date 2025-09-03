using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using AutoTool.Services.Plugin;
using AutoTool.ViewModel;
using AutoTool.ViewModel.Panels;
using AutoTool.List.Class;
using AutoTool.Model.List.Type;
using AutoTool.Model.CommandDefinition;
using AutoTool.Helpers;

namespace AutoTool.Services
{
    /// <summary>
    /// �T�[�r�X�R���N�V�����g�����\�b�h
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// AutoTool�̑S�T�[�r�X��o�^
        /// </summary>
        public static IServiceCollection AddAutoToolServices(this IServiceCollection services)
        {
            // ���M���O�ݒ�
            services.AddLogging(builder =>
            {
                builder.AddDebug();
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Debug);
            });

            // �v���O�C���T�[�r�X
            services.AddSingleton<IPluginService, PluginService>();

            // ViewModel�̓o�^
            services.AddTransient<MainWindowViewModel>();
            services.AddTransient<FavoritePanelViewModel>();

            // ���f���̓o�^
            services.AddTransient<CommandList>();
            services.AddTransient<BasicCommandItem>();

            // JsonSerializerHelper�Ƀ��K�[��ݒ�
            services.AddSingleton<IServiceProvider>(provider =>
            {
                var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
                var logger = loggerFactory.CreateLogger("JsonSerializerHelper");
                JsonSerializerHelper.SetLogger(logger);
                return provider;
            });

            return services;
        }
    }
}