using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MacroPanels.Message;

namespace MacroPanels.ViewModel
{

    public partial class LogPanelViewModel : ObservableObject
    {
        [ObservableProperty]
        private bool _isRunning;

        [ObservableProperty]
        private string _log = string.Empty;

        public LogPanelViewModel()
        {
        }

        public void SetRunningState(bool isRunning)
        {
            IsRunning = isRunning;
        }

        public void Prepare()
        {
            Log = string.Empty;
        }
        
        public void WriteLog(string text)
        {
            Log += text + Environment.NewLine;
        }
    }
}