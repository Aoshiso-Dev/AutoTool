using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MacroPanels.Model.List.Interface;
using MacroPanels.Model.List.Type;

namespace MacroPanels.Model.MacroFactory
{
    /// <summary>
    /// コマンドファクトリ用の拡張メソッド群
    /// </summary>
    public static class CommandFactoryExtensions
    {
        /// <summary>
        /// 指定した行番号範囲内の子要素を取得する
        /// </summary>
        public static IEnumerable<ICommandListItem> GetChildrenBetween(
            this IEnumerable<ICommandListItem> items,
            int startLine,
            int endLine)
        {
            return items.Where(x => x.LineNumber > startLine && x.LineNumber < endLine);
        }

        /// <summary>
        /// 指定したネストレベルの要素を取得する
        /// </summary>
        public static IEnumerable<ICommandListItem> GetByNestLevel(
            this IEnumerable<ICommandListItem> items,
            int nestLevel)
        {
            return items.Where(x => x.NestLevel == nestLevel);
        }

        /// <summary>
        /// 有効な要素のみを取得する
        /// </summary>
        public static IEnumerable<ICommandListItem> GetEnabled(
            this IEnumerable<ICommandListItem> items)
        {
            return items.Where(x => x.IsEnable);
        }

        /// <summary>
        /// ペア関係の検証
        /// </summary>
        public static void ValidatePair(this ICommandListItem item, string commandType)
        {
            if (item is IIfItem ifItem && ifItem.Pair == null)
                throw new InvalidOperationException($"{commandType} (行 {item.LineNumber}) に対応するEndIfがありません");

            if (item is ILoopItem loopItem && loopItem.Pair == null)
                throw new InvalidOperationException($"{commandType} (行 {item.LineNumber}) に対応するEndLoopがありません");
        }
    }
}