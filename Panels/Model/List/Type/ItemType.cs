using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MacroPanels.Model.CommandDefinition;

namespace MacroPanels.Model.List.Type
{
    /// <summary>
    /// コマンドタイプの定義。CommandRegistry から自動生成される
    /// </summary>
    public static class ItemType
    {
        // 後方互換性のために静的定数を残しておく（廃止予定）
        [Obsolete("Use CommandRegistry.GetAllTypeNames() instead")]
        public static readonly string Click = "Click";
        [Obsolete("Use CommandRegistry.GetAllTypeNames() instead")]
        public static readonly string Click_Image = "Click_Image";
        [Obsolete("Use CommandRegistry.GetAllTypeNames() instead")]
        public static readonly string Click_Image_AI = "Click_Image_AI";
        [Obsolete("Use CommandRegistry.GetAllTypeNames() instead")]
        public static readonly string Hotkey = "Hotkey";
        [Obsolete("Use CommandRegistry.GetAllTypeNames() instead")]
        public static readonly string Wait = "Wait";
        [Obsolete("Use CommandRegistry.GetAllTypeNames() instead")]
        public static readonly string Wait_Image = "Wait_Image";
        [Obsolete("Use CommandRegistry.GetAllTypeNames() instead")]
        public static readonly string Execute = "Execute";
        [Obsolete("Use CommandRegistry.GetAllTypeNames() instead")]
        public static readonly string Screenshot = "Screenshot";
        [Obsolete("Use CommandRegistry.GetAllTypeNames() instead")]
        public static readonly string Loop = "Loop";
        [Obsolete("Use CommandRegistry.GetAllTypeNames() instead")]
        public static readonly string Loop_End = "Loop_End";
        [Obsolete("Use CommandRegistry.GetAllTypeNames() instead")]
        public static readonly string Loop_Break = "Loop_Break";
        [Obsolete("Use CommandRegistry.GetAllTypeNames() instead")]
        public static readonly string IF_ImageExist = "IF_ImageExist";
        [Obsolete("Use CommandRegistry.GetAllTypeNames() instead")]
        public static readonly string IF_ImageNotExist = "IF_ImageNotExist";
        [Obsolete("Use CommandRegistry.GetAllTypeNames() instead")]
        public static readonly string IF_ImageExist_AI = "IF_ImageExist_AI";
        [Obsolete("Use CommandRegistry.GetAllTypeNames() instead")]
        public static readonly string IF_ImageNotExist_AI = "IF_ImageNotExist_AI";
        [Obsolete("Use CommandRegistry.GetAllTypeNames() instead")]
        public static readonly string IF_Variable = "IF_Variable";
        [Obsolete("Use CommandRegistry.GetAllTypeNames() instead")]
        public static readonly string IF_End = "IF_End";
        [Obsolete("Use CommandRegistry.GetAllTypeNames() instead")]
        public static readonly string SetVariable = "SetVariable";
        [Obsolete("Use CommandRegistry.GetAllTypeNames() instead")]
        public static readonly string SetVariable_AI = "SetVariable_AI";

        /// <summary>
        /// 全てのコマンドタイプを取得（CommandRegistry から自動生成）
        /// </summary>
        public static IEnumerable<string> GetTypes()
        {
            return CommandRegistry.GetAllTypeNames();
        }

        /// <summary>
        /// カテゴリ別のコマンドタイプを取得
        /// </summary>
        public static IEnumerable<string> GetTypesByCategory(CommandCategory category)
        {
            return CommandRegistry.GetTypeNamesByCategory(category);
        }

        /// <summary>
        /// If系コマンドかどうか判定（CommandRegistry に移譲）
        /// </summary>
        public static bool IsIfItem(string itemType)
        {
            return CommandRegistry.IsIfCommand(itemType);
        }

        /// <summary>
        /// ループ系コマンドかどうか判定（CommandRegistry に移譲）
        /// </summary>
        public static bool IsLoopItem(string itemType)
        {
            return CommandRegistry.IsLoopCommand(itemType);
        }
    }
}
