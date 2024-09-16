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

    public partial class FavoritePanelViewModel : ObservableObject
    {
        [ObservableProperty]
        private bool _isRunning = false;

        public FavoritePanelViewModel()
        {
        }
    }
}