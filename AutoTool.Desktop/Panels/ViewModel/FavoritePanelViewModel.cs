using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.IO;
using AutoTool.Core.Ports;
using AutoTool.Model;

namespace AutoTool.Panels.ViewModel;

public partial class FavoritePanelViewModel : ObservableObject, IFavoritePanelViewModel
{
    private readonly IFavoriteMacroStore _favoriteMacroStore;
    private const string FavoritesFileName = "favorites.xml";

    [ObservableProperty]
    private bool _isRunning;

    [ObservableProperty]
    private ObservableCollection<FavoriteMacroEntry> _favoriteList = [];

    [ObservableProperty]
    private FavoriteMacroEntry? _selectedFavorite;

    [ObservableProperty]
    private string _favoriteName = string.Empty;

    public event Action<string>? AddRequested;
    public event Action<FavoriteMacroEntry>? DeleteRequested;
    public event Action<FavoriteMacroEntry>? LoadRequested;

    public FavoritePanelViewModel(IFavoriteMacroStore favoriteMacroStore)
    {
        _favoriteMacroStore = favoriteMacroStore ?? throw new ArgumentNullException(nameof(favoriteMacroStore));
        LoadFavorites();
    }

    [RelayCommand(CanExecute = nameof(CanAddFavorite))]
    private void AddFavoriteFromCurrent()
    {
        var name = FavoriteName.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        AddRequested?.Invoke(name);
        FavoriteName = string.Empty;
    }

    [RelayCommand(CanExecute = nameof(CanLoadSelectedFavorite))]
    private void LoadSelectedFavorite()
    {
        if (SelectedFavorite is null)
        {
            return;
        }

        LoadRequested?.Invoke(SelectedFavorite);
    }

    [RelayCommand(CanExecute = nameof(CanDeleteSelectedFavorite))]
    private void DeleteSelectedFavorite()
    {
        if (SelectedFavorite is null)
        {
            return;
        }

        DeleteRequested?.Invoke(SelectedFavorite);
    }

    private bool CanAddFavorite() => !IsRunning && !string.IsNullOrWhiteSpace(FavoriteName);
    private bool CanLoadSelectedFavorite() => !IsRunning && SelectedFavorite is not null;
    private bool CanDeleteSelectedFavorite() => !IsRunning && SelectedFavorite is not null;

    partial void OnFavoriteNameChanged(string value)
    {
        AddFavoriteFromCurrentCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedFavoriteChanged(FavoriteMacroEntry? value)
    {
        LoadSelectedFavoriteCommand.NotifyCanExecuteChanged();
        DeleteSelectedFavoriteCommand.NotifyCanExecuteChanged();
    }

    partial void OnIsRunningChanged(bool value)
    {
        AddFavoriteFromCurrentCommand.NotifyCanExecuteChanged();
        LoadSelectedFavoriteCommand.NotifyCanExecuteChanged();
        DeleteSelectedFavoriteCommand.NotifyCanExecuteChanged();
    }

    public void AddFavorite(FavoriteMacroEntry favorite)
    {
        var existing = FavoriteList.FirstOrDefault(x => string.Equals(x.Name, favorite.Name, StringComparison.OrdinalIgnoreCase));
        if (existing is not null)
        {
            if (!string.IsNullOrWhiteSpace(existing.SnapshotPath) && File.Exists(existing.SnapshotPath))
            {
                File.Delete(existing.SnapshotPath);
            }
            FavoriteList.Remove(existing);
        }

        FavoriteList.Insert(0, favorite);
        SelectedFavorite = favorite;
        SaveFavorites();
    }

    public void RemoveFavorite(FavoriteMacroEntry favorite)
    {
        var existing = FavoriteList.FirstOrDefault(x => x.SnapshotPath == favorite.SnapshotPath);
        if (existing is null)
        {
            return;
        }

        var removedIndex = FavoriteList.IndexOf(existing);
        var wasSelected = ReferenceEquals(SelectedFavorite, existing);
        FavoriteList.Remove(existing);

        if (wasSelected)
        {
            if (FavoriteList.Count == 0)
            {
                SelectedFavorite = null;
            }
            else if (removedIndex < FavoriteList.Count)
            {
                SelectedFavorite = FavoriteList[removedIndex];
            }
            else
            {
                SelectedFavorite = FavoriteList[FavoriteList.Count - 1];
            }
        }

        SaveFavorites();
    }

    public void SetRunningState(bool isRunning) => IsRunning = isRunning;

    public void Prepare() { }

    private void LoadFavorites()
    {
        FavoriteList = _favoriteMacroStore.Load(GetFavoritesStoragePath()) ?? [];
    }

    private void SaveFavorites()
    {
        _favoriteMacroStore.Save(GetFavoritesStoragePath(), FavoriteList);
    }

    private static string GetFavoritesStoragePath()
    {
        var appDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AutoTool");

        Directory.CreateDirectory(appDir);
        return Path.Combine(appDir, FavoritesFileName);
    }
}
