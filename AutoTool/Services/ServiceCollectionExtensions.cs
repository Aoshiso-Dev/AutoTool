using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using AutoTool.ViewModel.Shared;
using AutoTool.ViewModel;
using AutoTool.ViewModel.Panels;
using AutoTool.Command.Class;

namespace AutoTool.Services
{
    /// <summary>
    /// AutoTool サービス登録用拡張メソッド（拡張版）
    /// 各ViewModelを個別にDI登録するように修正
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// AutoTool サービスを登録
        /// </summary>
        public static IServiceCollection AddAutoToolServices(this IServiceCollection services)
        {
            try
            {
                // Core Services
                services.AddSingleton<CommandHistoryManager>();
                services.AddSingleton<VariableStore>();
                services.AddSingleton<AutoTool.Command.Interface.IVariableStore>(provider => provider.GetRequiredService<VariableStore>());
                
                // Panel ViewModels（個別にDI登録）
                services.AddTransient<ButtonPanelViewModel>(provider =>
                {
                    var logger = provider.GetRequiredService<ILogger<ButtonPanelViewModel>>();
                    return new ButtonPanelViewModel(logger);
                });
                
                services.AddTransient<ListPanelViewModel>(provider =>
                {
                    var logger = provider.GetRequiredService<ILogger<ListPanelViewModel>>();
                    return new ListPanelViewModel(logger);
                });
                
                services.AddTransient<EditPanelViewModel>(provider =>
                {
                    var logger = provider.GetRequiredService<ILogger<EditPanelViewModel>>();
                    return new EditPanelViewModel(logger);
                });
                
                services.AddTransient<LogPanelViewModel>(provider =>
                {
                    var logger = provider.GetRequiredService<ILogger<LogPanelViewModel>>();
                    return new LogPanelViewModel(logger);
                });
                
                services.AddTransient<FavoritePanelViewModel>(provider =>
                {
                    var logger = provider.GetRequiredService<ILogger<FavoritePanelViewModel>>();
                    return new FavoritePanelViewModel(logger);
                });

                // ViewModelFactory（必要に応じて、従来のコードとの互換性のため）
                services.AddSingleton<IViewModelFactory, ViewModelFactory>();
                
                // MacroPanelViewModel（各ViewModelをDI経由で取得）
                services.AddTransient<MacroPanelViewModel>(provider => 
                {
                    var logger = provider.GetRequiredService<ILogger<MacroPanelViewModel>>();
                    var buttonPanelViewModel = provider.GetRequiredService<ButtonPanelViewModel>();
                    var listPanelViewModel = provider.GetRequiredService<ListPanelViewModel>();
                    var editPanelViewModel = provider.GetRequiredService<EditPanelViewModel>();
                    var logPanelViewModel = provider.GetRequiredService<LogPanelViewModel>();
                    var favoritePanelViewModel = provider.GetRequiredService<FavoritePanelViewModel>();
                    var history = provider.GetRequiredService<CommandHistoryManager>();
                    
                    return new MacroPanelViewModel(
                        logger,
                        buttonPanelViewModel,
                        listPanelViewModel,
                        editPanelViewModel,
                        logPanelViewModel,
                        favoritePanelViewModel,
                        history,
                        (System.IServiceProvider)provider);
                });

                // MainWindowViewModel（各ViewModelを直接DIで受け取る）
                services.AddTransient<MainWindowViewModel>(provider =>
                {
                    var logger = provider.GetRequiredService<ILogger<MainWindowViewModel>>();
                    var macroPanelViewModel = provider.GetRequiredService<MacroPanelViewModel>();
                    var buttonPanelViewModel = provider.GetRequiredService<ButtonPanelViewModel>();
                    var listPanelViewModel = provider.GetRequiredService<ListPanelViewModel>();
                    var editPanelViewModel = provider.GetRequiredService<EditPanelViewModel>();
                    var logPanelViewModel = provider.GetRequiredService<LogPanelViewModel>();
                    var favoritePanelViewModel = provider.GetRequiredService<FavoritePanelViewModel>();

                    return new MainWindowViewModel(
                        logger,
                        macroPanelViewModel,
                        buttonPanelViewModel,
                        listPanelViewModel,
                        editPanelViewModel,
                        logPanelViewModel,
                        favoritePanelViewModel);
                });

                // Command Factory サービスプロバイダー設定
                services.AddSingleton<IServiceProvider>(provider => provider);
                
                // Command Factory初期化
                var serviceProvider = services.BuildServiceProvider();
                CommandFactory.SetServiceProvider(serviceProvider);
                
                System.Diagnostics.Debug.WriteLine("AutoTool 拡張サービス登録完了（各ViewModelをDI注入）");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Service registration error: {ex.Message}");
                
                // 最低限のサービスのみ登録して継続
                try
                {
                    services.AddSingleton<CommandHistoryManager>();
                    services.AddSingleton<IViewModelFactory, ViewModelFactory>();
                    
                    // 最低限のViewModelsを登録
                    services.AddTransient<ButtonPanelViewModel>();
                    services.AddTransient<ListPanelViewModel>();
                    services.AddTransient<EditPanelViewModel>();
                    services.AddTransient<LogPanelViewModel>();
                    services.AddTransient<FavoritePanelViewModel>();
                    services.AddTransient<MacroPanelViewModel>();
                    services.AddTransient<MainWindowViewModel>();
                    
                    System.Diagnostics.Debug.WriteLine("フォールバックサービス登録完了");
                }
                catch (Exception fallbackEx)
                {
                    System.Diagnostics.Debug.WriteLine($"Fallback service registration error: {fallbackEx.Message}");
                    throw;
                }
            }

            return services;
        }
    }
}