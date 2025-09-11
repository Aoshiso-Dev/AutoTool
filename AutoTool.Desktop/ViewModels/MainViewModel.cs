// File: AutoTool.Desktop/ViewModels/MainViewModel.cs
#nullable enable
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AutoTool.Core.Abstractions;
using AutoTool.Core.Commands;
using AutoTool.Core.Descriptors;
using AutoTool.Core.Runtime;
using AutoTool.Core.Serialization;
using Microsoft.Win32;
using Microsoft.Extensions.Logging;

namespace AutoTool.Desktop.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly ICommandRunner _runner;
    private readonly ICommandRegistry _registry;
    private readonly IServiceProvider _services;
    private readonly IExecutionContext _executionContext;
    private readonly ILogger<MainViewModel> _logger;
    private readonly JsonSerializerOptions _json;

    public ObservableCollection<IAutoToolCommand> Root { get; } = new();

    [ObservableProperty] private object? selectedNode;
    [ObservableProperty] private object? selectedEditor; // ここは後で本物のEditor VMに差し替え
    [ObservableProperty] private string searchText = string.Empty;

    public MainViewModel(
        ICommandRunner runner,
        ICommandRegistry registry,
        IServiceProvider services,
        IExecutionContext executionContext,
        ILogger<MainViewModel> logger)
    {
        _runner = runner;
        _registry = registry;
        _services = services;
        _executionContext = executionContext;
        _logger = logger;
        _json = JsonOptions.Create();

        _logger.LogInformation("MainViewModel を初期化しています");

        // 初期ノード（必要なら）
        TryAddSample();
    }

    // ===== File =====

    [RelayCommand]
    private void New()
    {
        Root.Clear();
        SelectedNode = null;
        _logger.LogDebug("新規プロジェクトを作成しました");
    }

    [RelayCommand]
    private async Task Load()
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
            await using var fs = File.OpenRead(dlg.FileName);
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

    [RelayCommand]
    private async Task Save()
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
            await using var fs = File.Create(dlg.FileName);
            await CommandSerializer.SaveAsync(Root, fs, _registry, _json);
            _logger.LogInformation("ファイル保存完了: {Count} コマンド", Root.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ファイル保存中にエラーが発生しました: {FilePath}", dlg.FileName);
        }
    }

    // ===== Edit =====

    [RelayCommand]
    private void Add()
    {
        // 簡易追加：最初のDescriptorでデフォルトコマンドを作成
        var desc = _registry.All.FirstOrDefault();
        if (desc is null) 
        {
            _logger.LogWarning("利用可能なコマンドDescriptorがありません");
            return;
        }

        try
        {
            var cmd = desc.CreateCommand(desc.CreateDefaultSettings(), _services);
            InsertCommandAtSelection(cmd);
            _logger.LogDebug("コマンドを追加しました: {CommandType}", desc.Type);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "コマンド追加中にエラーが発生しました: {CommandType}", desc.Type);
        }
    }

    [RelayCommand]
    private void Remove()
    {
        if (SelectedNode is not IAutoToolCommand cmd) return;

        if (TryFindParent(Root, cmd, out var parent))
        {
            parent.Remove(cmd);
            if (ReferenceEquals(SelectedNode, cmd)) SelectedNode = null;
            _logger.LogDebug("コマンドを削除しました: {CommandType}", cmd.Type);
        }
    }

    [RelayCommand]
    private void MoveUp()
    {
        if (SelectedNode is not IAutoToolCommand cmd) return;
        if (!TryFindParent(Root, cmd, out var parent)) return;

        var idx = parent.IndexOf(cmd);
        if (idx > 0) parent.Move(idx, idx - 1);
    }

    [RelayCommand]
    private void MoveDown()
    {
        if (SelectedNode is not IAutoToolCommand cmd) return;
        if (!TryFindParent(Root, cmd, out var parent)) return;

        var idx = parent.IndexOf(cmd);
        if (idx >= 0 && idx < parent.Count - 1) parent.Move(idx, idx + 1);
    }

    [RelayCommand]
    private void SelectNode(object? node)
    {
        SelectedNode = node;
        // TODO: SelectedEditor = EditorFactory.From(node); // 実装にあわせて
    }

    // ===== Run =====

    [RelayCommand]
    private async Task Run(CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("マクロ実行を開始します: {Count} コマンド", Root.Count);
            await _runner.RunAsync(Root, _executionContext, ct);
            _logger.LogInformation("マクロ実行が完了しました");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "マクロ実行中にエラーが発生しました");
        }
    }

    [RelayCommand]
    private void Stop()
    {
        // Run は IAsyncRelayCommand になるので Cancel が使える
        RunCommand.Cancel();
        _logger.LogInformation("マクロ実行を停止しました");
    }

    // ===== Helpers =====

    private void InsertCommandAtSelection(IAutoToolCommand cmd)
    {
        if (SelectedNode is CommandBlock block)
        {
            block.Children.Add(cmd);
        }
        else
        {
            // ルートに追加
            Root.Add(cmd);
        }
        SelectedNode = cmd;
    }

    private static bool TryFindParent(ObservableCollection<IAutoToolCommand> current,
                                      IAutoToolCommand target,
                                      out ObservableCollection<IAutoToolCommand> parent)
    {
        // 1) 直下
        if (current.Contains(target))
        {
            parent = current;
            return true;
        }

        // 2) 再帰的にブロックを探索
        foreach (var cmd in current)
        {
            if (cmd is IHasBlocks hb)
            {
                foreach (var block in hb.Blocks)
                {
                    // ブロック直下
                    if (block.Children.Contains(target))
                    {
                        parent = block.Children;
                        return true;
                    }
                    // 更に深掘り
                    if (TryFindParent(block.Children, target, out parent))
                        return true;
                }
            }
        }

        parent = null!;
        return false;
    }

    // デモ用（任意）
    private void TryAddSample()
    {
        try
        {
            _logger.LogDebug("サンプルコマンドの追加を試行します");
            
            // CommandRegistryに登録されているDescriptorを確認
            var allDescriptors = _registry.All.ToList();
            _logger.LogDebug("登録されているDescriptor数: {Count}", allDescriptors.Count);
            
            foreach (var descriptor in allDescriptors)
            {
                _logger.LogDebug("登録済みDescriptor: {Type} ({DisplayName})", descriptor.Type, descriptor.DisplayName);
            }

            // waitコマンドを探す
            if (_registry.TryGet("wait", out var desc) && desc != null)
            {
                _logger.LogDebug("WaitDescriptorが見つかりました: {DisplayName}", desc.DisplayName);
                
                var settings = desc.CreateDefaultSettings();
                var cmd = desc.CreateCommand(settings, _services);
                Root.Add(cmd);
                
                _logger.LogInformation("サンプルWaitコマンドを追加しました");
            }
            else
            {
                _logger.LogWarning("WaitDescriptorが見つかりません。利用可能なタイプ: {Types}", 
                    string.Join(", ", allDescriptors.Select(d => d.Type)));

                // 代替として最初のDescriptorを使用
                var firstDesc = allDescriptors.FirstOrDefault();
                if (firstDesc != null)
                {
                    _logger.LogInformation("代替として {Type} コマンドを追加します", firstDesc.Type);
                    var settings = firstDesc.CreateDefaultSettings();
                    var cmd = firstDesc.CreateCommand(settings, _services);
                    Root.Add(cmd);
                }
                else
                {
                    _logger.LogError("利用可能なDescriptorが一つもありません");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "サンプルコマンド追加中にエラーが発生しました");
        }
    }
}
