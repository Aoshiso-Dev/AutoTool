using AutoTool.Commands.Interface;
using AutoTool.Panels.Model.List.Interface;

namespace AutoTool.Panels.Model.CommandDefinition;

public enum CommandCreationFailureReason
{
    None = 0,
    MissingItemType,
    UnknownItemType,
    CommandFactoryUnavailable,
    MissingCommandBinding,
    FactoryException
}

public sealed record CommandCreationResult(
    bool Success,
    ICommand? Command,
    CommandCreationFailureReason FailureReason,
    string Message)
{
    public static CommandCreationResult Ok(ICommand command) =>
        new(true, command, CommandCreationFailureReason.None, string.Empty);

    public static CommandCreationResult Fail(CommandCreationFailureReason reason, string message) =>
        new(false, null, reason, message);
}

/// <summary>
/// コマンドレジストリのインターフェース
/// </summary>
public interface ICommandRegistry
{
    /// <summary>
    /// 初期化
    /// </summary>
    void Initialize();

    /// <summary>
    /// すべてのコマンド種別名を取得
    /// </summary>
    IEnumerable<string> GetAllTypeNames();

    /// <summary>
    /// 表示順に並んだコマンド種別名を取得
    /// </summary>
    IEnumerable<string> GetOrderedTypeNames();

    /// <summary>
    /// コマンドアイテムを作成
    /// </summary>
    ICommandListItem? CreateCommandItem(string typeName);

    /// <summary>
    /// 単純コマンドを作成
    /// </summary>
    bool TryCreateSimple(ICommand parent, ICommandListItem item, IServiceProvider? serviceProvider, out ICommand? command);
    CommandCreationResult CreateSimple(ICommand parent, ICommandListItem item, IServiceProvider? serviceProvider);

    /// <summary>
    /// If 開始コマンドか判定
    /// </summary>
    bool IsIfCommand(string typeName);

    /// <summary>
    /// Loop 開始コマンドか判定
    /// </summary>
    bool IsLoopCommand(string typeName);

    /// <summary>
    /// 終了コマンドか判定
    /// </summary>
    bool IsEndCommand(string typeName);

    /// <summary>
    /// 開始コマンドか判定
    /// </summary>
    bool IsStartCommand(string typeName);

    /// <summary>
    /// 表示名を取得
    /// </summary>
    string GetDisplayName(string typeName, string language = "ja");

    /// <summary>
    /// カテゴリ名を取得
    /// </summary>
    string GetCategoryName(string typeName, string language = "ja");
}

/// <summary>
/// コマンド定義の参照専用プロバイダー
/// </summary>
public interface ICommandDefinitionProvider
{
    string GetDisplayName(string typeName, string language = "ja");
    string GetCategoryName(string typeName, string language = "ja");
    int GetDisplayPriority(string typeName);
    Type? GetItemType(string typeName);
    bool IsIfCommand(string typeName);
    bool IsLoopCommand(string typeName);
    bool IsEndCommand(string typeName);
    bool IsStartCommand(string typeName);
}


