using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MacroPanels.Message;
using CommunityToolkit.Mvvm.Input;
using LogHelper;

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
            Log += $"[{ DateTime.Now:yyyy - MM - dd HH: mm: ss}] {text} {Environment.NewLine}";
        }
        
        public void WriteLog(string str1, string str2, string str3)
        {
            Log += $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {str1.PadRight(20)} {str2.PadRight(20)} {str3} {Environment.NewLine}";
        }
    }
}