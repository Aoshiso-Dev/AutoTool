using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Panels.Command.Interface;

namespace Panels.Command.Class
{
    internal class Condition : ICondition
    {
        public bool Evaluate(out Exception exception)
        {
            throw new NotImplementedException();
        }
    }

    internal class  TrueCondition : Condition
    {
        public new bool Evaluate(out Exception exception)
        {
            exception = null;
            return true;
        }
    }

    internal class FalseCondition : Condition
    {
        public new bool Evaluate(out Exception exception)
        {
            exception = null;
            return false;
        }
    }

    internal class ImageExistsCondition : Condition, IImageCondition
    {
        public IImageConditionSettings Settings { get; set; }

        public ImageExistsCondition(IImageConditionSettings settings)
        {
            Settings = settings;
        }

        public new bool Evaluate(out Exception exception)
        {
            // TODO
            exception = null;
            return true;
        }
    }

    internal class ImageNotExistsCondition : Condition, IImageCondition
    {
        public IImageConditionSettings Settings { get; set; }

        public ImageNotExistsCondition(IImageConditionSettings settings)
        {
            Settings = settings;
        }

        public new bool Evaluate(out Exception exception)
        {
            // TODO
            exception = null;
            return true;
        }
    }
}
