using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace MacroPanels.ViewModel;

public partial class FavoritePanelViewModel : ObservableObject, IFavoritePanelViewModel
{
    [ObservableProperty]
    private bool _isRunning;

    [ObservableProperty]
    private ObservableCollection<string> _favoriteList = new();
    
    public FavoritePanelViewModel()
    {
        // TODO: お気に入りの永続化を実装
        FavoriteList.Add("test1");
        FavoriteList.Add("test2");
        FavoriteList.Add("test3");
    }

    public void SetRunningState(bool isRunning) => IsRunning = isRunning;

    public void Prepare() { }
}