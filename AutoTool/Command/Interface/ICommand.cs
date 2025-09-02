using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AutoTool.Command.Interface
{
    /// <summary>
    /// Phase 5���S�����ŁF�R�}���h�C���^�[�t�F�[�X
    /// MacroPanels�ˑ����폜���AAutoTool�����ł̂ݎg�p
    /// </summary>
    public interface ICommand
    {
        int LineNumber { get; set; }
        bool IsEnabled { get; set; }
        ICommand? Parent { get; }
        IEnumerable<ICommand> Children { get; }
        int NestLevel { get; set; }
        object? Settings { get; set; }
        string Description { get; }

        event EventHandler? OnStartCommand;
        event EventHandler? OnFinishCommand;
        event EventHandler<string>? OnDoingCommand;

        void AddChild(ICommand child);
        void RemoveChild(ICommand child);
        IEnumerable<ICommand> GetChildren();

        Task<bool> Execute(CancellationToken cancellationToken);
    }

    /// <summary>
    /// Phase 5�����ŁF���[�g�R�}���h�C���^�[�t�F�[�X
    /// </summary>
    public interface IRootCommand : ICommand
    {
    }

    /// <summary>
    /// Phase 5�����ŁF��{�R�}���h�ݒ�C���^�[�t�F�[�X
    /// </summary>
    public interface ICommandSettings
    {
        string WindowTitle { get; set; }
        string WindowClassName { get; set; }
    }

    /// <summary>
    /// Phase 5�����ŁF�ҋ@�R�}���h�ݒ�
    /// </summary>
    public interface IWaitCommandSettings : ICommandSettings
    {
        int Wait { get; set; }
    }

    /// <summary>
    /// Phase 5�����ŁF�N���b�N�R�}���h�ݒ�
    /// </summary>
    public interface IClickCommandSettings : ICommandSettings
    {
        int X { get; set; }
        int Y { get; set; }
        System.Windows.Input.MouseButton Button { get; set; }
        bool UseBackgroundClick { get; set; }
        int BackgroundClickMethod { get; set; }
    }

    /// <summary>
    /// Phase 5�����ŁF���[�v�R�}���h�ݒ�
    /// </summary>
    public interface ILoopCommandSettings : ICommandSettings
    {
        int LoopCount { get; set; }
    }

    /// <summary>
    /// Phase 5�����ŁF�摜�ҋ@�R�}���h�ݒ�
    /// </summary>
    public interface IWaitImageCommandSettings : ICommandSettings
    {
        string ImagePath { get; set; }
        int Timeout { get; set; }
        int Interval { get; set; }
        double Threshold { get; set; }
        bool SearchColor { get; set; }
    }

    /// <summary>
    /// Phase 5�����ŁF�摜�N���b�N�R�}���h�ݒ�
    /// </summary>
    public interface IClickImageCommandSettings : IWaitImageCommandSettings
    {
        System.Windows.Input.MouseButton Button { get; set; }
        bool UseBackgroundClick { get; set; }
        int BackgroundClickMethod { get; set; }
    }

    /// <summary>
    /// Phase 5�����ŁF�z�b�g�L�[�R�}���h�ݒ�
    /// </summary>
    public interface IHotkeyCommandSettings : ICommandSettings
    {
        string Key { get; set; }
        bool Ctrl { get; set; }
        bool Alt { get; set; }
        bool Shift { get; set; }
    }

    // ����R�}���h�C���^�[�t�F�[�X
    public interface IWaitCommand : ICommand { }
    public interface IClickCommand : ICommand { }
    public interface ILoopCommand : ICommand { }
    public interface IWaitImageCommand : ICommand { }
    public interface IClickImageCommand : ICommand { }
    public interface IHotkeyCommand : ICommand { }
}