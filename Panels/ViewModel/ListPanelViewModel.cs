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

        private ICommandListItem? _selectedItem = null;
        public ICommandListItem? SelectedItem
        {
            get => _selectedItem;
            set
            {
                SetProperty(ref _selectedItem, value);
                OnSelectedItemChanged();
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
            var existingItem = CommandList.Items.FirstOrDefault(x => x.LineNumber == SelectedLineNumber + 1);
            WeakReferenceMessenger.Default.Send(new ChangeSelectedMessage(existingItem));
        }

        private void OnSelectedItemChanged()
        {
            WeakReferenceMessenger.Default.Send(new ChangeSelectedMessage(SelectedItem));
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

                if(CommandList.Items.Count != 0)
                {
                    CommandList.Insert(SelectedLineNumber, item);
                }
                else
                {
                    CommandList.Add(item);
                }

                SetSelectedLineNumber(CommandList.Items.IndexOf(item));
                SetSelectedItem(item);

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
            SetSelectedLineNumber(selectedBak - 1);
        }

        public void Down()
        {
            if(SelectedLineNumber == CommandList.Items.Count - 1)
            {
                return;
            }

            var selectedBak = SelectedLineNumber;
            CommandList.Move(SelectedLineNumber, SelectedLineNumber + 1);
            SetSelectedLineNumber(selectedBak + 1);
        }

        public void Delete()
        {
            if (SelectedItem == null)
            {
                return;
            }

            // 選択行を変更する
            var index = CommandList.Items.IndexOf(SelectedItem);
            if (CommandList.Items.Count == 0)
            {
                SetSelectedLineNumber(0);
            }
            else if (index == CommandList.Items.Count)
            {
                SetSelectedLineNumber(index - 1);
            }
            else
            {
                SetSelectedLineNumber(index);
            }
            
            // 削除
            CommandList.Remove(SelectedItem);
        }

        public void Clear()
        {
            CommandList.Clear();
            SetSelectedLineNumber(0);
            SelectedItem = null;
        }

        public void Save()
        {
            CommandList.Save();
        }

        public void Load()
        {
            CommandList.Load();
            SetSelectedLineNumber(0);
            SelectedItem = null;
        }
        #endregion

        #region Call from MainWindowViewModel
        public int GetCount()
        {
            return CommandList.Items.Count;
        }

        public ICommandListItem? GetExecutedItem()
        {
            return CommandList.Items.Where(x => x.IsRunning == IsRunning).FirstOrDefault();
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
