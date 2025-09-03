using Microsoft.Extensions.Logging;
using AutoTool.ViewModel.Panels;

namespace AutoTool.ViewModel
{
    /// <summary>
    /// Phase 5äÆëSìùçáî≈ÅFViewModelFactoryé¿ëï
    /// </summary>
    public class ViewModelFactory : IViewModelFactory
    {
        private readonly ILoggerFactory _loggerFactory;

        public ViewModelFactory(ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory ?? throw new System.ArgumentNullException(nameof(loggerFactory));
        }

        public ButtonPanelViewModel CreateButtonPanelViewModel()
        {
            return new ButtonPanelViewModel(_loggerFactory.CreateLogger<ButtonPanelViewModel>());
        }

        public ListPanelViewModel CreateListPanelViewModel()
        {
            return new ListPanelViewModel(_loggerFactory.CreateLogger<ListPanelViewModel>());
        }

        public EditPanelViewModel CreateEditPanelViewModel()
        {
            return new EditPanelViewModel(_loggerFactory.CreateLogger<EditPanelViewModel>());
        }

        public LogPanelViewModel CreateLogPanelViewModel()
        {
            return new LogPanelViewModel(_loggerFactory.CreateLogger<LogPanelViewModel>());
        }

        public FavoritePanelViewModel CreateFavoritePanelViewModel()
        {
            return new FavoritePanelViewModel(_loggerFactory.CreateLogger<FavoritePanelViewModel>());
        }
    }
}