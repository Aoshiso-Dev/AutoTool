using AutoTool.Application.Ports;
using AutoTool.Desktop.Services;
using AutoTool.Plugin.Host.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Wpf.Ui.Controls;

namespace AutoTool.Desktop.ViewModel;

public partial class PluginQuickActionViewModel : ObservableObject
{
    private const SymbolRegular DefaultIcon = SymbolRegular.BoxToolbox24;
    private readonly PluginQuickActionDescriptor _descriptor;
    private readonly PluginQuickActionExecutor _executor;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ExecuteCommand))]
    [NotifyPropertyChangedFor(nameof(IsEnabled))]
    [NotifyPropertyChangedFor(nameof(EffectiveToolTip))]
    private bool _isHostRunning;

    private PluginQuickActionViewModel(
        PluginQuickActionDescriptor descriptor,
        PluginQuickActionExecutor executor,
        SymbolRegular iconSymbol)
    {
        _descriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
        _executor = executor ?? throw new ArgumentNullException(nameof(executor));
        IconSymbol = iconSymbol;
    }

    public string PluginId => _descriptor.PluginId;
    public string ActionId => _descriptor.ActionId;
    public string DisplayName => _descriptor.DisplayName;
    public string CommandType => _descriptor.CommandType;
    public SymbolRegular IconSymbol { get; }
    public bool IsAvailable => _descriptor.IsAvailable;
    public bool IsEnabled => IsAvailable && !IsHostRunning;

    public string EffectiveToolTip => (IsHostRunning, IsAvailable) switch
    {
        (true, _) => "実行中はプラグイン拡張を実行できません。",
        (_, false) => _descriptor.UnavailableReason ?? "この quick action は利用できません。",
        _ => string.IsNullOrWhiteSpace(_descriptor.ToolTip) ? DisplayName : _descriptor.ToolTip,
    };

    public static PluginQuickActionViewModel Create(
        PluginQuickActionDescriptor descriptor,
        PluginQuickActionExecutor executor,
        ILogWriter logWriter)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentNullException.ThrowIfNull(executor);
        ArgumentNullException.ThrowIfNull(logWriter);

        var iconSymbol = ResolveIcon(descriptor, logWriter);
        return new PluginQuickActionViewModel(descriptor, executor, iconSymbol);
    }

    [RelayCommand(CanExecute = nameof(CanExecute))]
    private async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        await _executor.ExecuteAsync(_descriptor, cancellationToken);
    }

    private bool CanExecute() => IsEnabled;

    private static SymbolRegular ResolveIcon(PluginQuickActionDescriptor descriptor, ILogWriter logWriter)
    {
        if (string.IsNullOrWhiteSpace(descriptor.Icon))
        {
            return DefaultIcon;
        }

        if (Enum.TryParse<SymbolRegular>(descriptor.Icon, ignoreCase: false, out var symbol))
        {
            return symbol;
        }

        logWriter.WriteStructured(
            "Plugin",
            "QuickActionIconFallback",
            new Dictionary<string, object?>
            {
                ["Message"] = $"QuickAction の icon 指定が不正なため既定アイコンを使用します: {descriptor.Icon}",
                ["PluginId"] = descriptor.PluginId,
                ["ActionId"] = descriptor.ActionId,
                ["Icon"] = descriptor.Icon,
            });

        return DefaultIcon;
    }
}
