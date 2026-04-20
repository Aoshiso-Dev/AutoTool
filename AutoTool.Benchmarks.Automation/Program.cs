using BenchmarkDotNet.Running;

namespace AutoTool.Automation.Runtime.Benchmarks;

/// <summary>
/// アプリケーション実行の開始点となり、起動処理を呼び出します。
/// </summary>
public static class Program
{
    public static void Main(string[] args)
    {
        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
    }
}
