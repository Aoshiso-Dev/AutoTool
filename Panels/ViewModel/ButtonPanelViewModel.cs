using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using MacroPanels.Message;
using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.Input;
using System.Security.Cryptography.X509Certificates;
using MacroPanels.List.Class;
using MacroPanels.Model.CommandDefinition;

namespace MacroPanels.ViewModel
{
    public partial class ButtonPanelViewModel : ObservableObject
    {
        [ObservableProperty]
        private bool _isRunning = false;

        [ObservableProperty]
        private ObservableCollection<string> _itemTypes = new();

        [ObservableProperty]
        private string _selectedItemType;

        public ButtonPanelViewModel()
        {
            // CommandRegistryを初期化
            CommandRegistry.Initialize();
            
            foreach(var type in CommandRegistry.GetAllTypeNames())
            {
                ItemTypes.Add(type);
            }

            SelectedItemType = ItemTypes.FirstOrDefault() ?? string.Empty;
        }

        [RelayCommand]
        public void Run()
        {
            if (IsRunning)
            {
                WeakReferenceMessenger.Default.Send(new StopMessage());
            }
            else
            {
                WeakReferenceMessenger.Default.Send(new RunMessage());
            }
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

        [RelayCommand]
        public void Up()
        {
            WeakReferenceMessenger.Default.Send(new UpMessage());
        }

        [RelayCommand]
        public void Down()
        {
            WeakReferenceMessenger.Default.Send(new DownMessage());
        }

        [RelayCommand]
        public void Delete()
        {
            WeakReferenceMessenger.Default.Send(new DeleteMessage());
        }
        public void SetRunningState(bool isRunning)
        {
            IsRunning = isRunning;
        }

        public void Prepare()
        {
        }
    }
}