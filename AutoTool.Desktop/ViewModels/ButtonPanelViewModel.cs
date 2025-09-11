using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using AutoTool.Core.Descriptors;
using Microsoft.Extensions.Logging;

namespace AutoTool.Desktop.ViewModels;

public partial class ButtonPanelViewModel : ObservableObject
{
    private readonly ICommandRegistry _registry;
    private readonly ILogger<ButtonPanelViewModel> _logger;
    private readonly IServiceProvider _serviceProvider;

    [ObservableProperty]
    private string _searchText = string.Empty;
    
    [ObservableProperty]
    private ObservableCollection<CommandDescriptorItem> _availableCommands = new();
    
    [ObservableProperty]
    private ObservableCollection<CommandDescriptorItem> _filteredCommands = new();
    
    [ObservableProperty]
    private CommandDescriptorItem? _selectedCommand;

    public ButtonPanelViewModel(
        ICommandRegistry registry, 
        ILogger<ButtonPanelViewModel> logger,
        IServiceProvider serviceProvider)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        
        LoadAvailableCommands();
        FilterCommands();
    }

    private void LoadAvailableCommands()
    {
        try
        {
            var descriptors = _registry.All.Select(d => new CommandDescriptorItem
            {
                Descriptor = d,
                Type = d.Type,
                DisplayName = d.DisplayName,
                IconKey = d.IconKey
            }).OrderBy(x => x.DisplayName).ToList();
            
            AvailableCommands = new ObservableCollection<CommandDescriptorItem>(descriptors);
            
            _logger.LogInformation("利用可能なコマンドを読み込みました: {Count}個", descriptors.Count);
            
            // デフォルト選択
            SelectedCommand = AvailableCommands.FirstOrDefault();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "利用可能なコマンドの読み込み中にエラーが発生しました");
        }
    }
    
    partial void OnSearchTextChanged(string value)
    {
        FilterCommands();
    }
    
    private void FilterCommands()
    {
        try
        {
            var filtered = AvailableCommands.AsEnumerable();
            
            if (!string.IsNullOrEmpty(SearchText))
            {
                var searchLower = SearchText.ToLower();
                filtered = filtered.Where(cmd => 
                    cmd.DisplayName.ToLower().Contains(searchLower) ||
                    cmd.Type.ToLower().Contains(searchLower));
            }
            
            FilteredCommands = new ObservableCollection<CommandDescriptorItem>(filtered);
            
            // 検索結果の最初のアイテムを選択
            if (FilteredCommands.Count > 0 && (SelectedCommand == null || !FilteredCommands.Contains(SelectedCommand)))
            {
                SelectedCommand = FilteredCommands.First();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "コマンドフィルタリング中にエラーが発生しました");
        }
    }

    // File operations - メッセージング経由でMainViewModelに通知
    [RelayCommand]
    private void New()
    {
        try
        {
            WeakReferenceMessenger.Default.Send(new NewFileMessage());
            _logger.LogInformation("New file message sent");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "New file command error");
        }
    }

    [RelayCommand]
    private void Load()
    {
        try
        {
            WeakReferenceMessenger.Default.Send(new LoadFileMessage());
            _logger.LogInformation("Load file message sent");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Load file command error");
        }
    }

    [RelayCommand]
    private void Save()
    {
        try
        {
            WeakReferenceMessenger.Default.Send(new SaveFileMessage());
            _logger.LogInformation("Save file message sent");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Save file command error");
        }
    }

    // Edit operations
    [RelayCommand]
    private void Add()
    {
        try
        {
            if (SelectedCommand?.Descriptor != null)
            {
                var settings = SelectedCommand.Descriptor.CreateDefaultSettings();
                var cmd = SelectedCommand.Descriptor.CreateCommand(settings, _serviceProvider);
                
                WeakReferenceMessenger.Default.Send(new AddCommandMessage(cmd));
                
                _logger.LogInformation("コマンドを追加しました: {CommandType}", SelectedCommand.Type);
            }
            else
            {
                _logger.LogWarning("コマンドが選択されていません");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "コマンド追加中にエラーが発生しました");
        }
    }

    [RelayCommand]
    private void AddSpecificCommand(string commandType)
    {
        try
        {
            if (_registry.TryGet(commandType, out var descriptor) && descriptor != null)
            {
                var settings = descriptor.CreateDefaultSettings();
                var cmd = descriptor.CreateCommand(settings, _serviceProvider);
                
                WeakReferenceMessenger.Default.Send(new AddCommandMessage(cmd));
                
                _logger.LogInformation("特定のコマンドを追加しました: {CommandType}", commandType);
            }
            else
            {
                _logger.LogWarning("指定されたコマンドタイプが見つかりません: {CommandType}", commandType);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "特定コマンド追加中にエラーが発生しました: {CommandType}", commandType);
        }
    }

    [RelayCommand]
    private void Remove()
    {
        try
        {
            WeakReferenceMessenger.Default.Send(new RemoveCommandMessage());
            _logger.LogInformation("Remove command message sent");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Remove command error");
        }
    }

    [RelayCommand]
    private void MoveUp()
    {
        try
        {
            WeakReferenceMessenger.Default.Send(new MoveUpCommandMessage());
            _logger.LogInformation("Move up command message sent");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Move up command error");
        }
    }

    [RelayCommand]
    private void MoveDown()
    {
        try
        {
            WeakReferenceMessenger.Default.Send(new MoveDownCommandMessage());
            _logger.LogInformation("Move down command message sent");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Move down command error");
        }
    }

    // Run operations
    [RelayCommand]
    private void Run()
    {
        try
        {
            WeakReferenceMessenger.Default.Send(new RunMacroMessage());
            _logger.LogInformation("Run macro message sent");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Run macro command error");
        }
    }

    [RelayCommand]
    private void Stop()
    {
        try
        {
            WeakReferenceMessenger.Default.Send(new StopMacroMessage());
            _logger.LogInformation("Stop macro message sent");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Stop macro command error");
        }
    }
}

/// <summary>
/// コマンドDescriptorの表示用アイテム
/// </summary>
public class CommandDescriptorItem
{
    public ICommandDescriptor Descriptor { get; set; } = null!;
    public string Type { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? IconKey { get; set; }
}

// Message classes for communication
public record NewFileMessage;
public record LoadFileMessage;
public record SaveFileMessage;
public record AddCommandMessage(object Command);
public record RemoveCommandMessage;
public record MoveUpCommandMessage;
public record MoveDownCommandMessage;
public record RunMacroMessage;
public record StopMacroMessage;