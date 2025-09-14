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
using CommunityToolkit.Mvvm.Messaging;
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
    private CancellationTokenSource _cts = new();
    private readonly ICommandRunner _runner;
    private readonly ICommandRegistry _registry;
    private readonly IServiceProvider _services;
    private readonly ILogger<MainViewModel> _logger;
    private readonly JsonSerializerOptions _json;

    public MainViewModel(
        ICommandRunner runner,
        ICommandRegistry registry,
        IServiceProvider services,
        ILogger<MainViewModel> logger)
    {
        _runner = runner;
        _registry = registry;
        _services = services;
        _logger = logger;
        _json = JsonOptions.Create();

        _logger.LogInformation("MainViewModel を初期化しています");

        RegisterMessages();
    }

    private void RegisterMessages()
    {
        // ButtonPanelからのメッセージを登録
        WeakReferenceMessenger.Default.Register<RunMacroMessage>(this, async (r, m) => await Run(_cts.Token));
        WeakReferenceMessenger.Default.Register<StopMacroMessage>(this, (r, m) => Stop());
    }

    private async Task Run(CancellationToken ct)
    {
        try
        {
            var root = WeakReferenceMessenger.Default.Send<GetRootMacroMessaage>().Root;

            if (root == null || !root.Any())
            {
                _logger.LogWarning("マクロが空です。実行を中止します");
                return;
            }

            _logger.LogInformation("マクロ実行を開始します: {Count} コマンド", root?.Count);
            WeakReferenceMessenger.Default.Send<StartMacroMessage>();
            await _runner.RunAsync(root!, ct);
            _logger.LogInformation("マクロ実行が完了しました");
            
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "マクロ実行中にエラーが発生しました");
        }
        finally
        {
            WeakReferenceMessenger.Default.Send<FinishMacroMessage>();
        }
    }

    private void Stop()
    {
        _cts.Cancel();
        _logger.LogInformation("マクロ実行を停止しました");
    }
}
