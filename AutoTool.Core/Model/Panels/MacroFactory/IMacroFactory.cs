using AutoTool.Commands.Interface;
using AutoTool.Panels.Model.List.Interface;

namespace AutoTool.Panels.Model.MacroFactory;

/// <summary>
/// マクロ生成のインターフェース
/// </summary>
public interface IMacroFactory
{
    /// <summary>
    /// コマンドアイテム列から実行可能なマクロを生成する
    /// </summary>
    /// <param name="items">コマンドアイテム列</param>
    /// <returns>実行可能なルートコマンド</returns>
    ICommand CreateMacro(IEnumerable<ICommandListItem> items);
}

