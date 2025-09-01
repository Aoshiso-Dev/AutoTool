using System;
using Microsoft.Extensions.DependencyInjection;
using MacroPanels.ViewModel;
using AutoTool.ViewModel;

namespace AutoTool.Services
{
    /// <summary>
    /// ViewModelファクトリの実装
    /// </summary>
    public class ViewModelFactory : IViewModelFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public ViewModelFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public T Create<T>() where T : class
        {
            return _serviceProvider.GetRequiredService<T>();
        }

        public ButtonPanelViewModel CreateButtonPanelViewModel()
        {
            return _serviceProvider.GetRequiredService<ButtonPanelViewModel>();
        }

        public ListPanelViewModel CreateListPanelViewModel()
        {
            return _serviceProvider.GetRequiredService<ListPanelViewModel>();
        }

        public EditPanelViewModel CreateEditPanelViewModel()
        {
            return _serviceProvider.GetRequiredService<EditPanelViewModel>();
        }

        public LogPanelViewModel CreateLogPanelViewModel()
        {
            return _serviceProvider.GetRequiredService<LogPanelViewModel>();
        }

        public FavoritePanelViewModel CreateFavoritePanelViewModel()
        {
            return _serviceProvider.GetRequiredService<FavoritePanelViewModel>();
        }
    }
}