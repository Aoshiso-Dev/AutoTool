namespace AutoTool.Commands.Interface;

/// <summary>
/// 実行可能コマンドの共通契約です。
/// </summary>
public interface ICommand
{
    int LineNumber { get; set; }
    bool IsEnabled { get; set; }
    ICommand? Parent { get; set; }
    IEnumerable<ICommand> Children { get; set; }
    int NestLevel { get; set; }
    ICommandSettings Settings { get; }
    EventHandler? OnStartCommand { get; set; }
    EventHandler? OnFinishCommand { get; set; }

    Task<bool> Execute(CancellationToken cancellationToken);
    bool CanExecute();
}

/// <summary>ルートコマンドを表すマーカー契約です。</summary>
public interface IRootCommand : ICommand { }
/// <summary>条件分岐コマンドを表すマーカー契約です。</summary>
public interface IIfCommand : ICommand { }
/// <summary>画像待機コマンドの契約です。</summary>
public interface IWaitImageCommand : ICommand { new IWaitImageCommandSettings Settings { get; } }
/// <summary>画像検索コマンドの契約です。</summary>
public interface IFindImageCommand : ICommand { new IFindImageCommandSettings Settings { get; } }
public interface IFindTextCommand : ICommand { new IFindTextCommandSettings Settings { get; } }
public interface IClickImageCommand : ICommand { new IClickImageCommandSettings Settings { get; } }
public interface IHotkeyCommand : ICommand { new IHotkeyCommandSettings Settings { get; } }
public interface IClickCommand : ICommand { new IClickCommandSettings Settings { get; } }
public interface IWaitCommand : ICommand { new IWaitCommandSettings Settings { get; } }
public interface IIfImageExistCommand : ICommand, IIfCommand { new IIfImageCommandSettings Settings { get; } }
public interface IIfImageNotExistCommand : ICommand, IIfCommand { new IIfImageCommandSettings Settings { get; } }
public interface IIfTextExistCommand : ICommand, IIfCommand { new IIfTextCommandSettings Settings { get; } }
public interface IIfTextNotExistCommand : ICommand, IIfCommand { new IIfTextCommandSettings Settings { get; } }
public interface ILoopCommand : ICommand { new ILoopCommandSettings Settings { get; } }
public interface IEndLoopCommand : ICommand { new ILoopEndCommandSettings Settings { get; } }
public interface IRetryCommand : ICommand { new IRetryCommandSettings Settings { get; } }
public interface ILoopBreakCommand : ICommand { }
public interface IIfImageExistAICommand : ICommand, IIfCommand { new IIfImageExistAISettings Settings { get; } }
public interface IIfImageNotExistAICommand : ICommand, IIfCommand { new IIfImageNotExistAISettings Settings { get; } }
public interface IClickImageAICommand : ICommand { new IClickImageAICommandSettings Settings { get; } }
public interface IExecuteCommand : ICommand { new IExecuteCommandSettings Settings { get; } }
public interface ISetVariableCommand : ICommand { new ISetVariableCommandSettings Settings { get; } }
public interface ISetVariableAICommand : ICommand { new ISetVariableAICommandSettings Settings { get; } }
public interface ISetVariableOCRCommand : ICommand { new ISetVariableOCRCommandSettings Settings { get; } }
public interface IIfVariableCommand : ICommand, IIfCommand { new IIfVariableCommandSettings Settings { get; } }
public interface IScreenshotCommand : ICommand { new IScreenshotCommandSettings Settings { get; } }
