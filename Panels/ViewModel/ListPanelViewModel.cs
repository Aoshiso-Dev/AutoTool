using System.Collections.ObjectModel;
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
using System.Windows.Data;
using System.Security.Cryptography.X509Certificates;


namespace Panels.ViewModel
{
    public partial class ListPanelViewModel : ObservableObject
    {
        #region Properties
        [ObservableProperty]
        private bool _isRunning;

        [ObservableProperty]
        private CommandList _commandList = new();

        private int _selectedLineNumber = 0;
        public int SelectedLineNumber
        {
            get => _selectedLineNumber;
            set
            {
                SetProperty(ref _selectedLineNumber, value);
                OnSelectedLineNumberChanged();
            }
        }

        public ICommandListItem? SelectedItem
        {
            get
            {
                return CommandList.Items.FirstOrDefault(x => x.IsSelected == true);
            }
            set
            {
                var existingItem = CommandList.Items.FirstOrDefault(x => x.IsSelected == true);

                var index = CommandList.Items.IndexOf(existingItem);

                CommandList.Override(index, value);

                CollectionViewSource.GetDefaultView(CommandList.Items).Refresh();
            }
        }

        private int _executedLineNumber = 0;
        public int ExecutedLineNumber
        {
            get => _executedLineNumber;
            set
            {
                SetProperty(ref _executedLineNumber, value);
                OnExecutedLineNumberChanged();
            }
        }
        #endregion

        public ListPanelViewModel()
        {
        }


        #region OnChanged
        private void OnSelectedLineNumberChanged()
        {
            CommandList.Items.ToList().ForEach(x => x.IsSelected = false);

            var existingItem = CommandList.Items.FirstOrDefault(x => x.LineNumber == SelectedLineNumber + 1);
            if (existingItem != null)
            {
                existingItem.IsSelected = true;
                WeakReferenceMessenger.Default.Send(new ChangeSelectedMessage(existingItem));
            }
        }

        private void OnExecutedLineNumberChanged()
        {
            CommandList.Items.ToList().ForEach(x => x.IsRunning = false);
            var cmd = CommandList.Items.Where(x => x.LineNumber == ExecutedLineNumber).FirstOrDefault();
            if (cmd != null)
            {
                cmd.IsRunning = true;
                CollectionViewSource.GetDefaultView(CommandList.Items).Refresh();
            }
        }
        #endregion

        #region ListIntaraction
        public void Refresh()
        {
            CollectionViewSource.GetDefaultView(CommandList.Items).Refresh();
        }

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
                item.ItemType = itemType;

                if(CommandList.Items.Count != 0 && SelectedLineNumber >= 0)
                {
                    CommandList.Insert(SelectedLineNumber + 1, item);
                }
                else
                {
                    CommandList.Add(item);
                }

                SelectedLineNumber = CommandList.Items.IndexOf(item);

                CollectionViewSource.GetDefaultView(CommandList.Items).Refresh();
            }
        }

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

        public void Delete()
        {
            if (SelectedItem == null)
            {
                return;
            }

            var index = CommandList.Items.IndexOf(SelectedItem);

            CommandList.Remove(SelectedItem);

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

        public void Clear()
        {
            CommandList.Clear();
            SelectedLineNumber = 0;
        }

        public void Save()
        {
            CommandList.Save();
        }

        public void Load()
        {
            CommandList.Load();
            SelectedLineNumber = 0;
            SelectedItem = CommandList.Items.FirstOrDefault();
        }
        #endregion

        #region Call from MainWindowViewModel
        public int GetCount()
        {
            return CommandList.Items.Count;
        }

        public ICommandListItem? GetRunningItem()
        {
            return CommandList.Items.FirstOrDefault(x => x.IsRunning == true);
        }

        public ICommandListItem? GetItem(int lineNumber)
        {
            return CommandList.Items.FirstOrDefault(x => x.LineNumber == lineNumber);
        }

        public void SetRunningState(bool isRunning)
        {
            IsRunning = isRunning;
        }

        public void SetSelectedItem(ICommandListItem? item)
        {
            SelectedItem = item;
        }

        public void SetSelectedLineNumber(int lineNumber)
        {
            SelectedLineNumber = lineNumber;
        }

        public void Prepare()
        {
            CommandList.Items.ToList().ForEach(x => x.IsRunning = false);
            CommandList.Items.ToList().ForEach(x => x.Progress = 0);

            CollectionViewSource.GetDefaultView(CommandList.Items).Refresh();
        }
        #endregion
    }
}
