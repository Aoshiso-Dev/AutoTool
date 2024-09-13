﻿using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Panels.View;
using Panels.List.Class;
using System.Windows.Input;
using System.IO;
using Panels.Model.MacroFactory;
using Panels.Model.List.Interface;
using Panels.Model.List.Type;
using Panels.Message;
using CommunityToolkit.Mvvm.Messaging;
using Panels.Model;


namespace Panels.ViewModel
{
    public partial class ListPanelViewModel : ObservableObject
    {
        [ObservableProperty]
        private CommandList _commandList = new();

        private int _selectedLineNumber = 0;
        public int SelectedLineNumber
        {
            get => _selectedLineNumber;
            set
            {
                SetProperty(ref _selectedLineNumber, value);
                OnUpdateSelectedLineNumber();
            }
        }

        //[ObservableProperty]
        private int _executedLineNumber = 0;
        public int ExecutedLineNumber
        {
            get => _executedLineNumber;
            set
            {
                SetProperty(ref _executedLineNumber, value);
                CommandList.Items.ToList().ForEach(x => x.IsRunning = false);
                var cmd = CommandList.Items.Where(x => x.LineNumber == value).FirstOrDefault();
                if (cmd != null)
                {
                    cmd.IsRunning = true;
                }
            }
        }


        public ListPanelViewModel()
        {
        }

        [RelayCommand]
        public void Add(string itemType)
        {
            ICommandListItem? item = itemType switch
            {
                nameof(ItemType.WaitImage) => new WaitImageItem(),
                nameof(ItemType.ClickImage) => new ClickImageItem(),
                nameof(ItemType.Click) => new ClickItem(),
                nameof(ItemType.Hotkey) => new HotkeyItem(),
                nameof(ItemType.Wait) => new WaitItem(),
                nameof(ItemType.Loop) => new LoopItem(),
                nameof(ItemType.EndLoop) => new EndLoopItem(),
                nameof(ItemType.Break) => new BreakItem(),
                nameof(ItemType.IfImageExist) => new IfImageExistItem(),
                nameof(ItemType.IfImageNotExist) => new IfImageNotExistItem(),
                nameof(ItemType.EndIf) => new EndIfItem(),
                _ => null
            };

            if (item != null)
            {
                item.LineNumber = CommandList.Items.Count + 1;
                item.ItemType = itemType;
                CommandList.Add(item);
            }
        }

        [RelayCommand]
        public void Up()
        {
            if(SelectedLineNumber == 0)
            {
                return;
            }

            var selectedBak = SelectedLineNumber;
            CommandList.Move(SelectedLineNumber, SelectedLineNumber - 1);
            SelectedLineNumber = selectedBak - 1;
        }

        [RelayCommand]
        public void Down()
        {
            if(SelectedLineNumber == CommandList.Items.Count - 1)
            {
                return;
            }

            var selectedBak = SelectedLineNumber;
            CommandList.Move(SelectedLineNumber, SelectedLineNumber + 1);
            SelectedLineNumber = selectedBak + 1;
        }

        [RelayCommand]
        public void Delete()
        {
            var item = CommandList.Items.FirstOrDefault(x => x.LineNumber == SelectedLineNumber + 1);
            if (item != null)
            {
                var index = CommandList.Items.IndexOf(item);
                CommandList.Items.RemoveAt(index);

                // LineNumberを振り直す
                for (int i = index; i < CommandList.Items.Count; i++)
                {
                    CommandList.Items[i].LineNumber = i + 1;
                }

                // 選択行を変更する
                if (CommandList.Items.Count == 0)
                {
                    SelectedLineNumber = 0;
                }
                else if (index == CommandList.Items.Count)
                {
                    SelectedLineNumber = index - 1;
                }
                else
                {
                    SelectedLineNumber = index;
                }
            }
        }

        private void OnUpdateSelectedLineNumber()
        {
            var existingItem = CommandList.Items.FirstOrDefault(x => x.LineNumber == SelectedLineNumber + 1);
            if (existingItem != null)
            {
                WeakReferenceMessenger.Default.Send(new SelectMessage(existingItem));
            }
        }

        public void UpdateSelectedItem(ICommandListItem item)
        {
            var existingItem = CommandList.Items.FirstOrDefault(x => x.LineNumber == item.LineNumber);
            if (existingItem != null)
            {
                var index = CommandList.Items.IndexOf(existingItem);
                CommandList.Items[index] = item;

                // Descriptionを更新する
                existingItem.Description = item.Description;
            }
        }
    }
}
