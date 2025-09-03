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
    /// サービスコレクション拡張メソッド
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// AutoToolの全サービスを登録
        /// </summary>
        public static IServiceCollection AddAutoToolServices(this IServiceCollection services)
        {
            // ロギング設定
            services.AddLogging(builder =>
            {
                builder.AddDebug();
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Debug);
            });

            // プラグインサービス
            services.AddSingleton<IPluginService, PluginService>();

            // ViewModelの登録
            services.AddTransient<MainWindowViewModel>();
            services.AddTransient<FavoritePanelViewModel>();

            // モデルの登録
            services.AddTransient<CommandList>();
            services.AddTransient<BasicCommandItem>();

            // JsonSerializerHelperにロガーを設定
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