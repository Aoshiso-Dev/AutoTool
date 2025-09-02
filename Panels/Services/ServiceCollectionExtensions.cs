using Microsoft.Extensions.DependencyInjection;
using MacroPanels.Command.Interface;
using MacroPanels.Command.Class;
using MacroPanels.ViewModel;
using MacroPanels.ViewModel.Shared;

namespace MacroPanels.Services
{
    /// <summary>
    /// MacroPanelsのサービス登録拡張メソッド
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// MacroPanelsのサービスを登録
        /// </summary>
        public static IServiceCollection AddMacroPanelsServices(this IServiceCollection services)
        {
            // 変数ストアをシングルトンとして登録
            services.AddSingleton<IVariableStore, VariableStore>();
            
            // 各ViewModelを登録
            services.AddTransient<ButtonPanelViewModel>();
            services.AddTransient<ListPanelViewModel>();
            services.AddTransient<EditPanelViewModel>();
            services.AddTransient<LogPanelViewModel>();
            services.AddTransient<FavoritePanelViewModel>();
            
            // CommandHistoryManagerをシングルトンとして登録
            services.AddSingleton<CommandHistoryManager>();
            
            return services;
        }
    }
}