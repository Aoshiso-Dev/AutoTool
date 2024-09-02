using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

internal class WaitHelper
{
    public static void Wait(int timeoutMs, CancellationToken cancellationToken)
    {
        var timeout = TimeSpan.FromMilliseconds(timeoutMs);
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        while (stopwatch.Elapsed < timeout)
        {
            // キャンセルがリクエストされたかを確認
            if (cancellationToken.IsCancellationRequested)
            {
                break; // ループを終了
            }

            Thread.Sleep(100);
        }
    }
}
