using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.ComponentModel;
using AutoTool.Command.Base;
using AutoTool.Command.Definition;
using AutoTool.Services.Keyboard;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AutoTool.Command.Commands
{
    [AutoToolCommand(nameof(TypeTextCommand), typeof(TypeTextCommand))]
    public class TypeTextCommand : BaseCommand
    {
        private readonly IKeyboardService _keyboardService;

        [Category("基本設定"), DisplayName("入力テキスト")]
        public string Text { get; set; } = string.Empty;

        [Category("基本設定"), DisplayName("ウィンドウタイトル")]
        public string WindowTitle { get; set; } = string.Empty;

        [Category("基本設定"), DisplayName("ウィンドウクラス名")]
        public string WindowClassName { get; set; } = string.Empty;

        [Category("詳細設定"), DisplayName("入力間隔")]
        public int InputInterval { get; set; } = 10;

        public TypeTextCommand(IAutoToolCommand? parent = null, IServiceProvider? serviceProvider = null)
            : base(parent, serviceProvider)
        {
            Description = "タイプテキスト";
            _keyboardService = serviceProvider?.GetService<IKeyboardService>() ?? throw new InvalidOperationException("IKeyboardServiceが利用できません");
        }

        protected override void ValidateSettings()
        {
            if (string.IsNullOrWhiteSpace(Text))
            {
                throw new ArgumentException("入力テキストが指定されていません");
            }

            if (InputInterval < 0)
            {
                throw new ArgumentException("入力間隔が不正です");
            }
        }

        protected override async Task<bool> DoExecuteAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (string.IsNullOrEmpty(WindowTitle) && string.IsNullOrEmpty(WindowClassName))
                {
                    await _keyboardService.TypeTextAsync(Text);
                    LogMessage($"テキストを入力しました: \"{Text}\" (全画面)");
                }
                else
                {
                    await _keyboardService.TypeTextAsync(Text, WindowTitle, WindowClassName);
                    LogMessage($"テキストを入力しました: \"{Text}\" -> {WindowTitle}[{WindowClassName}]");
                }

                return true;
            }
            catch (Exception ex)
            {
                LogMessage($"タイプテキストエラー: {ex.Message}");
                throw;
            }
        }
    }
}
