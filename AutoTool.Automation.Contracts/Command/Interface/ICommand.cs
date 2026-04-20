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

/// <summary>
/// コマンドツリーの最上位ノードであることを示すマーカー契約です。
/// </summary>
public interface IRootCommand : ICommand { }
/// <summary>
/// 条件分岐ブロックの開始コマンドであることを示すマーカー契約です。
/// </summary>
public interface IIfCommand : ICommand { }
/// <summary>
/// 画像出現待機コマンドの契約を定義し、画像待機専用の設定値へ型安全にアクセスできるようにします。
/// </summary>
public interface IWaitImageCommand : ICommand { new IWaitImageCommandSettings Settings { get; } }
/// <summary>
/// 画像検索コマンドの契約を定義し、検索専用設定を通じて検出条件を受け取れるようにします。
/// </summary>
public interface IFindImageCommand : ICommand { new IFindImageCommandSettings Settings { get; } }
/// <summary>
/// Find Text コマンドの実行契約を定義し、実装差し替え時も同じ呼び出し方で利用できるようにします。
/// </summary>
public interface IFindTextCommand : ICommand { new IFindTextCommandSettings Settings { get; } }
/// <summary>
/// Click Image コマンドの実行契約を定義し、実装差し替え時も同じ呼び出し方で利用できるようにします。
/// </summary>
public interface IClickImageCommand : ICommand { new IClickImageCommandSettings Settings { get; } }
/// <summary>
/// Hotkey コマンドの実行契約を定義し、実装差し替え時も同じ呼び出し方で利用できるようにします。
/// </summary>
public interface IHotkeyCommand : ICommand { new IHotkeyCommandSettings Settings { get; } }
/// <summary>
/// Click コマンドの実行契約を定義し、実装差し替え時も同じ呼び出し方で利用できるようにします。
/// </summary>
public interface IClickCommand : ICommand { new IClickCommandSettings Settings { get; } }
/// <summary>
/// Wait コマンドの実行契約を定義し、実装差し替え時も同じ呼び出し方で利用できるようにします。
/// </summary>
public interface IWaitCommand : ICommand { new IWaitCommandSettings Settings { get; } }
/// <summary>
/// 条件判定コマンドの契約を定義し、判定に応じた分岐処理を共通の形で扱えるようにします。
/// </summary>
public interface IIfImageExistCommand : ICommand, IIfCommand { new IIfImageCommandSettings Settings { get; } }
/// <summary>
/// 条件判定コマンドの契約を定義し、判定に応じた分岐処理を共通の形で扱えるようにします。
/// </summary>
public interface IIfImageNotExistCommand : ICommand, IIfCommand { new IIfImageCommandSettings Settings { get; } }
/// <summary>
/// 条件判定コマンドの契約を定義し、判定に応じた分岐処理を共通の形で扱えるようにします。
/// </summary>
public interface IIfTextExistCommand : ICommand, IIfCommand { new IIfTextCommandSettings Settings { get; } }
/// <summary>
/// 条件判定コマンドの契約を定義し、判定に応じた分岐処理を共通の形で扱えるようにします。
/// </summary>
public interface IIfTextNotExistCommand : ICommand, IIfCommand { new IIfTextCommandSettings Settings { get; } }
/// <summary>
/// ループ開始コマンドの契約を定義し、繰り返し回数や範囲設定を共通で扱えるようにします。
/// </summary>
public interface ILoopCommand : ICommand { new ILoopCommandSettings Settings { get; } }
/// <summary>
/// ループ終了コマンドの契約を定義し、対応する開始コマンドとの境界を明確にします。
/// </summary>
public interface IEndLoopCommand : ICommand { new ILoopEndCommandSettings Settings { get; } }
/// <summary>
/// 再試行制御コマンドの契約を定義し、失敗時のリトライ条件を共通で扱えるようにします。
/// </summary>
public interface IRetryCommand : ICommand { new IRetryCommandSettings Settings { get; } }
/// <summary>
/// ループ中断コマンドの契約を定義し、条件成立時に繰り返し処理を終了できるようにします。
/// </summary>
public interface ILoopBreakCommand : ICommand { }
/// <summary>
/// 条件判定コマンドの契約を定義し、判定に応じた分岐処理を共通の形で扱えるようにします。
/// </summary>
public interface IIfImageExistAICommand : ICommand, IIfCommand { new IIfImageExistAISettings Settings { get; } }
/// <summary>
/// 条件判定コマンドの契約を定義し、判定に応じた分岐処理を共通の形で扱えるようにします。
/// </summary>
public interface IIfImageNotExistAICommand : ICommand, IIfCommand { new IIfImageNotExistAISettings Settings { get; } }
/// <summary>
/// Click Image AI コマンドの実行契約を定義し、実装差し替え時も同じ呼び出し方で利用できるようにします。
/// </summary>
public interface IClickImageAICommand : ICommand { new IClickImageAICommandSettings Settings { get; } }
/// <summary>
/// Execute コマンドの実行契約を定義し、実装差し替え時も同じ呼び出し方で利用できるようにします。
/// </summary>
public interface IExecuteCommand : ICommand { new IExecuteCommandSettings Settings { get; } }
/// <summary>
/// Set Variable コマンドの実行契約を定義し、実装差し替え時も同じ呼び出し方で利用できるようにします。
/// </summary>
public interface ISetVariableCommand : ICommand { new ISetVariableCommandSettings Settings { get; } }
/// <summary>
/// Set Variable AI コマンドの実行契約を定義し、実装差し替え時も同じ呼び出し方で利用できるようにします。
/// </summary>
public interface ISetVariableAICommand : ICommand { new ISetVariableAICommandSettings Settings { get; } }
/// <summary>
/// Set Variable OCR コマンドの実行契約を定義し、実装差し替え時も同じ呼び出し方で利用できるようにします。
/// </summary>
public interface ISetVariableOCRCommand : ICommand { new ISetVariableOCRCommandSettings Settings { get; } }
/// <summary>
/// 条件判定コマンドの契約を定義し、判定に応じた分岐処理を共通の形で扱えるようにします。
/// </summary>
public interface IIfVariableCommand : ICommand, IIfCommand { new IIfVariableCommandSettings Settings { get; } }
/// <summary>
/// Screenshot コマンドの実行契約を定義し、実装差し替え時も同じ呼び出し方で利用できるようにします。
/// </summary>
public interface IScreenshotCommand : ICommand { new IScreenshotCommandSettings Settings { get; } }
