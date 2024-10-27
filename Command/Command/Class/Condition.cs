using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Command.Interface;
using OpenCVHelper;
/*
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

            var stopwatch = Stopwatch.StartNew();

            while (stopwatch.ElapsedMilliseconds < settings.Timeout)
            {
                var point = ImageSearchHelper.SearchImage(settings.ImagePath, cancellationToken, settings.Threshold, settings. settings.WindowTitle, settings.WindowClassName);

                if (point != null)
                {
                    return true;
                }

                if (cancellationToken.IsCancellationRequested) return false;

                await Task.Delay(settings.Interval, cancellationToken);
            }

            return false;
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

            var stopwatch = Stopwatch.StartNew();

            while (stopwatch.ElapsedMilliseconds < settings.Timeout)
            {
                var point = string.IsNullOrEmpty(settings.WindowTitle)
                    ? ImageSearchHelper.SearchImageFromScreen(settings.ImagePath, cancellationToken, settings.Threshold)
                    : ImageSearchHelper.SearchImageFromWindow(settings.WindowTitle, settings.ImagePath, cancellationToken, settings.Threshold);

                if (point != null)
                {
                    return false;
                }

                if (cancellationToken.IsCancellationRequested) return false;

                await Task.Delay(settings.Interval, cancellationToken);
            }

            return true;
        }
    }
}
*/