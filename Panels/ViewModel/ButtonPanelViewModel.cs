using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using Panels.Message;
using System.Collections.ObjectModel;
using Panels.Model.List.Type;
using System.Windows;
using CommunityToolkit.Mvvm.Input;

namespace Panels.ViewModel
{
    public partial class ButtonPanelViewModel : ObservableObject
    {
        private bool _isRunning = false;

        [ObservableProperty]
        private string _runButtonText = "Run";

        [ObservableProperty]
        private Brush _runButtonColor = Brushes.Green;

        [ObservableProperty]
        private ObservableCollection<string> _itemTypes = new ObservableCollection<string>();

        [ObservableProperty]
        private string _selectedItemType;


        public ButtonPanelViewModel()
        {
            foreach(var type in ItemType.GetTypes())
            {
                ItemTypes.Add(type);
            }

            SelectedItemType = ItemTypes.FirstOrDefault() ?? string.Empty;
        }

        [RelayCommand]
        public void Run()
        {
            WeakReferenceMessenger.Default.Send(new RunMessage());

            RunButtonColorChange();
            RunButtonTextChange();
        }

        [RelayCommand]
        public void Save()
        {
            WeakReferenceMessenger.Default.Send(new SaveMessage());
        }

        [RelayCommand]
        public void Load()
        {
            WeakReferenceMessenger.Default.Send(new LoadMessage());
        }

        [RelayCommand]
        public void Clear()
        {
            WeakReferenceMessenger.Default.Send(new ClearMessage());
        }

        [RelayCommand]
        public void Add()
        {
            WeakReferenceMessenger.Default.Send(new AddMessage(SelectedItemType));
        }

        public void RunButtonColorChange()
        {
            if (RunButtonColor == Brushes.Green)
            {
                RunButtonColor = Brushes.Red;
            }
            else
            {
                RunButtonColor = Brushes.Green;
            }
        }

        public void RunButtonTextChange()
        {
            if (RunButtonText == "Run")
            {
                RunButtonText = "Stop";
            }
            else
            {
                RunButtonText = "Run";
            }
        }



    }
}