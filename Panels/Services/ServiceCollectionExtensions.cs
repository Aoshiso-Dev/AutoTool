using Microsoft.Extensions.DependencyInjection;
using MacroPanels.Command.Interface;
using MacroPanels.Command.Class;
using MacroPanels.ViewModel;
using MacroPanels.ViewModel.Shared;

namespace MacroPanels.Services
{
    /// <summary>
    /// MacroPanels�̃T�[�r�X�o�^�g�����\�b�h
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// MacroPanels�̃T�[�r�X��o�^
        /// </summary>
        public static IServiceCollection AddMacroPanelsServices(this IServiceCollection services)
        {
            // �ϐ��X�g�A���V���O���g���Ƃ��ēo�^
            services.AddSingleton<IVariableStore, VariableStore>();
            
            // �eViewModel��o�^
            services.AddTransient<ButtonPanelViewModel>();
            services.AddTransient<ListPanelViewModel>();
            services.AddTransient<EditPanelViewModel>();
            services.AddTransient<LogPanelViewModel>();
            services.AddTransient<FavoritePanelViewModel>();
            
            // CommandHistoryManager���V���O���g���Ƃ��ēo�^
            services.AddSingleton<CommandHistoryManager>();
            
            return services;
        }
    }
}