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

    public partial class ImageConditionSettings : ObservableObject, IImageConditionSettings
    {
        [ObservableProperty]
        private string _imagePath = string.Empty;
        [ObservableProperty]
        private double _threshold = 0.8;
        [ObservableProperty]
        private int _timeout = 5000;
        [ObservableProperty]
        private int _interval = 500;
    }
}
