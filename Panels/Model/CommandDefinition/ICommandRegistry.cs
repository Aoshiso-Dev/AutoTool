using MacroPanels.Command.Interface;
using MacroPanels.Model.List.Interface;

namespace MacroPanels.Model.CommandDefinition;

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
    /// すべてのコマンドタイプ名を取得
    /// </summary>
    IEnumerable<string> GetAllTypeNames();

    /// <summary>
    /// 順序付けされたコマンドタイプ名を取得
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

    /// <summary>
    /// If系コマンドかどうか判定
    /// </summary>
    bool IsIfCommand(string typeName);

    /// <summary>
    /// ループ系コマンドかどうか判定
    /// </summary>
    bool IsLoopCommand(string typeName);

    /// <summary>
    /// 終了系コマンドかどうか判定
    /// </summary>
    bool IsEndCommand(string typeName);

    /// <summary>
    /// 開始系コマンドかどうか判定
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
/// 静的CommandRegistryをラップするアダプタ
/// </summary>
public class CommandRegistryAdapter : ICommandRegistry
{
    public void Initialize() => CommandRegistry.Initialize();

    public IEnumerable<string> GetAllTypeNames() => CommandRegistry.GetAllTypeNames();

    public IEnumerable<string> GetOrderedTypeNames() => CommandRegistry.GetOrderedTypeNames();

    public ICommandListItem? CreateCommandItem(string typeName) => CommandRegistry.CreateCommandItem(typeName);

    public bool TryCreateSimple(ICommand parent, ICommandListItem item, IServiceProvider? serviceProvider, out ICommand? command) 
        => CommandRegistry.TryCreateSimple(parent, item, serviceProvider, out command);

    public bool IsIfCommand(string typeName) => CommandRegistry.IsIfCommand(typeName);

    public bool IsLoopCommand(string typeName) => CommandRegistry.IsLoopCommand(typeName);

    public bool IsEndCommand(string typeName) => CommandRegistry.IsEndCommand(typeName);

    public bool IsStartCommand(string typeName) => CommandRegistry.IsStartCommand(typeName);

    public string GetDisplayName(string typeName, string language = "ja") 
        => CommandRegistry.DisplayOrder.GetDisplayName(typeName, language);

    public string GetCategoryName(string typeName, string language = "ja") 
        => CommandRegistry.DisplayOrder.GetCategoryName(typeName, language);
}
