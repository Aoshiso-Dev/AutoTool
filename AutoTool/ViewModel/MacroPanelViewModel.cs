using System;
using Microsoft.Extensions.Logging;

namespace AutoTool.ViewModel
{
    /// <summary>
    /// MacroPanelViewModel�i�����̊g���p�j
    /// </summary>
    public class MacroPanelViewModel
    {
        private readonly ILogger<MacroPanelViewModel> _logger;

        public MacroPanelViewModel(ILogger<MacroPanelViewModel> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logger.LogInformation("MacroPanelViewModel�������i�����̊g���p�j");
        }
    }
}