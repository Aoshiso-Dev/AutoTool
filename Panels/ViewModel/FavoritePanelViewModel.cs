using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Panels.Message;
using System.Collections.ObjectModel;

namespace Panels.ViewModel
{

    public partial class FavoritePanelViewModel : ObservableObject
    {
        [ObservableProperty]
        private bool _isRunning = false;

        [ObservableProperty]
        private ObservableCollection<string> _favoriteList = new();
        
        public FavoritePanelViewModel()
        {
            FavoriteList.Add("test1");
            FavoriteList.Add("test2");
            FavoriteList.Add("test3");
            FavoriteList.Add("test3");

        }
    }
}