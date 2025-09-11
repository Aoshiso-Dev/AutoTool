using System.ComponentModel;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using AutoTool.Core.Commands;
using Microsoft.Extensions.Logging;

namespace AutoTool.Desktop.ViewModels;

public partial class EditPanelViewModel : ObservableObject, IRecipient<SelectNodeMessage>
{
    private readonly ILogger<EditPanelViewModel> _logger;
    private readonly IServiceProvider _serviceProvider;

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
        WeakReferenceMessenger.Default.Register<SelectNodeMessage>(this);

        _logger.LogInformation("EditPanelViewModel が初期化されました");
    }

    public void Receive(SelectNodeMessage message)
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
            // コマンドタイプに応じて適切なEditorを作成
            SelectedEditor = CreateCommandEditor(command);
            SelectedCommandType = command.Type;
            SelectedCommandDisplayName = command.DisplayName;
            HasSelection = true;
            StatusMessage = $"編集中: {command.DisplayName}";

            _logger.LogInformation("EditPanelでコマンド設定を表示: {CommandType}", command.Type);
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
    }

    private object CreateCommandEditor(IAutoToolCommand command)
    {
        return command.Type switch
        {
            "wait" => new WaitCommandEditor(command),
            "click" => new ClickCommandEditor(command),
            "if" => new IfCommandEditor(command),
            "while" => new WhileCommandEditor(command),
            _ => new BasicCommandEditor(command) // 基本的なEditor
        };
    }

    [RelayCommand]
    private void Apply()
    {
        try
        {
            if (SelectedEditor is BasicCommandEditor editor)
            {
                // 編集内容をコマンドに適用
                editor.ApplyToCommand();
                
                StatusMessage = "設定を適用しました";
                _logger.LogInformation("コマンド設定を適用: {CommandType}", editor.Type);

                // 変更を通知
                if (editor.Command != null)
                {
                    WeakReferenceMessenger.Default.Send(new CommandUpdatedMessage(editor.Command));
                }
            }
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
            if (SelectedEditor is BasicCommandEditor editor && editor.Command != null)
            {
                // 元の値に戻す（コマンドから再読み込み）
                var originalEditor = CreateCommandEditor(editor.Command);
                SelectedEditor = originalEditor;
                
                StatusMessage = "設定を元に戻しました";
                _logger.LogInformation("コマンド設定を元に戻し: {CommandType}", editor.Type);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "設定復元中にエラー");
            StatusMessage = $"復元エラー: {ex.Message}";
        }
    }
}

/// <summary>
/// 基本的なPropertyGridで編集するためのコマンド設定クラス
/// </summary>
public class BasicCommandEditor : INotifyPropertyChanged
{
    private bool _isEnabled = true;
    private string _displayName = string.Empty;
    private string _description = string.Empty;

    public IAutoToolCommand? Command { get; set; }

    [Category("基本")]
    [DisplayName("コマンドタイプ")]
    [Description("コマンドの種類")]
    [ReadOnly(true)]
    public string Type { get; set; } = string.Empty;

    [Category("基本")]
    [DisplayName("表示名")]
    [Description("コマンドの表示名")]
    public string DisplayName
    {
        get => _displayName;
        set
        {
            if (_displayName != value)
            {
                _displayName = value;
                OnPropertyChanged();
            }
        }
    }

    [Category("基本")]
    [DisplayName("有効")]
    [Description("コマンドが有効かどうか")]
    public bool IsEnabled
    {
        get => _isEnabled;
        set
        {
            if (_isEnabled != value)
            {
                _isEnabled = value;
                OnPropertyChanged();
            }
        }
    }

    [Category("詳細")]
    [DisplayName("説明")]
    [Description("コマンドの説明")]
    public string Description
    {
        get => _description;
        set
        {
            if (_description != value)
            {
                _description = value ?? string.Empty;
                OnPropertyChanged();
            }
        }
    }

    public BasicCommandEditor(IAutoToolCommand command)
    {
        Command = command;
        Type = command.Type;
        DisplayName = command.DisplayName;
        IsEnabled = command.IsEnabled;
        Description = $"{command.DisplayName} コマンドの設定";
    }

    public virtual void ApplyToCommand()
    {
        if (Command != null)
        {
            Command.IsEnabled = IsEnabled;
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

/// <summary>
/// Waitコマンド専用のEditor
/// </summary>
public class WaitCommandEditor : BasicCommandEditor
{
    private int _durationMs = 1000;

    [Category("待機設定")]
    [DisplayName("待機時間(ms)")]
    [Description("待機する時間（ミリ秒）")]
    public int DurationMs
    {
        get => _durationMs;
        set
        {
            if (_durationMs != value)
            {
                _durationMs = Math.Max(0, value);
                OnPropertyChanged();
            }
        }
    }

    public WaitCommandEditor(IAutoToolCommand command) : base(command)
    {
        // Waitコマンドの設定を読み込み
        // 実際の実装ではSettingsプロパティから値を取得
        DurationMs = 1000; // デフォルト値
    }

    public override void ApplyToCommand()
    {
        base.ApplyToCommand();
        // Waitコマンドの設定を適用
        // 実際の実装ではSettingsプロパティに値を設定
    }
}

/// <summary>
/// Clickコマンド専用のEditor
/// </summary>
public class ClickCommandEditor : BasicCommandEditor
{
    private int _x = 0;
    private int _y = 0;
    private string _windowTitle = string.Empty;

    [Category("クリック設定")]
    [DisplayName("X座標")]
    [Description("クリックするX座標")]
    public int X
    {
        get => _x;
        set
        {
            if (_x != value)
            {
                _x = Math.Max(0, value);
                OnPropertyChanged();
            }
        }
    }

    [Category("クリック設定")]
    [DisplayName("Y座標")]
    [Description("クリックするY座標")]
    public int Y
    {
        get => _y;
        set
        {
            if (_y != value)
            {
                _y = Math.Max(0, value);
                OnPropertyChanged();
            }
        }
    }

    [Category("ウィンドウ設定")]
    [DisplayName("ウィンドウタイトル")]
    [Description("対象ウィンドウのタイトル（オプション）")]
    public string WindowTitle
    {
        get => _windowTitle;
        set
        {
            if (_windowTitle != value)
            {
                _windowTitle = value ?? string.Empty;
                OnPropertyChanged();
            }
        }
    }

    public ClickCommandEditor(IAutoToolCommand command) : base(command)
    {
        // Clickコマンドの設定を読み込み
        X = 0;
        Y = 0;
        WindowTitle = string.Empty;
    }

    public override void ApplyToCommand()
    {
        base.ApplyToCommand();
        // Clickコマンドの設定を適用
    }
}

/// <summary>
/// Ifコマンド専用のEditor（タイムアウト設定あり）
/// </summary>
public class IfCommandEditor : BasicCommandEditor
{
    private int _timeout = 5000;
    private string _imagePath = string.Empty;

    [Category("条件設定")]
    [DisplayName("タイムアウト(ms)")]
    [Description("条件判定のタイムアウト時間（ミリ秒）")]
    public int Timeout
    {
        get => _timeout;
        set
        {
            if (_timeout != value)
            {
                _timeout = Math.Max(100, value);
                OnPropertyChanged();
            }
        }
    }

    [Category("条件設定")]
    [DisplayName("画像パス")]
    [Description("検索する画像ファイルのパス")]
    public string ImagePath
    {
        get => _imagePath;
        set
        {
            if (_imagePath != value)
            {
                _imagePath = value ?? string.Empty;
                OnPropertyChanged();
            }
        }
    }

    public IfCommandEditor(IAutoToolCommand command) : base(command)
    {
        Timeout = 5000;
        ImagePath = string.Empty;
    }

    public override void ApplyToCommand()
    {
        base.ApplyToCommand();
        // Ifコマンドの設定を適用
    }
}

/// <summary>
/// Whileコマンド専用のEditor
/// </summary>
public class WhileCommandEditor : BasicCommandEditor
{
    private string _conditionExpr = "true";
    private int _maxIterations = 10000;

    [Category("ループ設定")]
    [DisplayName("条件式")]
    [Description("ループの継続条件")]
    public string ConditionExpr
    {
        get => _conditionExpr;
        set
        {
            if (_conditionExpr != value)
            {
                _conditionExpr = value ?? "true";
                OnPropertyChanged();
            }
        }
    }

    [Category("ループ設定")]
    [DisplayName("最大反復回数")]
    [Description("無限ループを防ぐための最大反復回数")]
    public int MaxIterations
    {
        get => _maxIterations;
        set
        {
            if (_maxIterations != value)
            {
                _maxIterations = Math.Max(1, value);
                OnPropertyChanged();
            }
        }
    }

    public WhileCommandEditor(IAutoToolCommand command) : base(command)
    {
        ConditionExpr = "true";
        MaxIterations = 10000;
    }

    public override void ApplyToCommand()
    {
        base.ApplyToCommand();
        // Whileコマンドの設定を適用
    }
}

// メッセージクラス
public record SelectNodeMessage(object? Node);
public record CommandUpdatedMessage(IAutoToolCommand Command);