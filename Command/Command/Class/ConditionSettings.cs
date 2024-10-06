using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Command.Interface;

namespace Command.Class
{
    public class ConditionSettings : IConditionSettings
    {
    }

    public partial class ImageConditionSettings : IImageConditionSettings
    {
        public string ImagePath { get; set; } = string.Empty;
        public double Threshold { get; set; } = 0.8;
        public int Timeout { get; set; } = 5000;
        public int Interval { get; set; } = 500;
        public string WindowTitle { get; set; } = string.Empty;
    }
}
