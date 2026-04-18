using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.IO;
using AutoTool.Application.Ports;
using AutoTool.Domain.Macros;
using AutoTool.Application.Files;
using AutoTool.Infrastructure.Paths;

namespace AutoTool.Desktop.Panels.ViewModel;

public partial class FavoritePanelViewModel : ObservableObject, IFavoritePanelViewModel
{
    private readonly IFavoriteMacroStore _favoriteMacroStore;
    private const string FavoritesFileName = "favorites.json";

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
        ArgumentNullException.ThrowIfNull(favoriteMacroStore);
        _favoriteMacroStore = favoriteMacroStore;
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
        ArgumentNullException.ThrowIfNull(favorite);
        favorite.Normalize();
        if (!favorite.IsValid())
        {
            return;
        }

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
        var storagePath = GetFavoritesStoragePath();
        var loaded = _favoriteMacroStore.Load(storagePath);

        if (loaded is null)
        {
            var legacyPath = GetLegacyFavoritesStoragePath();
            loaded = _favoriteMacroStore.Load(legacyPath);
            if (loaded is not null)
            {
                _favoriteMacroStore.Save(storagePath, loaded);
                TryDeleteLegacyFavoritesFile(legacyPath);
            }
        }

        loaded ??= [];
        var normalized = loaded
            .Where(x => x is not null)
            .Select(x => x.Normalize())
            .Where(x => x.IsValid())
            .ToList();

        FavoriteList = [.. normalized];

        if (loaded.Count != FavoriteList.Count)
        {
            SaveFavorites();
        }
    }

    private void SaveFavorites()
    {
        _favoriteMacroStore.Save(GetFavoritesStoragePath(), FavoriteList);
    }

    private static string GetFavoritesStoragePath()
    {
        var settingsDirectory = Path.Combine(ApplicationPathResolver.GetApplicationDirectory(), "Settings");
        Directory.CreateDirectory(settingsDirectory);
        return Path.Combine(settingsDirectory, FavoritesFileName);
    }

    private static string GetLegacyFavoritesStoragePath()
    {
        var appDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AutoTool");
        return Path.Combine(appDir, "favorites.xml");
    }

    private static void TryDeleteLegacyFavoritesFile(string legacyPath)
    {
        try
        {
            if (File.Exists(legacyPath))
            {
                File.Delete(legacyPath);
            }
        }
        catch
        {
            // No-op: keep legacy file when cleanup fails.
        }
    }
}