using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AutoTool.Panels.Model.CommandDefinition;
using AutoTool.Panels.ViewModel.Shared;
using System.Collections.ObjectModel;

namespace AutoTool.Panels.ViewModel;

public partial class ButtonPanelViewModel : ObservableObject, IButtonPanelViewModel
{
    private readonly ICommandRegistry _commandRegistry;

    [ObservableProperty]
    private bool _isRunning;

    [ObservableProperty]
    private ObservableCollection<CommandDisplayItem> _itemTypes = new();

    [ObservableProperty]
    private CommandDisplayItem? _selectedItemType;

    // イベント
    public event Func<Task>? RunRequested;
    public event Action? StopRequested;
    public event Action? SaveRequested;
    public event Action? LoadRequested;
    public event Action? ClearRequested;
    public event Action<string>? AddRequested;
    public event Action? UpRequested;
    public event Action? DownRequested;
    public event Action? DeleteRequested;

    public ButtonPanelViewModel(ICommandRegistry commandRegistry)
    {
        _commandRegistry = commandRegistry ?? throw new ArgumentNullException(nameof(commandRegistry));
        InitializeItemTypes();
    }

    private void InitializeItemTypes()
    {
        _commandRegistry.Initialize();
        
        var displayItems = _commandRegistry.GetOrderedTypeNames()
            .Select(typeName => new CommandDisplayItem
            {
                TypeName = typeName,
                DisplayName = _commandRegistry.GetDisplayName(typeName),
                Category = _commandRegistry.GetCategoryName(typeName)
            })
            .ToList();
        
        ItemTypes = new ObservableCollection<CommandDisplayItem>(displayItems);
        SelectedItemType = ItemTypes.FirstOrDefault();
    }

    [RelayCommand]
    public async Task Run()
    {
        if (IsRunning)
        {
            StopRequested?.Invoke();
            return;
        }

        if (RunRequested == null)
        {
            return;
        }

        foreach (var handler in RunRequested.GetInvocationList())
        {
            try
            {
                await ((Func<Task>)handler)();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"RunRequested handler failed: {ex}");
            }
        }
    }

    [RelayCommand]
    public void Save() => SaveRequested?.Invoke();

    [RelayCommand]
    public void Load() => LoadRequested?.Invoke();

    [RelayCommand]
    public void Clear() => ClearRequested?.Invoke();

    [RelayCommand]
    public void Add() 
    {
        if (SelectedItemType != null)
        {
            AddRequested?.Invoke(SelectedItemType.TypeName);
        }
    }

    [RelayCommand]
    public void Up() => UpRequested?.Invoke();

    [RelayCommand]
    public void Down() => DownRequested?.Invoke();

    [RelayCommand]
    public void Delete() => DeleteRequested?.Invoke();

    public void SetRunningState(bool isRunning) => IsRunning = isRunning;

    public void Prepare() { }
}

