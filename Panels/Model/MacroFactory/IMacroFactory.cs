using MacroPanels.Command.Interface;
using MacroPanels.Model.List.Interface;

namespace MacroPanels.Model.MacroFactory;

/// <summary>
/// マクロファクトリのインターフェース
/// </summary>
public interface IMacroFactory
{
    /// <summary>
    /// コマンドリストアイテムからマクロを作成します
    /// </summary>
    /// <param name="items">コマンドリストアイテム</param>
    /// <returns>実行可能なマクロコマンド</returns>
    ICommand CreateMacro(IEnumerable<ICommandListItem> items);
}
