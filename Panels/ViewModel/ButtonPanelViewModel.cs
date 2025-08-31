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
using MacroPanels.ViewModel.Shared;

namespace MacroPanels.ViewModel
{
    public partial class ButtonPanelViewModel : ObservableObject
    {
        [ObservableProperty]
        private bool _isRunning = false;

        [ObservableProperty]
        private ObservableCollection<CommandDisplayItem> _itemTypes = new();

        [ObservableProperty]
        private CommandDisplayItem? _selectedItemType;

        public ButtonPanelViewModel()
        {
            InitializeItemTypes();
        }

        private void InitializeItemTypes()
        {
            // CommandRegistryを初期化
            CommandRegistry.Initialize();
            
            // 日本語表示名付きのアイテムを作成
            var displayItems = CommandRegistry.GetOrderedTypeNames()
                .Select(typeName => new CommandDisplayItem
                {
                    TypeName = typeName,
                    DisplayName = CommandRegistry.DisplayOrder.GetDisplayName(typeName),
                    Category = CommandRegistry.DisplayOrder.GetCategoryName(typeName)
                })
                .ToList();
            
            ItemTypes = new ObservableCollection<CommandDisplayItem>(displayItems);
            SelectedItemType = ItemTypes.FirstOrDefault();
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
        public void Save() => WeakReferenceMessenger.Default.Send(new SaveMessage());

        [RelayCommand]
        public void Load() => WeakReferenceMessenger.Default.Send(new LoadMessage());

        [RelayCommand]
        public void Clear() => WeakReferenceMessenger.Default.Send(new ClearMessage());

        [RelayCommand]
        public void Add() 
        {
            if (SelectedItemType != null)
            {
                WeakReferenceMessenger.Default.Send(new AddMessage(SelectedItemType.TypeName));
            }
        }

        [RelayCommand]
        public void Up() => WeakReferenceMessenger.Default.Send(new UpMessage());

        [RelayCommand]
        public void Down() => WeakReferenceMessenger.Default.Send(new DownMessage());

        [RelayCommand]
        public void Delete() => WeakReferenceMessenger.Default.Send(new DeleteMessage());

        public void SetRunningState(bool isRunning) => IsRunning = isRunning;

        public void Prepare() { }
    }
}