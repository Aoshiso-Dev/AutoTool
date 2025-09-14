using AutoTool.Core.Commands;
using AutoTool.Core.Descriptors;
using AutoTool.Core.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using System.Windows;
using static OpenCvSharp.ML.DTrees;

namespace AutoTool.Desktop.ViewModels
{
    public partial class ListPanelViewModel : ObservableObject, IDisposable
    {
        private readonly IServiceProvider _services;
        private readonly ICommandRegistry _registry;
        private readonly JsonSerializerOptions _json;
        private readonly ILogger<ListPanelViewModel> _logger;

        public ObservableCollection<IAutoToolCommand> Root { get; } = new();
        
        [ObservableProperty]
        private ObservableCollection<FlatItem> flatItems = new();

        private object? _selectedNode = null;
        public object? SelectedNode
        {
            get => _selectedNode;
            set
            {
                SetProperty(ref _selectedNode, value);

                var cmd = (value as FlatItem)?.OriginalObject as IAutoToolCommand;

                // EditPanelに選択変更を通知
                WeakReferenceMessenger.Default.Send(new SelectNodeMessage(cmd));

                _logger.LogInformation("ノード選択: {NodeType} - {DisplayName}",
                    cmd?.GetType()?.Name ?? "null",
                    cmd?.DisplayName ?? (SelectedNode as FlatItem)?.Name ?? "N/A");
            }
        }

        public ListPanelViewModel(ILogger<ListPanelViewModel> logger, IServiceProvider services)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _services = services ?? throw new ArgumentNullException(nameof(services));
            _registry = services.GetService(typeof(ICommandRegistry)) as ICommandRegistry ?? throw new ArgumentNullException(nameof(ICommandRegistry));
            _json = services.GetService(typeof(JsonSerializerOptions)) as JsonSerializerOptions ?? new JsonSerializerOptions();

            // Messenger registrations
            WeakReferenceMessenger.Default.Register<NewFileMessage>(this, (r, m) => OnNewMessage());
            WeakReferenceMessenger.Default.Register<LoadFileMessage>(this, (r, m) => _ = OnLoadMessage());
            WeakReferenceMessenger.Default.Register<SaveFileMessage>(this, (r, m) => _ = OnSaveMessage());
            WeakReferenceMessenger.Default.Register<AddCommandMessage>(this, (r, m) => OnAddCommandMessage(m));
            WeakReferenceMessenger.Default.Register<RemoveCommandMessage>(this, (r, m) => OnRemoveCommandMessage());
            WeakReferenceMessenger.Default.Register<MoveUpCommandMessage>(this, (r, m) => OnMoveUpMessage());
            WeakReferenceMessenger.Default.Register<MoveDownCommandMessage>(this, (r, m) => OnMoveDownMessage());
            WeakReferenceMessenger.Default.Register<GetRootMacroMessaage>(this, (r, m) => m.Reply(Root));


            // Subscribe to Root changes to rebuild flat list
            Root.CollectionChanged += (s, e) => RebuildFlatItems();

            _logger.LogDebug("ListPanelViewModel initialized and message handlers registered");
        }

        private void RebuildFlatItems()
        {
            try
            {
                _logger.LogDebug("=== FlatItems再構築開始 ===");
                
                var items = new List<FlatItem>();
                foreach (var cmd in Root)
                {
                    AddCommandToFlatList(cmd, items, 0);
                }
                
                FlatItems.Clear();
                foreach (var item in items)
                {
                    FlatItems.Add(item);
                }
                
                _logger.LogDebug("FlatItems再構築完了: {Count}個", FlatItems.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "FlatItems再構築中にエラー");
            }
        }

        private void AddCommandToFlatList(IAutoToolCommand command, List<FlatItem> items, int indentLevel)
        {
            // Add the command itself
            var commandItem = new FlatItem();
            commandItem.OriginalObject = command;
            commandItem.DisplayName = command.DisplayName;
            commandItem.Type = command.Type;
            commandItem.IsEnabled = command.IsEnabled;
            commandItem.IndentLevel = indentLevel;
            commandItem.IsCommand = true;
            commandItem.IsBlock = false;

            // Get block count if command has blocks
            var blocksProp = command.GetType().GetProperty("Blocks");
            if (blocksProp != null)
            {
                var blocks = blocksProp.GetValue(command) as System.Collections.IList;
                commandItem.BlockCount = blocks?.Count ?? 0;
            }

            items.Add(commandItem);

            // Add blocks and their children
            if (blocksProp != null)
            {
                var blocks = blocksProp.GetValue(command) as System.Collections.IEnumerable;
                if (blocks != null)
                {
                    foreach (var block in blocks)
                    {
                        if (block is CommandBlock cmdBlock)
                        {
                            // Add the block
                            var blockItem = new FlatItem();
                            blockItem.OriginalObject = cmdBlock;
                            blockItem.Name = cmdBlock.Name;
                            blockItem.IndentLevel = indentLevel + 1;
                            blockItem.IsCommand = false;
                            blockItem.IsBlock = true;
                            blockItem.ChildCount = cmdBlock.Children.Count;
                            items.Add(blockItem);

                            // Add block's children
                            foreach (var child in cmdBlock.Children)
                            {
                                AddCommandToFlatList(child, items, indentLevel + 2);
                            }
                        }
                    }
                }
            }
        }

        private void OnNewMessage()
        {
            Root.Clear();
            SelectedNode = null;
            _logger.LogDebug("新規プロジェクトを作成しました");
        }

        private async Task OnLoadMessage()
        {
            var dlg = new OpenFileDialog
            {
                Filter = "AutoTool Script (*.json)|*.json|All files (*.*)|*.*",
                CheckFileExists = true,
                Multiselect = false
            };
            if (dlg.ShowDialog() != true) return;

            try
            {
                _logger.LogInformation("ファイルを読み込み中: {FilePath}", dlg.FileName);
                await using var fs = System.IO.File.OpenRead(dlg.FileName);
                var nodes = await CommandSerializer.LoadAsync(fs, _services, _registry, _json);
                Root.Clear();
                foreach (var n in nodes) Root.Add(n);
                _logger.LogInformation("ファイル読み込み完了: {Count} コマンド", nodes.Count());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ファイル読み込み中にエラーが発生しました: {FilePath}", dlg.FileName);
            }
        }

        private async Task OnSaveMessage()
        {
            var dlg = new SaveFileDialog
            {
                Filter = "AutoTool Script (*.json)|*.json|All files (*.*)|*.*",
                AddExtension = true,
                DefaultExt = ".json",
                OverwritePrompt = true
            };
            if (dlg.ShowDialog() != true) return;

            try
            {
                _logger.LogInformation("ファイルを保存中: {FilePath}", dlg.FileName);
                await using var fs = System.IO.File.Create(dlg.FileName);
                await CommandSerializer.SaveAsync(Root, fs, _registry, _json);
                _logger.LogInformation("ファイル保存完了: {Count} コマンド", Root.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ファイル保存中にエラーが発生しました: {FilePath}", dlg.FileName);
            }
        }

        private void OnAddCommandMessage(AddCommandMessage message)
        {
            try
            {
                if (message.Command is not IAutoToolCommand newCmd)
                {
                    _logger.LogWarning("AddCommandMessage contained non-IAutoToolCommand payload");
                    return;
                }

                _logger.LogInformation("=== AddCommand処理開始 ===");
                _logger.LogInformation("新しいコマンド: {CommandType} - {DisplayName}", newCmd.GetType().Name, newCmd.DisplayName);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    bool addedToBlock = false;

                    // If selected node is a CommandBlock, add to its Children
                    if (SelectedNode is FlatItem flatItem && flatItem.OriginalObject is CommandBlock block)
                    {
                        _logger.LogInformation("CommandBlockに追加: {BlockName} (現在の子数: {Count})", block.Name, block.Children.Count);
                        block.Children.Add(newCmd);
                        _logger.LogInformation("CommandBlockに追加完了: {BlockName} (新しい子数: {Count})", block.Name, block.Children.Count);
                        addedToBlock = true;
                    }
                    // If selected node is a command that exposes Blocks, try to add to its first block
                    else if (SelectedNode is FlatItem flatCmd && flatCmd.OriginalObject is IAutoToolCommand selCmd)
                    {
                        _logger.LogInformation("選択コマンドのブロックを検索中: {CommandType}", selCmd.GetType().Name);
                        
                        var blocksProp = selCmd.GetType().GetProperty("Blocks");
                        if (blocksProp != null)
                        {
                            var blocks = blocksProp.GetValue(selCmd) as System.Collections.IList;
                            if (blocks != null && blocks.Count > 0)
                            {
                                var firstBlock = blocks[0];
                                if (firstBlock is CommandBlock cmdBlock)
                                {
                                    _logger.LogInformation("CommandBlockとして処理: {BlockName} (現在の子数: {Count})", cmdBlock.Name, cmdBlock.Children.Count);
                                    cmdBlock.Children.Add(newCmd);
                                    _logger.LogInformation("CommandBlockに追加完了: {BlockName} (新しい子数: {Count})", cmdBlock.Name, cmdBlock.Children.Count);
                                    addedToBlock = true;
                                }
                            }
                        }
                    }

                    // Fallback: add to root
                    if (!addedToBlock)
                    {
                        _logger.LogInformation("ルートに追加: 現在の子数={Count}", Root.Count);
                        Root.Add(newCmd);
                        _logger.LogInformation("ルートに追加完了: 新しい子数={Count}", Root.Count);
                    }

                    // Rebuild flat items
                    RebuildFlatItems();

                    SelectedNode = FlatItems.FirstOrDefault(f => f.OriginalObject == newCmd);
                });

                _logger.LogInformation("=== AddCommand処理完了 ===");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while handling AddCommandMessage");
            }
        }

        private void OnRemoveCommandMessage()
        {
            try
            {
                if (SelectedNode is not FlatItem target)
                {
                    _logger.LogWarning("Remove requested but selected node is not a command");
                    return;
                }

                if (target.OriginalObject is not IAutoToolCommand cmd)
                {
                    _logger.LogWarning("Remove requested but selected node's original object is not a command");
                    return;
                }

                Application.Current.Dispatcher.Invoke(() =>
                {
                    var parentList = FindParentList(Root, cmd);
                    if (parentList != null)
                    {
                        parentList.Remove(cmd);
                        _logger.LogInformation("Removed command: {Type}", cmd.GetType().Name);
                        SelectedNode = null;
                    }

                    RebuildFlatItems();

                    SelectedNode = null;
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while handling RemoveCommandMessage");
            }
        }

        private void OnMoveUpMessage()
        {
            try
            {
                if (SelectedNode is not FlatItem target)
                {
                    _logger.LogWarning("MoveUp requested but selected node is not a command");
                    return;
                }

                if (target.OriginalObject is not IAutoToolCommand cmd)
                {
                    _logger.LogWarning("MoveUp requested but selected node's original object is not a command");
                    return;
                }

                Application.Current.Dispatcher.Invoke(() =>
                {
                    var parentList = FindParentList(Root, cmd);
                    if (parentList != null)
                    {
                        var idx = parentList.IndexOf(cmd);
                        if (idx > 0)
                        {
                            parentList.RemoveAt(idx);
                            parentList.Insert(idx - 1, cmd);
                            _logger.LogInformation("Moved up command: {Type}", target.GetType().Name);
                        }
                    }

                    RebuildFlatItems();

                    SelectedNode = FlatItems.FirstOrDefault(f => f.OriginalObject == cmd);
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while handling MoveUpCommandMessage");
            }
        }

        private void OnMoveDownMessage()
        {
            try
            {
                if (SelectedNode is not FlatItem target)
                {
                    _logger.LogWarning("MoveDown requested but selected node is not a command");
                    return;
                }

                if (target.OriginalObject is not IAutoToolCommand cmd)
                {
                    _logger.LogWarning("MoveDown requested but selected node's original object is not a command");
                    return;
                }

                Application.Current.Dispatcher.Invoke(() =>
                {
                    var parentList = FindParentList(Root, cmd);
                    if (parentList != null)
                    {
                        var idx = parentList.IndexOf(cmd);
                        if (idx >= 0 && idx < parentList.Count - 1)
                        {
                            parentList.RemoveAt(idx);
                            parentList.Insert(idx + 1, cmd);
                            _logger.LogInformation("Moved down command: {Type}", target.GetType().Name);
                        }
                    }

                    RebuildFlatItems();

                    SelectedNode = FlatItems.FirstOrDefault(f => f.OriginalObject == cmd);
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while handling MoveDownCommandMessage");
            }
        }

        // Recursively search for the parent ObservableCollection that contains target
        private ObservableCollection<IAutoToolCommand>? FindParentList(ObservableCollection<IAutoToolCommand> current, IAutoToolCommand target)
        {
            if (current.Contains(target)) return current;

            foreach (var cmd in current)
            {
                var blocksProp = cmd.GetType().GetProperty("Blocks");
                if (blocksProp == null) continue;
                var blocks = blocksProp.GetValue(cmd) as System.Collections.IEnumerable;
                if (blocks == null) continue;

                foreach (var block in blocks)
                {
                    var childrenProp = block.GetType().GetProperty("Children");
                    if (childrenProp == null) continue;
                    var children = childrenProp.GetValue(block) as System.Collections.IList;
                    if (children == null) continue;

                    // Try cast to ObservableCollection<IAutoToolCommand>
                    if (children is ObservableCollection<IAutoToolCommand> oc)
                    {
                        if (oc.Contains(target)) return oc;

                        var result = FindParentList(oc, target);
                        if (result != null) return result;
                    }
                    else
                    {
                        // If it's a general IList, check items and try recursive search
                        foreach (var child in children.OfType<IAutoToolCommand>())
                        {
                            var blocks2 = child.GetType().GetProperty("Blocks")?.GetValue(child) as System.Collections.IEnumerable;
                            if (blocks2 != null)
                            {
                                // To search deeper, build a temporary ObservableCollection from children if possible
                                // but prefer existing ObservableCollection path above
                                var found = FindParentListFromChildren(child, target);
                                if (found != null) return found;
                            }
                        }
                    }
                }
            }

            return null;
        }

        // Helper for deeper search when Children is not ObservableCollection<IAutoToolCommand>
        private ObservableCollection<IAutoToolCommand>? FindParentListFromChildren(IAutoToolCommand parentCmd, IAutoToolCommand target)
        {
            var blocksProp = parentCmd.GetType().GetProperty("Blocks");
            if (blocksProp == null) return null;
            var blocks = blocksProp.GetValue(parentCmd) as System.Collections.IEnumerable;
            if (blocks == null) return null;

            foreach (var block in blocks)
            {
                var childrenProp = block.GetType().GetProperty("Children");
                if (childrenProp == null) continue;
                var children = childrenProp.GetValue(block) as System.Collections.IList;
                if (children == null) continue;

                if (children is ObservableCollection<IAutoToolCommand> oc && oc.Contains(target)) return oc;

                foreach (var child in children.OfType<IAutoToolCommand>())
                {
                    var found = FindParentListFromChildren(child, target);
                    if (found != null) return found;
                }
            }

            return null;
        }

        public void Dispose()
        {
            try
            {
                WeakReferenceMessenger.Default.UnregisterAll(this);
            }
            catch { }
        }
    }

    // Flat item wrapper for ListView
    public class FlatItem
    {
        public object? OriginalObject { get; set; }
        public string DisplayName { get; set; } = "";
        public string Type { get; set; } = "";
        public string Name { get; set; } = "";
        public bool IsEnabled { get; set; } = true;
        public int IndentLevel { get; set; }
        public bool IsCommand { get; set; }
        public bool IsBlock { get; set; }
        public int BlockCount { get; set; }
        public int ChildCount { get; set; }
    }
}

// Missing message class - add at bottom of file
public record SelectNodeMessage(object? Node);
