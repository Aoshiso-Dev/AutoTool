using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Panels.Command.Class;
using Panels.Command.Interface;

namespace Panels.Command.Class
{
    public partial class Condition : ObservableObject, ICondition
    {
        [ObservableProperty]
        private IConditionSettings _settings = new ConditionSettings();

        public async Task<bool> Evaluate(CancellationToken cancellationToken)
        {
            await Task.Delay(0);
            throw new NotImplementedException();
        }
    }

    public partial class TrueCondition : ObservableObject, ICondition
    {
        [ObservableProperty]
        private IConditionSettings _settings = new ConditionSettings();

        public async Task<bool> Evaluate(CancellationToken cancellationToken)
        {
            await Task.Delay(0);
            return true;
        }
    }

    public partial class FalseCondition : ObservableObject, ICondition
    {
        [ObservableProperty]
        private IConditionSettings _settings = new ConditionSettings();

        public async Task<bool> Evaluate(CancellationToken cancellationToken)
        {
            await Task.Delay(0);
            return false;
        }
    }

    public partial class ImageExistsCondition : ObservableObject, ICondition
    {

        [ObservableProperty]
        private IConditionSettings _settings = new ImageConditionSettings();

        public ImageExistsCondition(IImageConditionSettings settings)
        {
            Settings = settings;
        }

        public async Task<bool> Evaluate(CancellationToken cancellationToken)
        {
            var settings = (IImageConditionSettings)Settings;
            var point = await ImageFinder.WaitForImageAsync(settings.ImagePath, settings.Threshold, settings.Timeout, settings.Interval, cancellationToken);

            return point != null;
        }
    }

    public partial class ImageNotExistsCondition : ObservableObject, ICondition
    {
        [ObservableProperty]
        private IConditionSettings _settings = new ImageConditionSettings();

        public ImageNotExistsCondition(IImageConditionSettings settings)
        {
            Settings = settings;
        }

        public async Task<bool> Evaluate(CancellationToken cancellationToken)
        {
            var settings = (IImageConditionSettings)Settings;
            var point = await ImageFinder.WaitForImageAsync(settings.ImagePath, settings.Threshold, settings.Timeout, settings.Interval, cancellationToken);
            
            return point == null;
        }
    }
}
