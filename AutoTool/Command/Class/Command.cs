using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using AutoTool.Command.Interface;
using CommunityToolkit.Mvvm.Messaging;
using AutoTool.Message;
using WpfMouseButton = System.Windows.Input.MouseButton;
using AutoToolICommand = AutoTool.Command.Interface.ICommand;

namespace AutoTool.Command.Class
{
    /// <summary>
    /// Phase 5���S�����ŁF�R�}���h�̊��N���X
    /// MacroPanels�ˑ����폜���AAutoTool�����ł̂ݎg�p
    /// </summary>
    public abstract class BaseCommand : AutoToolICommand
    {
        // �v���C�x�[�g�t�B�[���h
        private readonly List<AutoToolICommand> _children = new();
        protected readonly ILogger? _logger;
        protected readonly IServiceProvider? _serviceProvider;

        // ICommand�C���^�[�t�F�[�X�̎���
        public int LineNumber { get; set; }
        public bool IsEnabled { get; set; } = true;
        public AutoToolICommand? Parent { get; private set; }
        public IEnumerable<AutoToolICommand> Children => _children;
        public int NestLevel { get; set; }
        public object? Settings { get; set; }
        public string Description { get; protected set; } = string.Empty;

        // �C�x���g
        public event EventHandler? OnStartCommand;
        public event EventHandler? OnFinishCommand;
        public event EventHandler<string>? OnDoingCommand;

        protected BaseCommand(AutoToolICommand? parent = null, object? settings = null, IServiceProvider? serviceProvider = null)
        {
            Parent = parent;
            Settings = settings;
            NestLevel = parent?.NestLevel + 1 ?? 0;
            _serviceProvider = serviceProvider;
            _logger = serviceProvider?.GetService<ILogger<BaseCommand>>();
            
            // Phase 5: AutoTool�����Ń��b�Z�[�W���O
            OnStartCommand += (sender, e) => WeakReferenceMessenger.Default.Send(new StartCommandMessage(this));
            OnDoingCommand += (sender, log) => WeakReferenceMessenger.Default.Send(new DoingCommandMessage(this, log ?? ""));
            OnFinishCommand += (sender, e) => WeakReferenceMessenger.Default.Send(new FinishCommandMessage(this));
        }

        public virtual void AddChild(AutoToolICommand child)
        {
            _children.Add(child);
        }

        public virtual void RemoveChild(AutoToolICommand child)
        {
            _children.Remove(child);
        }

        public virtual IEnumerable<AutoToolICommand> GetChildren()
        {
            return _children;
        }

        /// <summary>
        /// �R�}���h�����s
        /// </summary>
        public virtual async Task<bool> Execute(CancellationToken cancellationToken)
        {
            if (!IsEnabled)
                return true;

            OnStartCommand?.Invoke(this, EventArgs.Empty);

            try
            {
                var result = await DoExecuteAsync(cancellationToken);
                OnFinishCommand?.Invoke(this, EventArgs.Empty);
                return result;
            }
            catch (OperationCanceledException)
            {
                OnFinishCommand?.Invoke(this, EventArgs.Empty);
                throw;
            }
            catch (Exception ex)
            {
                LogMessage($"? ���s�G���[: {ex.Message}");
                _logger?.LogError(ex, "�R�}���h���s���ɃG���[���������܂���: {Description}", Description);
                OnFinishCommand?.Invoke(this, EventArgs.Empty);
                return false;
            }
        }

        /// <summary>
        /// ���ۂ̎��s�����i�h���N���X�Ŏ����j
        /// </summary>
        protected abstract Task<bool> DoExecuteAsync(CancellationToken cancellationToken);

        /// <summary>
        /// �q�R�}���h���������s
        /// </summary>
        protected async Task<bool> ExecuteChildrenAsync(CancellationToken cancellationToken)
        {
            foreach (var child in _children)
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                var result = await child.Execute(cancellationToken);
                if (!result)
                    return false;
            }
            return true;
        }

        /// <summary>
        /// ���O�o��
        /// </summary>
        protected void LogMessage(string message)
        {
            OnDoingCommand?.Invoke(this, message);
            _logger?.LogInformation("{Message}", message);
        }

        /// <summary>
        /// �T�[�r�X���擾
        /// </summary>
        protected T? GetService<T>() where T : class
        {
            return _serviceProvider?.GetService<T>();
        }

        /// <summary>
        /// �K�{�T�[�r�X���擾
        /// </summary>
        protected T GetRequiredService<T>() where T : class
        {
            if (_serviceProvider == null)
                throw new InvalidOperationException("ServiceProvider ���ݒ肳��Ă��܂���");
            
            return _serviceProvider.GetRequiredService<T>();
        }
    }

    /// <summary>
    /// Phase 5�����ŁF���[�g�R�}���h
    /// </summary>
    public class RootCommand : BaseCommand, IRootCommand
    {
        public RootCommand(IServiceProvider? serviceProvider = null) : base(null, null, serviceProvider)
        {
            Description = "Phase 5�����Ń��[�g�R�}���h";
        }

        protected override Task<bool> DoExecuteAsync(CancellationToken cancellationToken)
        {
            return ExecuteChildrenAsync(cancellationToken);
        }
    }

    /// <summary>
    /// Phase 5�����ŁF�ҋ@�R�}���h
    /// </summary>
    public class WaitCommand : BaseCommand, IWaitCommand
    {
        public new IWaitCommandSettings Settings => (IWaitCommandSettings)base.Settings!;

        public WaitCommand(AutoToolICommand? parent = null, object? settings = null, IServiceProvider? serviceProvider = null) 
            : base(parent, settings, serviceProvider)
        {
            Description = "Phase 5�����őҋ@";
        }

        protected override async Task<bool> DoExecuteAsync(CancellationToken cancellationToken)
        {
            var settings = Settings;
            if (settings == null) return false;

            var stopwatch = Stopwatch.StartNew();

            while (stopwatch.ElapsedMilliseconds < settings.Wait)
            {
                if (cancellationToken.IsCancellationRequested) return false;
                await Task.Delay(50, cancellationToken);
            }

            LogMessage("Phase 5�����őҋ@���������܂����B");
            return true;
        }
    }

    /// <summary>
    /// Phase 5�����ŁF�N���b�N�R�}���h
    /// </summary>
    public class ClickCommand : BaseCommand, IClickCommand
    {
        public new IClickCommandSettings Settings => (IClickCommandSettings)base.Settings!;

        public ClickCommand(AutoToolICommand? parent = null, object? settings = null, IServiceProvider? serviceProvider = null) 
            : base(parent, settings, serviceProvider)
        {
            Description = "Phase 5�����ŃN���b�N";
        }

        protected override async Task<bool> DoExecuteAsync(CancellationToken cancellationToken)
        {
            var settings = Settings;
            if (settings == null) return false;

            // Phase 5: ��{�I�ȃN���b�N�����i�ڍ׎����͌�Œǉ��\��j
            await Task.Delay(100, cancellationToken);

            LogMessage($"Phase 5�����ŃN���b�N���܂����B({settings.X}, {settings.Y})");
            return true;
        }
    }

    /// <summary>
    /// Phase 5�����ŁF���[�v�R�}���h
    /// </summary>
    public class LoopCommand : BaseCommand, ILoopCommand
    {
        public new ILoopCommandSettings Settings => (ILoopCommandSettings)base.Settings!;

        public LoopCommand(AutoToolICommand? parent = null, object? settings = null, IServiceProvider? serviceProvider = null) 
            : base(parent, settings, serviceProvider)
        {
            Description = "Phase 5�����Ń��[�v";
        }

        protected override async Task<bool> DoExecuteAsync(CancellationToken cancellationToken)
        {
            var settings = Settings;
            if (settings == null) return false;

            LogMessage($"Phase 5�����Ń��[�v���J�n���܂��B({settings.LoopCount}��)");

            for (int i = 0; i < settings.LoopCount; i++)
            {
                if (cancellationToken.IsCancellationRequested) return false;

                var result = await ExecuteChildrenAsync(cancellationToken);
                if (!result) return false;
            }

            LogMessage("Phase 5�����Ń��[�v���������܂����B");
            return true;
        }
    }

    #region Phase 5�����Őݒ�N���X

    /// <summary>
    /// Phase 5�����ŁF��{�ݒ�N���X
    /// </summary>
    public class BasicCommandSettings : ICommandSettings
    {
        public string WindowTitle { get; set; } = string.Empty;
        public string WindowClassName { get; set; } = string.Empty;
    }

    /// <summary>
    /// Phase 5�����ŁF�ҋ@�ݒ�
    /// </summary>
    public class BasicWaitSettings : BasicCommandSettings, IWaitCommandSettings
    {
        public int Wait { get; set; } = 1000;
    }

    /// <summary>
    /// Phase 5�����ŁF�N���b�N�ݒ�
    /// </summary>
    public class BasicClickSettings : BasicCommandSettings, IClickCommandSettings
    {
        public int X { get; set; } = 0;
        public int Y { get; set; } = 0;
        public WpfMouseButton Button { get; set; } = WpfMouseButton.Left;
        public bool UseBackgroundClick { get; set; } = false;
        public int BackgroundClickMethod { get; set; } = 0;
    }

    /// <summary>
    /// Phase 5�����ŁF���[�v�ݒ�
    /// </summary>
    public class BasicLoopSettings : BasicCommandSettings, ILoopCommandSettings
    {
        public int LoopCount { get; set; } = 1;
    }

    #endregion
}