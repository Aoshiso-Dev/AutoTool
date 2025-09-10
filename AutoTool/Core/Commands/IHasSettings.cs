using AutoTool.Core.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoTool.Core.Commands
{
    /// <summary>このコマンドが強い型の設定を持つ場合の公開。</summary>
    public interface IHasSettings<out TSettings> where TSettings : IAutoToolCommandSettings
    {
        TSettings Settings { get; }
    }
}
