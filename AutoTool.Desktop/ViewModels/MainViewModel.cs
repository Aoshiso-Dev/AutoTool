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

namespace AutoTool.Desktop.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly ICommandRunner _runner;
    private readonly ICommandRegistry _registry;
    private readonly IServiceProvider _services;
    private readonly IExecutionContext _executionContext;
    private readonly JsonSerializerOptions _json;

    public ObservableCollection<IAutoToolCommand> Root { get; } = new();

    [ObservableProperty] private object? selectedNode;
    [ObservableProperty] private object? selectedEditor; // ここは後で本物のEditor VMに差し替え
    [ObservableProperty] private string searchText = string.Empty;

    public MainViewModel(
        ICommandRunner runner,
        ICommandRegistry registry,
        IServiceProvider services,
        IExecutionContext executionContext)
    {
        _runner = runner;
        _registry = registry;
        _services = services;
        _executionContext = executionContext;
        _json = JsonOptions.Create();

        // 初期ノード（必要なら）
        // TryAddSample();
    }

    // ===== File =====

    [RelayCommand]
    private void New()
    {
        Root.Clear();
        SelectedNode = null;
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

        await using var fs = File.OpenRead(dlg.FileName);
        var nodes = await CommandSerializer.LoadAsync(fs, _services, _registry, _json);
        Root.Clear();
        foreach (var n in nodes) Root.Add(n);
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

        await using var fs = File.Create(dlg.FileName);
        await CommandSerializer.SaveAsync(Root, fs, _registry, _json);
    }

    // ===== Edit =====

    [RelayCommand]
    private void Add()
    {
        // 簡易追加：最初のDescriptorでデフォルトコマンドを作成
        var desc = _registry.All.FirstOrDefault();
        if (desc is null) return;

        var cmd = desc.CreateCommand(desc.CreateDefaultSettings(), _services);
        InsertCommandAtSelection(cmd);
    }

    [RelayCommand]
    private void Remove()
    {
        if (SelectedNode is not IAutoToolCommand cmd) return;

        if (TryFindParent(Root, cmd, out var parent))
        {
            parent.Remove(cmd);
            if (ReferenceEquals(SelectedNode, cmd)) SelectedNode = null;
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
        await _runner.RunAsync(Root, _executionContext, ct);
    }

    [RelayCommand]
    private void Stop()
    {
        // Run は IAsyncRelayCommand になるので Cancel が使える
        RunCommand.Cancel();
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
    // private void TryAddSample()
    // {
    //     var desc = _registry.Get("if");
    //     var cmd = desc.CreateCommand(desc.CreateDefaultSettings(), _services);
    //     Root.Add(cmd);
    // }
}
