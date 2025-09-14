using System.ComponentModel;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using AutoTool.Core.Commands;
using Microsoft.Extensions.Logging;
using System.Reflection;
using System.Text.Json;
using AutoTool.Core.Abstractions;

namespace AutoTool.Desktop.ViewModels;

public partial class EditPanelViewModel : ObservableObject
{
    private readonly ILogger<EditPanelViewModel> _logger;
    private readonly IServiceProvider _serviceProvider;

    // 現在選択中のコマンドを保持（Apply 時に使う)
    private IAutoToolCommand? _currentCommand;

    [ObservableProperty]
    private object? _selectedEditor;

    [ObservableProperty]
    private string _selectedCommandType = string.Empty;

    [ObservableProperty]
    private string _selectedCommandDisplayName = string.Empty;

    [ObservableProperty]
    private bool _hasSelection = false;

    [ObservableProperty]
    private string _statusMessage = "コマンドを選択してください";

    public EditPanelViewModel(ILogger<EditPanelViewModel> logger, IServiceProvider serviceProvider)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

        // メッセージ登録
        WeakReferenceMessenger.Default.Register<SelectNodeMessage>(this, (r, m) => OnSelectNodeMessage(m));

        _logger.LogInformation("EditPanelViewModel が初期化されました");
    }

    public void OnSelectNodeMessage(SelectNodeMessage message)
    {
        try
        {
            _logger.LogDebug("SelectNodeMessage受信: {Node}", message.Node?.GetType().Name ?? "null");

            if (message.Node is IAutoToolCommand command)
            {
                SetSelectedCommand(command);
                _logger.LogInformation("EditPanelでコマンドを選択: {CommandType}", command.Type);
            }
            else
            {
                ClearSelection();
                _logger.LogDebug("EditPanel選択解除");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SelectNodeMessage処理中にエラー");
            StatusMessage = $"エラー: {ex.Message}";
        }
    }

    private void SetSelectedCommand(IAutoToolCommand command)
    {
        try
        {
            _currentCommand = command;

            SelectedCommandType = command.Type;

            SelectedCommandDisplayName = command.DisplayName;
            HasSelection = true;
            StatusMessage = $"編集中: {command.DisplayName}";

            // Settings プロパティをリフレクションで探す
            var settingsProp = command.GetType().GetProperty("Settings", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (settingsProp != null)
            {
                var settingsValue = settingsProp.GetValue(command);
                if (settingsValue != null)
                {
                    SelectedEditor = settingsValue;
                    _logger.LogInformation("EditPanelでSettingsを直接表示: {CommandType}", command.Type);
                    return;
                }
            }

            _logger.LogInformation("Settings が見つからず編集不可: {CommandType}", command.Type);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "コマンド設定表示中にエラー: {CommandType}", command.Type);
            StatusMessage = $"設定表示エラー: {ex.Message}";
        }
    }

    private void ClearSelection()
    {
        SelectedEditor = null;
        SelectedCommandType = string.Empty;
        SelectedCommandDisplayName = string.Empty;
        HasSelection = false;
        StatusMessage = "コマンドを選択してください";
        _currentCommand = null;
    }

    [RelayCommand]
    private void Apply()
    {
        try
        {
            if (SelectedEditor is IAutoToolCommandSettings settings && _currentCommand != null)
            {
                // 現在のコマンドの Settings 型を取得
                var targetSettingsType = _currentCommand.GetType().GetProperty("Settings", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.PropertyType;
                if (targetSettingsType == null)
                {
                    StatusMessage = "コマンドに Settings プロパティが見つかりません";
                    return;
                }

                // SelectedEditor の型が targetSettingsType と一致するか確認
                if (!targetSettingsType.IsInstanceOfType(SelectedEditor))
                {
                    StatusMessage = "選択された設定の型がコマンドの設定型と一致しません";
                    return;
                }

                // Settings プロパティに新しい設定を適用
                var settingsProp = _currentCommand.GetType().GetProperty("Settings", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (settingsProp == null || !settingsProp.CanWrite)
                {
                    StatusMessage = "コマンドの Settings プロパティが見つからないか、書き込み不可です";
                    return;
                }
                settingsProp.SetValue(_currentCommand, SelectedEditor);

                StatusMessage = "設定を適用しました";
                _logger.LogInformation("コマンドSettingsを適用: {CommandType}", _currentCommand.Type);

                WeakReferenceMessenger.Default.Send(new CommandUpdatedMessage(_currentCommand));
                return;
            }

            StatusMessage = "適用対象が見つかりません";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "設定適用中にエラー");
            StatusMessage = $"適用エラー: {ex.Message}";
        }
    }

    [RelayCommand]
    private void Revert()
    {
        try
        {
            if (SelectedEditor is IAutoToolCommandSettings && _currentCommand != null)
            {
                // 現在のコマンドの Settings プロパティをリフレクションで取得
                var settingsProp = _currentCommand.GetType().GetProperty("Settings", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (settingsProp == null)
                {
                    StatusMessage = "コマンドに Settings プロパティが見つかりません";
                    return;
                }

                var currentSettings = settingsProp.GetValue(_currentCommand);
                if (currentSettings == null)
                {
                    StatusMessage = "コマンドの Settings プロパティが null です";
                    return;
                }

                // 現在の Settings を JSON 経由でディープコピーして SelectedEditor に設定
                var json = JsonSerializer.Serialize(currentSettings, currentSettings.GetType());
                var clonedSettings = JsonSerializer.Deserialize(json, currentSettings.GetType());
                if (clonedSettings == null)
                {
                    StatusMessage = "設定の復元に失敗しました";
                    return;
                }
                SelectedEditor = clonedSettings;

                StatusMessage = "設定を元に戻しました";
                _logger.LogInformation("Settings編集を元に戻し: {CommandType}", _currentCommand.Type);
                return;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "設定復元中にエラー");
            StatusMessage = $"復元エラー: {ex.Message}";
        }
    }
}

// メッセージクラス
public record SelectNodeMessage(object? Node);
public record CommandUpdatedMessage(IAutoToolCommand Command);