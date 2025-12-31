using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MacroPanels.Model.CommandDefinition;
using MacroPanels.ViewModel.Shared;
using System.Collections.ObjectModel;

namespace MacroPanels.ViewModel;

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

    /// <summary>
    /// 実行/停止コマンド - 同期メソッドで即座に返す
    /// </summary>
    [RelayCommand]
    public void Run()
    {
        if (IsRunning)
        {
            // 停止要求を即座に発行
            StopRequested?.Invoke();
        }
        else
        {
            // 実行要求をfire-and-forget（awaitしない）
            // Task.Runで別スレッドから実行開始することでUIをブロックしない
            if (RunRequested != null)
            {
                Task.Run(async () =>
                {
                    try
                    {
                        await RunRequested.Invoke();
                    }
                    catch
                    {
                        // エラーはMacroPanelViewModelで処理される
                    }
                });
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