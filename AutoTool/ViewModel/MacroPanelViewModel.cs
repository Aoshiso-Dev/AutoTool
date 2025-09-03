using System;
using Microsoft.Extensions.Logging;

namespace AutoTool.ViewModel
{
    /// <summary>
    /// MacroPanelViewModel（将来の拡張用）
    /// </summary>
    public class MacroPanelViewModel
    {
        private readonly ILogger<MacroPanelViewModel> _logger;

        public MacroPanelViewModel(ILogger<MacroPanelViewModel> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logger.LogInformation("MacroPanelViewModel初期化（将来の拡張用）");
        }
    }
}