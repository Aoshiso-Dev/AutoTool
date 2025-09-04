using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using AutoTool.Model.List.Interface;

using AutoTool.Command.Interface;

namespace AutoTool.Services.Execution
{
    /// <summary>
    /// �}�N�����s���Ǘ�����T�[�r�X
    /// </summary>
    public interface IMacroExecutionService
    {
        bool IsRunning { get; }
        string StatusMessage { get; }
        Task<bool> StartAsync(ObservableCollection<ICommandListItem> items);
        Task StopAsync();
        
        event EventHandler<string> StatusChanged;
        event EventHandler<bool> RunningStateChanged;
    }

    /// <summary>
    /// �}�N�����s�T�[�r�X�̎���
    /// </summary>
    public class MacroExecutionService : IMacroExecutionService
    {
        private readonly ILogger<MacroExecutionService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private CancellationTokenSource? _cancellationTokenSource;
        private bool _isRunning;
        private string _statusMessage = "��������";

        public bool IsRunning => _isRunning;
        public string StatusMessage => _statusMessage;

        public event EventHandler<string>? StatusChanged;
        public event EventHandler<bool>? RunningStateChanged;

        public MacroExecutionService(ILogger<MacroExecutionService> logger, IServiceProvider serviceProvider)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public async Task<bool> StartAsync(ObservableCollection<ICommandListItem> items)
        {
            if (_isRunning)
            {
                _logger.LogWarning("�}�N���͊��Ɏ��s���ł�");
                return false;
            }

            if (items.Count == 0)
            {
                _logger.LogWarning("���s�Ώۂ̃R�}���h������܂���");
                SetStatus("���s�Ώۂ�����܂���");
                return false;
            }

            try
            {
                SetRunningState(true);
                SetStatus("���s��...");

                _cancellationTokenSource = new CancellationTokenSource();
                var token = _cancellationTokenSource.Token;

                // MacroFactory�̐ݒ�
                AutoTool.Model.MacroFactory.MacroFactory.SetServiceProvider(_serviceProvider);

                var result = await Task.Run(() => ExecuteMacro(items, token), token);
                
                SetStatus(result ? "���s����" : "�ꕔ���s/���f");
                return result;
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("�}�N�����L�����Z������܂���");
                SetStatus("���s�L�����Z��");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�}�N�����s���ɃG���[���������܂���");
                SetStatus($"���s�G���[: {ex.Message}");
                return false;
            }
            finally
            {
                SetRunningState(false);
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }
        }

        public async Task StopAsync()
        {
            if (!_isRunning || _cancellationTokenSource == null)
            {
                _logger.LogWarning("��~�v��: �}�N���͎��s���ł͂���܂���");
                return;
            }

            try
            {
                _logger.LogInformation("�}�N����~�v������M���܂���");
                SetStatus("��~�v����...");

                _cancellationTokenSource.Cancel();

                // �����^�C���A�E�g�i5�b�j
                await Task.Delay(5000);
                
                if (_isRunning)
                {
                    _logger.LogWarning("�}�N����5�b�ȓ��ɒ�~���Ȃ��������߁A�����I�����܂�");
                    SetRunningState(false);
                    SetStatus("������~����");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�}�N����~���ɃG���[���������܂���");
                SetStatus($"��~�G���[: {ex.Message}");
                SetRunningState(false);
            }
        }

        private bool ExecuteMacro(ObservableCollection<ICommandListItem> items, CancellationToken cancellationToken)
        {
            try
            {
                var itemsSnapshot = new List<ICommandListItem>(items);
                var root = AutoTool.Model.MacroFactory.MacroFactory.CreateMacro(itemsSnapshot);
                
                var task = root.Execute(cancellationToken);
                
                // �����I�ɑҋ@�iCancellationToken���Ď��j
                while (!task.IsCompleted)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    Thread.Sleep(50);
                }
                
                return task.Result;
            }
            catch (AggregateException ex)
            {
                if (ex.InnerException is OperationCanceledException)
                    throw ex.InnerException;
                throw;
            }
        }

        private void SetRunningState(bool isRunning)
        {
            if (_isRunning != isRunning)
            {
                _isRunning = isRunning;
                RunningStateChanged?.Invoke(this, isRunning);
            }
        }

        private void SetStatus(string status)
        {
            if (_statusMessage != status)
            {
                _statusMessage = status;
                StatusChanged?.Invoke(this, status);
            }
        }
    }
}