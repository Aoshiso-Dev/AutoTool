using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using AutoTool.ViewModel.Shared;
using AutoTool.ViewModel;
using AutoTool.ViewModel.Panels;
using AutoTool.Command.Class;

namespace AutoTool.Services
{
    /// <summary>
    /// AutoTool �T�[�r�X�o�^�p�g�����\�b�h�i�g���Łj
    /// �eViewModel���ʂ�DI�o�^����悤�ɏC��
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// AutoTool �T�[�r�X��o�^
        /// </summary>
        public static IServiceCollection AddAutoToolServices(this IServiceCollection services)
        {
            try
            {
                // Core Services
                services.AddSingleton<CommandHistoryManager>();
                services.AddSingleton<VariableStore>();
                services.AddSingleton<AutoTool.Command.Interface.IVariableStore>(provider => provider.GetRequiredService<VariableStore>());
                
                // Panel ViewModels�i�ʂ�DI�o�^�j
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

                // ViewModelFactory�i�K�v�ɉ����āA�]���̃R�[�h�Ƃ̌݊����̂��߁j
                services.AddSingleton<IViewModelFactory, ViewModelFactory>();
                
                // MacroPanelViewModel�i�eViewModel��DI�o�R�Ŏ擾�j
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

                // MainWindowViewModel�i�eViewModel�𒼐�DI�Ŏ󂯎��j
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

                // Command Factory �T�[�r�X�v���o�C�_�[�ݒ�
                services.AddSingleton<IServiceProvider>(provider => provider);
                
                // Command Factory������
                var serviceProvider = services.BuildServiceProvider();
                CommandFactory.SetServiceProvider(serviceProvider);
                
                System.Diagnostics.Debug.WriteLine("AutoTool �g���T�[�r�X�o�^�����i�eViewModel��DI�����j");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Service registration error: {ex.Message}");
                
                // �Œ���̃T�[�r�X�̂ݓo�^���Čp��
                try
                {
                    services.AddSingleton<CommandHistoryManager>();
                    services.AddSingleton<IViewModelFactory, ViewModelFactory>();
                    
                    // �Œ����ViewModels��o�^
                    services.AddTransient<ButtonPanelViewModel>();
                    services.AddTransient<ListPanelViewModel>();
                    services.AddTransient<EditPanelViewModel>();
                    services.AddTransient<LogPanelViewModel>();
                    services.AddTransient<FavoritePanelViewModel>();
                    services.AddTransient<MacroPanelViewModel>();
                    services.AddTransient<MainWindowViewModel>();
                    
                    System.Diagnostics.Debug.WriteLine("�t�H�[���o�b�N�T�[�r�X�o�^����");
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