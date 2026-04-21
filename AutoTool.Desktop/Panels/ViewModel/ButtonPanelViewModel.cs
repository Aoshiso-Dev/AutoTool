using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AutoTool.Automation.Runtime.Definitions;
using AutoTool.Desktop.Panels.ViewModel.Shared;
using System.Collections.ObjectModel;

namespace AutoTool.Desktop.Panels.ViewModel;

/// <summary>
/// 画面状態とユーザー操作を管理する ViewModel です。
/// </summary>
public partial class ButtonPanelViewModel : ObservableObject, IButtonPanelViewModel
{
    private readonly ICommandRegistry _commandRegistry;

    [ObservableProperty]
    private bool _isRunning;

    [ObservableProperty]
    private ObservableCollection<CommandDisplayItem> _itemTypes = [];

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
    public event Action? CopyRequested;
    public event Action? PasteRequested;

    public ButtonPanelViewModel(ICommandRegistry commandRegistry)
    {
        ArgumentNullException.ThrowIfNull(commandRegistry);
        _commandRegistry = commandRegistry;
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
                Category = _commandRegistry.GetCategoryName(typeName),
                Description = CommandDescriptionCatalog.GetDescription(typeName)
            })
            .ToList();
        
        ItemTypes = new ObservableCollection<CommandDisplayItem>(displayItems);
        SelectedItemType = ItemTypes.FirstOrDefault();
    }

    [RelayCommand(AllowConcurrentExecutions = true)]
    public async Task Run()
    {
        if (IsRunning)
        {
            StopRequested?.Invoke();
            return;
        }

        if (RunRequested is null)
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
                System.Diagnostics.Debug.WriteLine($"RunRequested ハンドラーで例外が発生しました: {ex}");
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
        if (SelectedItemType is not null)
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

    [RelayCommand]
    public void Copy() => CopyRequested?.Invoke();

    [RelayCommand]
    public void Paste() => PasteRequested?.Invoke();

    public void SetRunningState(bool isRunning) => IsRunning = isRunning;
    public void RequestStop() => StopRequested?.Invoke();

    public void Prepare() { }
}

