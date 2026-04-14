using AutoTool.Model;
using AutoTool.Panels.Model.List.Interface;
using System.IO;

namespace AutoTool.ViewModel;

public partial class MacroPanelViewModel
{
    private void HandleFavoriteAddRequested(string favoriteName)
    {
        try
        {
            var snapshotItems = _listPanel.CommandList.Clone().ToList();
            if (snapshotItems.Count == 0)
            {
                _notifier.ShowError("お気に入りに追加するコマンドがありません。", "お気に入り");
                return;
            }

            var favoritesDirectory = GetFavoritesDirectory();
            Directory.CreateDirectory(favoritesDirectory);
            var snapshotPath = Path.Combine(favoritesDirectory, $"{Guid.NewGuid():N}.macro");

            _macroFileSerializer.SerializeToFile<IEnumerable<ICommandListItem>>(snapshotItems, snapshotPath);

            _favoritePanel.AddFavorite(new FavoriteMacroEntry
            {
                Name = favoriteName,
                SnapshotPath = snapshotPath,
                CreatedAt = DateTime.Now
            });
        }
        catch (Exception ex)
        {
            _logWriter.Write(ex);
            _notifier.ShowError($"お気に入りの追加に失敗しました。\n{ex.Message}", "お気に入り");
        }
    }

    private void HandleFavoriteDeleteRequested(FavoriteMacroEntry favorite)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(favorite.SnapshotPath) && File.Exists(favorite.SnapshotPath))
            {
                File.Delete(favorite.SnapshotPath);
            }

            _favoritePanel.RemoveFavorite(favorite);
        }
        catch (Exception ex)
        {
            _logWriter.Write(ex);
            _notifier.ShowError($"お気に入りの削除に失敗しました。\n{ex.Message}", "お気に入り");
        }
    }

    private void HandleFavoriteLoadRequested(FavoriteMacroEntry favorite)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(favorite.SnapshotPath) || !File.Exists(favorite.SnapshotPath))
            {
                _favoritePanel.RemoveFavorite(favorite);
                _notifier.ShowError("お気に入りの保存ファイルが見つかりませんでした。", "お気に入り");
                return;
            }

            _listPanel.Load(favorite.SnapshotPath);
            _editPanel.SetListCount(_listPanel.GetCount());
            _commandHistory?.Clear();
        }
        catch (Exception ex)
        {
            _logWriter.Write(ex);
            _notifier.ShowError($"お気に入りの読み込みに失敗しました。\n{ex.Message}", "お気に入り");
        }
    }

    private static string GetFavoritesDirectory()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AutoTool",
            "Favorites");
    }
}
