using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Command.Interface;
using OpenCVHelper;

namespace Command.Class
{
    public partial class Condition : ICondition
    {
        public IConditionSettings Settings { get; set; } = new ConditionSettings();

        public async Task<bool> Evaluate(CancellationToken cancellationToken)
        {
            await Task.Delay(0);
            throw new NotImplementedException();
        }
    }

    public partial class TrueCondition : ICondition
    {
        public IConditionSettings Settings { get; set; } = new ConditionSettings();

        public async Task<bool> Evaluate(CancellationToken cancellationToken)
        {
            await Task.Delay(0);
            return true;
        }
    }

    public partial class FalseCondition : ICondition
    {
        public IConditionSettings Settings { get; set; } = new ConditionSettings();
        
        public async Task<bool> Evaluate(CancellationToken cancellationToken)
        {
            await Task.Delay(0);
            return false;
        }
    }

    public partial class ImageExistsCondition : ICondition
    {
        public IConditionSettings Settings { get; set; } = new ImageConditionSettings();

        public ImageExistsCondition(IImageConditionSettings settings)
        {
            Settings = settings;
        }

        public async Task<bool> Evaluate(CancellationToken cancellationToken)
        {
            var settings = (IImageConditionSettings)Settings;
            var point = await ImageSearchHelper.WaitForImageAsync(settings.ImagePath, settings.Threshold, settings.Timeout, settings.Interval, cancellationToken);

            return point != null;
        }
    }

    public partial class ImageNotExistsCondition : ICondition
    {
        public IConditionSettings Settings { get; set; } = new ImageConditionSettings();

        public ImageNotExistsCondition(IImageConditionSettings settings)
        {
            Settings = settings;
        }

        public async Task<bool> Evaluate(CancellationToken cancellationToken)
        {
            var settings = (IImageConditionSettings)Settings;
            var point = await ImageSearchHelper.WaitForImageAsync(settings.ImagePath, settings.Threshold, settings.Timeout, settings.Interval, cancellationToken);
            
            return point == null;
        }
    }
}
