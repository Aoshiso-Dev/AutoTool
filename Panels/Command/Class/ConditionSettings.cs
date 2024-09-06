using CommunityToolkit.Mvvm.ComponentModel;
using Panels.Command.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Panels.Command.Class
{
    public class ConditionSettings : ObservableObject, IConditionSettings
    {
    }

    public class ImageExistsConditionSettings : ConditionSettings, IImageConditionSettings
    {
        public string ImagePath { get; set; }
        public double Threshold { get; set; }
        public double Timeout { get; set; }
        public double Interval { get; set; }
    }
}
