using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using AutoTool.ViewModel.Shared;
using AutoTool.ViewModel.Panels;

namespace AutoTool.Services
{
    /// <summary>
    /// AutoTool サービス登録拡張メソッド
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// AutoTool サービス登録
        /// </summary>
        public static IServiceCollection AddAutoToolServices(this IServiceCollection services)
        {
            // Core services
            services.AddSingleton<CommandHistoryManager>();
            services.AddSingleton<AutoTool.Command.Class.VariableStore>();
            services.AddSingleton<AutoTool.Command.Interface.IVariableStore>(sp => sp.GetRequiredService<AutoTool.Command.Class.VariableStore>());

            // Panel ViewModels
            services.AddTransient<ButtonPanelViewModel>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<ButtonPanelViewModel>>();
                return new ButtonPanelViewModel(logger);
            });

            services.AddTransient<ListPanelViewModel>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<ListPanelViewModel>>();
                return new ListPanelViewModel(logger);
            });

            services.AddTransient<EditPanelViewModel>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<EditPanelViewModel>>();
                return new EditPanelViewModel(logger);
            });

            services.AddTransient<LogPanelViewModel>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<LogPanelViewModel>>();
                return new LogPanelViewModel(logger);
            });

            services.AddTransient<FavoritePanelViewModel>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<FavoritePanelViewModel>>();
                return new FavoritePanelViewModel(logger);
            });

            // MainWindowViewModel（各パネルを注入）
            services.AddTransient<AutoTool.MainWindowViewModel>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<AutoTool.MainWindowViewModel>>();
                var button = sp.GetRequiredService<ButtonPanelViewModel>();
                var list = sp.GetRequiredService<ListPanelViewModel>();
                var edit = sp.GetRequiredService<EditPanelViewModel>();
                var log = sp.GetRequiredService<LogPanelViewModel>();
                var favorite = sp.GetRequiredService<FavoritePanelViewModel>();
                return new AutoTool.MainWindowViewModel(
                    logger,
                    button,
                    list,
                    edit,
                    log,
                    favorite,
                    sp);
            });

            return services;
        }
    }
}