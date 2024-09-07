using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Panels.Command.Interface
{
    public interface IConditionSettings
    {
    }

    public interface IImageConditionSettings : IConditionSettings
    {
        string ImagePath { get; set; }
        double Threshold { get; set; }
        int Timeout { get; set; }
        int Interval { get; set; }
    }
}
