using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Threading;
using AutoTool.Application.Ports;
using AutoTool.Desktop.CommandLine;
using AutoTool.Automation.Runtime.Diagnostics;
using AutoTool.Commands.Infrastructure;
using AutoTool.Commands.Services;
using AutoTool.Desktop.Hosting;
using AutoTool.Desktop.Services;
using AutoTool.Desktop.Ui;
using AutoTool.Infrastructure;
using AutoTool.Infrastructure.Implementations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AutoTool.Bootstrap;

/// <summary>
/// App.xaml のコードビハインドです。
/// </summary>
public partial class App : System.Windows.Application
{
    private static readonly Lock CrashLogSync = new();
    private const string SingleInstanceMutexName = @"Local\AutoTool.SingleInstance.v1";
    private IHost? _host;
    private INotifier? _notifier;
    private ILogWriter? _logWriter;
    private Mutex? _singleInstanceMutex;

    protected override void OnStartup(StartupEventArgs e)
    {
        var parseResult = CommandLineInvocationParser.Parse(e.Args);
        if (!parseResult.IsSuccess || parseResult.Invocation is null)
        {
            var message = parseResult.ErrorMessage ?? "引数の解析に失敗しました。";
            MessageBox.Show(message, "引数エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
            Environment.ExitCode = CommandLineExitCodes.InvalidArguments;
            Shutdown();
            return;
        }

        var invocation = parseResult.Invocation;
        if (!TryAcquireSingleInstanceMutex(out _singleInstanceMutex))
        {
            var forwardingInvocation = invocation.HasAnyOperation
                ? invocation
                : invocation with { Show = true };

            CommandLineIpcResponse? response = null;
            try
            {
                response = CommandLineIpcClient
                    .SendAsync(forwardingInvocation, TimeSpan.FromSeconds(3))
                    .ConfigureAwait(false)
                    .GetAwaiter()
                    .GetResult();
            }
            catch (Exception)
            {
                // 既存インスタンスへの接続に失敗した場合は例外を吸収して
                // 当インスタンスを主実行体として起動し、起動引数をローカルで処理します。
            }

            if (response is null)
            {
                // 既存インスタンスが起動中でない/到達不可なら、ローカル起動して実行します。
                Environment.ExitCode = CommandLineExitCodes.TargetNotFound;
            }
            else
            {
                Environment.ExitCode = response.ExitCode;
                if (response.ExitCode != CommandLineExitCodes.Success && !invocation.SilentErrors)
                {
                    MessageBox.Show(response.Message, "AutoTool", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }

            Shutdown();
            return;
        }

        // 例外通知で使う依存は明示的に組み立てて共有し、Service Locator を回避します。
        var asyncFileLog = new AsyncFileLog();
        var logWriter = new DelegatingLogWriter(asyncFileLog);
        var notifier = new WpfNotifier(new WpfAppDialogService(), logWriter);

        _logWriter = logWriter;
        _notifier = notifier;

        // ホストを構築して初期化
        _host = AppHostBuilder
            .CreateHostBuilder(invocation, e.Args)
            .ConfigureServices((_, services) =>
            {
                services.AddSingleton(asyncFileLog);
                services.AddSingleton<ILogWriter>(logWriter);
                services.AddSingleton<INotifier>(notifier);
            })
            .Build();

        // UIスレッドで発生した未処理の例外をキャッチ
        DispatcherUnhandledException += App_DispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

        base.OnStartup(e);

        // ホストを開始
        _host.Start();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        DispatcherUnhandledException -= App_DispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException -= CurrentDomain_UnhandledException;
        TaskScheduler.UnobservedTaskException -= TaskScheduler_UnobservedTaskException;

        // グローバルフックが残っていても必ず解除する
        Win32KeyboardHookHelper.StopHook();
        Win32MouseHookHelper.StopHook();

        // UIスレッドの同期コンテキストに依存せず停止処理を完了させる
        if (_host is not null)
        {
            try
            {
                _host.StopAsync().ConfigureAwait(false).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _logWriter?.Write(ex);
            }
            finally
            {
                _host.Dispose();
            }
        }

        _singleInstanceMutex?.Dispose();
        _singleInstanceMutex = null;

        base.OnExit(e);
    }

    private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        var exception = e.Exception;
        var crashLogPath = WriteCrashReport(exception, "DispatcherUnhandledException", terminating: false);
        _logWriter?.Write(exception);
        _logWriter?.Write(
            "CRASH",
            "Source=DispatcherUnhandledException",
            "Terminating=False",
            $"CrashLogPath={crashLogPath ?? "（出力失敗）"}");

        if (IsCriticalException(exception))
        {
            _notifier?.ShowError(
                $"致命的なエラーが発生したためアプリケーションを終了します。\nログ: {crashLogPath ?? "Logs フォルダを確認してください"}",
                "致命的エラー");
            e.Handled = false;
            return;
        }

        _notifier?.ShowError(
            $"予期しないエラーが発生しました: {exception.Message}\nログ: {crashLogPath ?? "Logs フォルダを確認してください"}",
            "エラー");

        // 例外をハンドル済みとして設定
        e.Handled = true;
    }

    private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is not Exception exception)
        {
            return;
        }

        var crashLogPath = WriteCrashReport(exception, "AppDomain.UnhandledException", e.IsTerminating);
        _logWriter?.Write(exception);
        _logWriter?.Write(
            "CRASH",
            "Source=AppDomain.UnhandledException",
            $"Terminating={e.IsTerminating}",
            $"CrashLogPath={crashLogPath ?? "（出力失敗）"}");
    }

    private void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        var exception = e.Exception;
        var crashLogPath = WriteCrashReport(exception, "TaskScheduler.UnobservedTaskException", terminating: false);
        _logWriter?.Write(exception);
        _logWriter?.Write(
            "CRASH",
            "Source=TaskScheduler.UnobservedTaskException",
            "Terminating=False",
            $"CrashLogPath={crashLogPath ?? "（出力失敗）"}");
        e.SetObserved();
    }

    private static string? WriteCrashReport(Exception exception, string source, bool terminating)
    {
        ArgumentNullException.ThrowIfNull(exception);
        ArgumentNullException.ThrowIfNull(source);

        try
        {
            lock (CrashLogSync)
            {
                var logDir = Path.Combine(AppContext.BaseDirectory, "Logs");
                Directory.CreateDirectory(logDir);
                var path = Path.Combine(logDir, $"crash_{DateTimeOffset.Now:yyyy-MM-dd_HH-mm-ss-fff}.log");

                using var writer = new StreamWriter(path, append: false, encoding: new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
                writer.WriteLine($"Timestamp: {DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss.fff zzz}");
                writer.WriteLine($"Source: {source}");
                writer.WriteLine($"Terminating: {terminating}");
                writer.WriteLine($"Process: {Environment.ProcessPath}");
                writer.WriteLine($"AppBase: {AppContext.BaseDirectory}");
                writer.WriteLine($"ThreadId: {Environment.CurrentManagedThreadId}");
                writer.WriteLine($"OS: {Environment.OSVersion}");
                writer.WriteLine($".NET: {Environment.Version}");
                writer.WriteLine($"ExceptionSummary: {ExceptionDetailsFormatter.FormatDetailed(exception)}");
                writer.WriteLine();
                writer.WriteLine("Exception.ToString():");
                writer.WriteLine(exception.ToString());
                writer.WriteLine();
                writer.WriteLine("ExceptionChain:");

                var chain = ExceptionDetailsFormatter.GetExceptionChain(exception);
                for (var i = 0; i < chain.Count; i++)
                {
                    var current = chain[i];
                    writer.WriteLine($"[{i}] Type={current.GetType().FullName}");
                    writer.WriteLine($"[{i}] Message={current.Message}");
                    writer.WriteLine($"[{i}] HResult=0x{current.HResult:X8}");
                    if (current.Data.Count > 0)
                    {
                        writer.WriteLine($"[{i}] Data:");
                        foreach (var key in current.Data.Keys)
                        {
                            writer.WriteLine($"  - {key}={current.Data[key]}");
                        }
                    }

                    writer.WriteLine($"[{i}] StackTrace:");
                    writer.WriteLine(current.StackTrace ?? "(none)");
                    writer.WriteLine();
                }

                return path;
            }
        }
        catch
        {
            return null;
        }
    }

    private static bool IsCriticalException(Exception exception) =>
        exception is OutOfMemoryException
            or AccessViolationException
            or AppDomainUnloadedException
            or BadImageFormatException
            or CannotUnloadAppDomainException
            or InvalidProgramException
            or StackOverflowException;

    private static bool TryAcquireSingleInstanceMutex(out Mutex? mutex)
    {
        mutex = null;
        try
        {
            mutex = new Mutex(initiallyOwned: true, name: SingleInstanceMutexName, createdNew: out var createdNew);
            if (!createdNew)
            {
                mutex.Dispose();
                mutex = null;
                return false;
            }

            return true;
        }
        catch
        {
            mutex?.Dispose();
            mutex = null;
            return false;
        }
    }
}



