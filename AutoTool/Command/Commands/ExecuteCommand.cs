using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.ComponentModel;
using AutoTool.Command.Base;
using AutoTool.Command.Definition;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AutoTool.Command.Commands
{
    [AutoToolCommand(nameof(ExecuteCommand), typeof(ExecuteCommand))]
    public class ExecuteCommand : BaseCommand
    {
        [Category("基本設定"), DisplayName("実行ファイルパス")]
        public string ProgramPath { get; set; } = string.Empty;

        [Category("基本設定"), DisplayName("引数")]
        public string Arguments { get; set; } = string.Empty;

        [Category("基本設定"), DisplayName("作業ディレクトリ")]
        public string WorkingDirectory { get; set; } = string.Empty;

        public ExecuteCommand(IAutoToolCommand? parent = null, IServiceProvider? serviceProvider = null)
            : base(parent, serviceProvider)
        {
            Description = "外部プログラム実行";
        }

        protected override void ValidateSettings()
        {
            if (string.IsNullOrWhiteSpace(ProgramPath)) throw new ArgumentException("実行ファイルパスが指定されていません");
        }

        protected override void ValidateFiles()
        {
            if (!string.IsNullOrEmpty(ProgramPath))
            {
                ValidateFileExists(ProgramPath, "実行ファイル");
            }

            if (!string.IsNullOrEmpty(WorkingDirectory))
            {
                ValidateDirectoryExists(WorkingDirectory, "作業ディレクトリ");
            }
        }

        protected override async Task<bool> DoExecuteAsync(CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(ProgramPath)) return false;

            try
            {
                var resolvedProgramPath = ResolvePath(ProgramPath);
                var resolvedWorkingDirectory = !string.IsNullOrEmpty(WorkingDirectory) ? ResolvePath(WorkingDirectory) : string.Empty;

                _logger?.LogDebug("[DoExecuteAsync] Execute ProgramPath: {ProgramPath} -> {Resolved}", ProgramPath, resolvedProgramPath);

                var startInfo = new ProcessStartInfo
                {
                    FileName = resolvedProgramPath,
                    Arguments = Arguments,
                    WorkingDirectory = resolvedWorkingDirectory,
                    UseShellExecute = true,
                };

                await Task.Run(() =>
                {
                    Process.Start(startInfo);
                    LogMessage($"プログラムを実行しました: {Path.GetFileName(resolvedProgramPath)}");
                });

                return true;
            }
            catch (Exception ex)
            {
                LogMessage($"実行エラー: {ex.Message}");
                return false;
            }
        }
    }
}
