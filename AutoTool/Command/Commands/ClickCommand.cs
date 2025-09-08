using System.ComponentModel;
using System.Windows.Input;
using AutoTool.Command.Base;
using AutoTool.Command.Definition;
using AutoTool.Services.Mouse;
using Microsoft.Extensions.DependencyInjection;

namespace AutoTool.Command.Commands
{
    [AutoToolCommand(nameof(ClickCommand), typeof(ClickCommand))]
    public sealed class ClickCommand : BaseCommand
    {
        public sealed class Settings : IAutoToolCommandSettings
        {
            [Category("基本設定"), DisplayName("クリック座標")]
            public System.Windows.Point MousePosition { get; set; } = new(100, 100);

            [Category("基本設定"), DisplayName("マウスボタン")]
            public MouseButton Button { get; set; } = MouseButton.Left;

            [Category("基本設定"), DisplayName("ウィンドウタイトル")]
            public string WindowTitle { get; set; } = string.Empty;

            [Category("基本設定"), DisplayName("ウィンドウクラス名")]
            public string WindowClassName { get; set; } = string.Empty;
        }

        private readonly IMouseService _mouseService;

        public Settings CommandSettings { get; }

        public ClickCommand(IAutoToolCommand? parent = null, IAutoToolCommandSettings? settings = null, IServiceProvider? serviceProvider = null)
            : base(parent, serviceProvider)
        {
            Description = "指定した座標をクリックします";
            CommandSettings = settings as Settings ?? new Settings();
            _mouseService = serviceProvider?.GetService<IMouseService>() ?? throw new InvalidOperationException("IMouseServiceが見つかりません");
        }

        protected override void ValidateSettings()
        {
            if (CommandSettings.MousePosition.X < 0 || CommandSettings.MousePosition.Y < 0)
                throw new ArgumentException("座標は0以上で指定してください。");
        }

        protected override async Task<bool> DoExecuteAsync(CancellationToken cancellationToken)
        {
            var x = (int)CommandSettings.MousePosition.X;
            var y = (int)CommandSettings.MousePosition.Y;

            await (CommandSettings.Button switch
            {
                MouseButton.Left => _mouseService.ClickAsync(x, y, CommandSettings.WindowTitle, CommandSettings.WindowClassName),
                MouseButton.Right => _mouseService.RightClickAsync(x, y, CommandSettings.WindowTitle, CommandSettings.WindowClassName),
                MouseButton.Middle => _mouseService.MiddleClickAsync(x, y, CommandSettings.WindowTitle, CommandSettings.WindowClassName),
                _ => throw new Exception("マウスボタンが不正です。"),
            });

            var targetDesc = string.IsNullOrEmpty(CommandSettings.WindowTitle) ? "グローバル" : $"{CommandSettings.WindowTitle}";
            LogMessage($"{targetDesc} の ({x}, {y}) を {CommandSettings.Button} クリックしました");

            return true;
        }
    }
}
