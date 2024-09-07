using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Panels.List.Type
{
    public class ItemType
    {
        public static readonly string WaitImage = "WaitImage";
        public static readonly string ClickImage = "ClickImage";
        public static readonly string Click = "Click";
        public static readonly string Hotkey = "Hotkey";
        public static readonly string Wait = "Wait";
        public static readonly string Loop = "Loop";
        public static readonly string EndLoop = "EndLoop";
        public static readonly string IfImageExist = "IfImageExist";
        public static readonly string IfImageNotExist = "IfImageNotExist";
        public static readonly string EndIf = "EndIf";
    }
}
