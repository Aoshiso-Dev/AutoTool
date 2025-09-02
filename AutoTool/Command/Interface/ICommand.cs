using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AutoTool.Command.Interface
{
    /// <summary>
    /// Phase 5完全統合版：コマンドインターフェース
    /// MacroPanels依存を削除し、AutoTool統合版のみ使用
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
    /// Phase 5統合版：ルートコマンドインターフェース
    /// </summary>
    public interface IRootCommand : ICommand
    {
    }

    /// <summary>
    /// Phase 5統合版：基本コマンド設定インターフェース
    /// </summary>
    public interface ICommandSettings
    {
        string WindowTitle { get; set; }
        string WindowClassName { get; set; }
    }

    /// <summary>
    /// Phase 5統合版：待機コマンド設定
    /// </summary>
    public interface IWaitCommandSettings : ICommandSettings
    {
        int Wait { get; set; }
    }

    /// <summary>
    /// Phase 5統合版：クリックコマンド設定
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
    /// Phase 5統合版：ループコマンド設定
    /// </summary>
    public interface ILoopCommandSettings : ICommandSettings
    {
        int LoopCount { get; set; }
    }

    /// <summary>
    /// Phase 5統合版：画像待機コマンド設定
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
    /// Phase 5統合版：画像クリックコマンド設定
    /// </summary>
    public interface IClickImageCommandSettings : IWaitImageCommandSettings
    {
        System.Windows.Input.MouseButton Button { get; set; }
        bool UseBackgroundClick { get; set; }
        int BackgroundClickMethod { get; set; }
    }

    /// <summary>
    /// Phase 5統合版：ホットキーコマンド設定
    /// </summary>
    public interface IHotkeyCommandSettings : ICommandSettings
    {
        string Key { get; set; }
        bool Ctrl { get; set; }
        bool Alt { get; set; }
        bool Shift { get; set; }
    }

    // 特定コマンドインターフェース
    public interface IWaitCommand : ICommand { }
    public interface IClickCommand : ICommand { }
    public interface ILoopCommand : ICommand { }
    public interface IWaitImageCommand : ICommand { }
    public interface IClickImageCommand : ICommand { }
    public interface IHotkeyCommand : ICommand { }
}