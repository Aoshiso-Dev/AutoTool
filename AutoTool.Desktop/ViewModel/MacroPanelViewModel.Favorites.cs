using AutoTool.Application.History.Commands;
using AutoTool.Automation.Contracts.Lists;
using AutoTool.Automation.Runtime.Lists;
using AutoTool.Domain.Macros;
using AutoTool.Infrastructure.Paths;
using System.Collections.ObjectModel;
using System.IO;

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

            PublishStatusMessage($"テンプレートを追加しました: {favoriteName}");
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
            PublishStatusMessage($"テンプレートを削除しました: {favorite.Name}");
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
            PublishStatusMessage($"テンプレートを置換読込しました: {favorite.Name}");
        }
        catch (Exception ex)
        {
            _logWriter.Write(ex);
            _notifier.ShowError($"お気に入り読込に失敗しました。\n{ex.Message}", "お気に入り読み込み");
        }
    }

    private void HandleFavoriteInsertRequested(FavoriteMacroEntry favorite)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(favorite.SnapshotPath) || !File.Exists(favorite.SnapshotPath))
            {
                _favoritePanel.RemoveFavorite(favorite);
                _notifier.ShowError("テンプレートファイルが見つからないため削除しました。", "テンプレート挿入");
                return;
            }

            var loaded = _macroFileSerializer.DeserializeFromFile<ObservableCollection<ICommandListItem>>(favorite.SnapshotPath);
            if (loaded is null || loaded.Count == 0)
            {
                _notifier.ShowWarning("挿入できるコマンドがありません。", "テンプレート挿入");
                return;
            }

            var itemsToInsert = loaded
                .Select(x => x.Clone())
                .Select(item =>
                {
                    PrepareItemForPaste(item);
                    return item;
                })
                .ToList();

            var selectedItems = _listPanel.GetSelectedItems().Distinct().ToList();
            var insertIndex = selectedItems.Count > 0
                ? selectedItems
                    .Select(x => _listPanel.CommandList.Items.IndexOf(x))
                    .Where(x => x >= 0)
                    .DefaultIfEmpty(_listPanel.SelectedLineNumber)
                    .Max() + 1
                : _listPanel.SelectedLineNumber >= 0 ? _listPanel.SelectedLineNumber + 1 : _listPanel.GetCount();

            if (insertIndex < 0)
            {
                insertIndex = _listPanel.GetCount();
            }

            if (_commandHistory is not null)
            {
                var addCommand = new AddItemsCommand(
                    itemsToInsert,
                    insertIndex,
                    (item, index) => _listPanel.InsertAt(index, item),
                    index => _listPanel.RemoveAt(index));
                _commandHistory.ExecuteCommand(addCommand);
            }
            else
            {
                for (var i = 0; i < itemsToInsert.Count; i++)
                {
                    _listPanel.InsertAt(insertIndex + i, itemsToInsert[i]);
                }
            }

            _editPanel.SetListCount(_listPanel.GetCount());
            PublishStatusMessage($"テンプレートを挿入しました: {favorite.Name}（{itemsToInsert.Count}件）");
        }
        catch (Exception ex)
        {
            _logWriter.Write(ex);
            _notifier.ShowError($"テンプレート挿入に失敗しました。\n{ex.Message}", "テンプレート挿入");
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
