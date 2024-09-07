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

    public partial class ImageExistsConditionSettings : ConditionSettings, IImageConditionSettings
    {
        [ObservableProperty]
        private string _imagePath = string.Empty;
        [ObservableProperty]
        private double _threshold = 0.8;
        [ObservableProperty]
        private double _timeout = 5000;
        [ObservableProperty]
        private double _interval = 500;
    }
}
