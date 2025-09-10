using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoTool.Core.Descriptors
{
    public sealed record BlockSlot(
        string Name,
        bool AllowEmpty = true,
        int Min = 0,
        int? Max = null
    );
}
