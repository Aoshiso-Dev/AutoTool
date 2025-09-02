using Microsoft.Extensions.DependencyInjection;
using AutoTool.ViewModel;
using AutoTool.ViewModel.Panels;

namespace AutoTool.Services
{
    /// <summary>
    /// Phase 5完全統合版：ViewModelファクトリ
    /// MacroPanels依存を削除し、AutoTool統合版のみ使用
    /// </summary>
    public class ViewModelFactory : AutoTool.ViewModel.IViewModelFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public ViewModelFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public AutoTool.ViewModel.Panels.ButtonPanelViewModel CreateButtonPanelViewModel()
        {
            return _serviceProvider.GetRequiredService<AutoTool.ViewModel.Panels.ButtonPanelViewModel>();
        }

        public AutoTool.ViewModel.Panels.ListPanelViewModel CreateListPanelViewModel()
        {
            return _serviceProvider.GetRequiredService<AutoTool.ViewModel.Panels.ListPanelViewModel>();
        }

        public AutoTool.ViewModel.Panels.EditPanelViewModel CreateEditPanelViewModel()
        {
            return _serviceProvider.GetRequiredService<AutoTool.ViewModel.Panels.EditPanelViewModel>();
        }

        public AutoTool.ViewModel.Panels.LogPanelViewModel CreateLogPanelViewModel()
        {
            return _serviceProvider.GetRequiredService<AutoTool.ViewModel.Panels.LogPanelViewModel>();
        }

        public AutoTool.ViewModel.Panels.FavoritePanelViewModel CreateFavoritePanelViewModel()
        {
            return _serviceProvider.GetRequiredService<AutoTool.ViewModel.Panels.FavoritePanelViewModel>();
        }
    }
}