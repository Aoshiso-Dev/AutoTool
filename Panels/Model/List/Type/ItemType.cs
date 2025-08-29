using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MacroPanels.Model.List.Type
{
    public class ItemType
    {
        public static readonly string Click = "Click";
        public static readonly string Click_Image = "Click_Image";
        public static readonly string Hotkey = "Hotkey";
        public static readonly string Wait = "Wait";
        public static readonly string Wait_Image = "Wait_Image";
        public static readonly string Execute = "Execute";
        public static readonly string Screenshot = "Screenshot";
        public static readonly string Loop = "Loop";
        public static readonly string Loop_End = "Loop_End";
        public static readonly string Loop_Break = "Loop_Break";
        public static readonly string IF_ImageExist = "IF_ImageExist";
        public static readonly string IF_ImageNotExist = "IF_ImageNotExist";
        public static readonly string IF_ImageExist_AI = "IF_ImageExistAI";
        public static readonly string IF_ImageNotExist_AI = "IF_ImageNotExistAI";
        public static readonly string IF_Variable = "IF_Variable";
        public static readonly string IF_End = "IF_End";
        public static readonly string SetVariable = "SetVariable";
        public static readonly string SetVariable_AI = "SetVariableAI";

        public static IEnumerable<string> GetTypes()
        {
            return new List<string>
            {
                Click,
                Click_Image,
                Hotkey,
                Wait,
                Wait_Image,
                Execute,
                Screenshot,
                Loop,
                Loop_End,
                Loop_Break,
                IF_ImageExist,
                IF_ImageNotExist,
                IF_ImageExist_AI,
                IF_ImageNotExist_AI,
                IF_Variable,
                IF_End,
                SetVariable,
                SetVariable_AI
            };
        }

        public static bool IsIfItem(string itemType)
        {
            return itemType == IF_ImageExist ||
                   itemType == IF_ImageNotExist ||
                   itemType == IF_ImageExist_AI ||
                   itemType == IF_ImageNotExist_AI ||
                   itemType == IF_Variable;
        }

        public static bool IsLoopItem(string itemType)
        {
            return itemType == Loop;
        }
    }
}
