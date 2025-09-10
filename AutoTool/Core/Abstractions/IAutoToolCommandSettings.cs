using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoTool.Core.Abstractions
{
    public interface IAutoToolCommandSettings
    {
        int Version { get; } // 設定スキーマのバージョン
    }
}
