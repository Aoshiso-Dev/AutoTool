using System.Drawing;

namespace AutoTool.Desktop.Panels.Services;

public interface IDetectionHighlightService
{
    Task BlinkAsync(Rectangle bounds, CancellationToken cancellationToken = default);
}
