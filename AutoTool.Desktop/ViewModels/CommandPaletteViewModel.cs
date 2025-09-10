using AutoTool.Core.Commands;
using AutoTool.Core.Descriptors;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows.Controls.Primitives;

namespace AutoTool.Desktop.ViewModels;

public partial class CommandPaletteViewModel : ObservableObject
{
    private readonly ICommandRegistry _registry;

    public IEnumerable<ICommandDescriptor> Commands => _registry.All;

    [ObservableProperty] private ICommandDescriptor? selected;
    public event Action<IAutoToolCommand>? CommandCreated;

    public CommandPaletteViewModel(ICommandRegistry registry) => _registry = registry;

    [RelayCommand]
    private void Create()
    {
        if (Selected is null) return;
        var settings = Selected.CreateDefaultSettings();
        var cmd = Selected.CreateCommand(settings, services: null!); // 実装がDI不要なら null でOK
        CommandCreated?.Invoke(cmd);
    }
}