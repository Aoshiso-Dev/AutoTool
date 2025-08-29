using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace MacroPanels.Command.Interface
{

    public interface ICommand
    {
        int LineNumber { get; set; }
        bool IsEnabled { get; set; }
        ICommand? Parent { get; set; }
        IEnumerable<ICommand> Children { get; set; }
        int NestLevel { get; set; }
        ICommandSettings Settings { get; }
        EventHandler OnStartCommand { get; set; }
        EventHandler OnFinishCommand { get; set; }

        Task<bool> Execute(CancellationToken cancellationToken);

        bool CanExecute();
    }

    public interface IRootCommand : ICommand { }
    public interface IIfCommand : ICommand { }
    public interface IWaitImageCommand : ICommand { new IWaitImageCommandSettings Settings { get; } }
    public interface IClickImageCommand : ICommand { new IClickImageCommandSettings Settings { get; } }
    public interface IHotkeyCommand : ICommand { new IHotkeyCommandSettings Settings { get; } }
    public interface IClickCommand : ICommand { new IClickCommandSettings Settings { get; } }
    public interface IWaitCommand : ICommand { new IWaitCommandSettings Settings { get; } }
    public interface IIfImageExistCommand : ICommand, IIfCommand { new IWaitImageCommandSettings Settings { get; } }
    public interface IIfImageNotExistCommand : ICommand, IIfCommand { new IWaitImageCommandSettings Settings { get; } }
    public interface ILoopCommand : ICommand { new ILoopCommandSettings Settings { get; } }
    public interface IEndLoopCommand : ICommand { new ILoopEndCommandSettings Settings { get; } }
    public interface ILoopBreakCommand : ICommand { }
    public interface IIfImageExistAICommand : ICommand, IIfCommand { new IIfImageExistAISettings Settings { get; } }
    public interface IExecuteCommand : ICommand { new IExecuteCommandSettings Settings { get; } }
    public interface ISetVariableCommand : ICommand { new ISetVariableCommandSettings Settings { get; } }
    public interface ISetVariableAICommand : ICommand { new ISetVariableAICommandSettings Settings { get; } }
    public interface IIfVariableCommand : ICommand, IIfCommand { new IIfVariableCommandSettings Settings { get; } }
    public interface IScreenshotCommand : ICommand { new IScreenshotCommandSettings Settings { get; } }
}
