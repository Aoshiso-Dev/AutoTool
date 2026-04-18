using AutoTool.Domain.Macros;
using AutoTool.Automation.Contracts.Lists;
using System.IO;
using AutoTool.Infrastructure.Paths;

namespace AutoTool.Desktop.ViewModel;

public partial class MacroPanelViewModel
{
    private void HandleFavoriteAddRequested(string favoriteName)
    {
        try
        {
            var snapshotItems = _listPanel.CommandList.Clone().ToList();
            if (snapshotItems.Count == 0)
            {
                _notifier.ShowError("追加できるコマンドがありません。", "お気に入り追加");
                return;
            }

            var favoritesDirectory = GetFavoritesDirectory();
            Directory.CreateDirectory(favoritesDirectory);
            var snapshotPath = Path.Combine(favoritesDirectory, $"{Guid.NewGuid():N}.macro");

            _macroFileSerializer.SerializeToFile<IEnumerable<ICommandListItem>>(snapshotItems, snapshotPath);

            _favoritePanel.AddFavorite(FavoriteMacroEntry.Create(
                favoriteName,
                snapshotPath,
                _timeProvider.GetLocalNow()));
        }
        catch (Exception ex)
        {
            _logWriter.Write(ex);
            _notifier.ShowError($"お気に入り追加に失敗しました。\n{ex.Message}", "お気に入り追加");
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
            _notifier.ShowError($"お気に入り削除に失敗しました。\n{ex.Message}", "お気に入り削除");
        }
    }

    private void HandleFavoriteLoadRequested(FavoriteMacroEntry favorite)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(favorite.SnapshotPath) || !File.Exists(favorite.SnapshotPath))
            {
                _favoritePanel.RemoveFavorite(favorite);
                _notifier.ShowError("お気に入りファイルが見つからないため削除しました。", "お気に入り読み込み");
                return;
            }

            _listPanel.Load(favorite.SnapshotPath);
            _editPanel.SetListCount(_listPanel.GetCount());
            _commandHistory?.Clear();
        }
        catch (Exception ex)
        {
            _logWriter.Write(ex);
            _notifier.ShowError($"お気に入り読込に失敗しました。\n{ex.Message}", "お気に入り読み込み");
        }
    }

    private static string GetFavoritesDirectory()
    {
        return Path.Combine(
            ApplicationPathResolver.GetApplicationDirectory(),
            "Settings",
            "Favorites");
    }
}