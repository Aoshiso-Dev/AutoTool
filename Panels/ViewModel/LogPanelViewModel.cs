using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Panels.Message;

namespace Panels.ViewModel
{

    public partial class LogPanelViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _log = string.Empty;

        public LogPanelViewModel()
        {
            WeakReferenceMessenger.Default.Register<LogMessage>(this, (sender, message) =>
            {
                Log += message.Text + Environment.NewLine;
            });
        }
    }
}