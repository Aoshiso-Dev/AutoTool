using AutoTool.Core.Abstractions;
using AutoTool.Core.Attributes;
using AutoTool.Core.Commands;
using AutoTool.Core.Diagnostics;
using AutoTool.Core.Utilities;
using AutoTool.Services.Abstractions;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace AutoTool.Commands.Flow.IfImageExist;

[Command("IfImageExist", "条件分岐（画像検出）", IconKey = "mdi:code-braces", Category = "フロー制御", Description = "画像検出条件に応じて処理を分岐します", Order = 30)]
public sealed class IfImageExistCommand :
ObservableObject,
IAutoToolCommand,
IHasSettings<IfImageExistSettings>,
IHasBlocks,
IValidatableCommand,
INotifyPropertyChanged
{
    public Guid Id { get; } = Guid.NewGuid();
    public string Type => "IfImageExist";
    public string DisplayName => "条件分岐（画像検出）";
    public bool IsEnabled { get; set; } = true;

    public IfImageExistSettings Settings { get; private set; }

    private readonly CommandBlock _then;
    private readonly CommandBlock _else;
    public ObservableCollection<CommandBlock> Blocks { get; }

    private readonly IServiceProvider? _serviceProvider = null;
    private readonly ILogger<IfImageExistCommand>? _logger = null;
    private readonly ICaptureService? _capture = null;

    public IfImageExistCommand(IfImageExistSettings settings,
                     IServiceProvider? serviceProvider,
                     IEnumerable<IAutoToolCommand>? then = null,
                     IEnumerable<IAutoToolCommand>? @else = null)
    {
        Settings = settings;

        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = _serviceProvider.GetService(typeof(ILogger<IfImageExistCommand>)) as ILogger<IfImageExistCommand> ?? throw new ArgumentNullException(nameof(_logger));
        _capture = _serviceProvider.GetService(typeof(ICaptureService)) as ICaptureService ?? throw new ArgumentNullException(nameof(_capture));

        _then = new CommandBlock("Then", then);
        _else = new CommandBlock("Else", @else);
        Blocks = new ObservableCollection<CommandBlock> { _then, _else };
    }

    public async Task<ControlFlow> ExecuteAsync(CancellationToken ct)
    {
        if (!IsEnabled) return ControlFlow.Next;
        if ( _capture == null) throw new InvalidOperationException("CaptureService is not set.");

        var ui = _serviceProvider?.GetService(typeof(IUIService)) as IUIService;
        ui?.ShowToast("IfImageExistCommand");

        var point = await _capture.FindImageAsync(Settings.ImagePath, Settings.WindowTitle, Settings.WindowClass, Settings.Similarity, ct);

        var target = point.HasValue ? _then.Children : _else.Children;

        foreach (var child in target)
        {
            var r = await child.ExecuteAsync(ct);
            if (r is not ControlFlow.Next) return r;
        }
        return ControlFlow.Next;
    }

    public IEnumerable<string> Validate(IServiceProvider _)
    {
        if (string.IsNullOrWhiteSpace(Settings.ImagePath))
        {
            yield return "画像パスを指定してください。";
        }
        else if (!System.IO.File.Exists(Settings.ImagePath))
        {
            yield return "指定された画像ファイルが存在しません。";
        }
        if (Settings.Similarity < 0.0 || Settings.Similarity > 1.0)
        {
            yield return "類似度は0.0から1.0の範囲で指定してください。";
        }
    }

    public void ReplaceSettings(IfImageExistSettings next)
    {
        Settings = next;
        OnPropertyChanged(nameof(Settings));
    }
}
