using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Input;
using MacroPanels.List.Class;
using MacroPanels.Command.Class;
using MacroPanels.Command.Interface;
using MacroPanels.Command.Message;
using System.Windows.Controls;
using System.Windows.Data;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.IO;
using System.Xml.Serialization;
using System.Windows.Shapes;
using System.Security.Policy;

using MacroPanels.ViewModel;
using MacroPanels.Message;
using MacroPanels.Model.MacroFactory;
using MacroPanels.Model.List.Interface;
using static System.Runtime.InteropServices.JavaScript.JSType;
using static System.Net.Mime.MediaTypeNames;
using AutoTool.Model;
using AutoTool.Services.Interfaces;

namespace AutoTool.ViewModel
{
    public partial class MacroPanelViewModel : ObservableObject
    {
        private readonly INotificationService _notificationService;
        private readonly ILogService _logService;
        private CancellationTokenSource? _cts;
        private CommandHistoryManager? _commandHistory;

        [ObservableProperty]
        private bool _isRunning = false;

        [ObservableProperty]
        private ButtonPanelViewModel _buttonPanelViewModel;

        [ObservableProperty]
        private ListPanelViewModel _listPanelViewModel;

        [ObservableProperty]
        private EditPanelViewModel _editPanelViewModel;

        [ObservableProperty]
        private LogPanelViewModel _logPanelViewModel;

        [ObservableProperty]
        private FavoritePanelViewModel _favoritePanelViewModel;

        [ObservableProperty]
        private int _selectedListTabIndex = 0;

        public MacroPanelViewModel(INotificationService notificationService, ILogService logService)
        {
            _notificationService = notificationService;
            _logService = logService;
            ListPanelViewModel = new ListPanelViewModel();
            EditPanelViewModel = new EditPanelViewModel();
            ButtonPanelViewModel = new ButtonPanelViewModel();
            LogPanelViewModel = new LogPanelViewModel();
            FavoritePanelViewModel = new FavoritePanelViewModel();

            RegisterMessages();
        }

        /// <summary>
        /// CommandHistoryManagerを設定
        /// </summary>
        public void SetCommandHistory(CommandHistoryManager commandHistory)
        {
            _commandHistory = commandHistory;
            
            // ListPanelViewModelにも設定
            ListPanelViewModel.SetCommandHistory(commandHistory);
        }

        private void RegisterMessages()
        {
            // From ButtonPanelViewModel
            WeakReferenceMessenger.Default.Register<RunMessage>(this, async (sender, message) =>
            {
                ListPanelViewModel.Prepare();
                EditPanelViewModel.Prepare();
                LogPanelViewModel.Prepare();
                FavoritePanelViewModel.Prepare();
                ButtonPanelViewModel.Prepare();

                ListPanelViewModel.SetRunningState(true);
                EditPanelViewModel.SetRunningState(true);
                FavoritePanelViewModel.SetRunningState(true);
                LogPanelViewModel.SetRunningState(true);
                ButtonPanelViewModel.SetRunningState(true);

                await Run();
            });
            WeakReferenceMessenger.Default.Register<StopMessage>(this, (sender, message) =>
            {
                ListPanelViewModel.SetRunningState(false);
                EditPanelViewModel.SetRunningState(false);
                FavoritePanelViewModel.SetRunningState(false);
                LogPanelViewModel.SetRunningState(false);
                ButtonPanelViewModel.SetRunningState(false);

                _cts?.Cancel();
            });
            WeakReferenceMessenger.Default.Register<SaveMessage>(this, (sender, message) =>
            {
                ListPanelViewModel.Save();
            });
            WeakReferenceMessenger.Default.Register<LoadMessage>(this, (sender, message) =>
            {
                ListPanelViewModel.Load();
                EditPanelViewModel.SetListCount(ListPanelViewModel.GetCount());
                
                // ファイル読み込み後は履歴をクリア
                _commandHistory?.Clear();
            });
            WeakReferenceMessenger.Default.Register<ClearMessage>(this, (sender, message) =>
            {
                // クリア操作をUndoスタックに追加
                if (_commandHistory != null)
                {
                    var clearCommand = new ClearAllCommand(
                        ListPanelViewModel.CommandList.Items.ToList(),
                        () => ListPanelViewModel.Clear(),
                        (items) => RestoreItems(items)
                    );
                    _commandHistory.ExecuteCommand(clearCommand);
                }
                else
                {
                    ListPanelViewModel.Clear();
                }
                
                EditPanelViewModel.SetListCount(ListPanelViewModel.GetCount());
            });
            WeakReferenceMessenger.Default.Register<AddMessage>(this, (sender, message) =>
            {
                var itemType = (message as AddMessage).ItemType;
                
                // 追加操作をUndoスタックに追加
                if (_commandHistory != null)
                {
                    // 新しいアイテムを作成
                    var newItem = MacroPanels.Model.CommandDefinition.CommandRegistry.CreateCommandItem(itemType);
                    if (newItem != null)
                    {
                        var targetIndex = ListPanelViewModel.SelectedLineNumber + 1;
                        var addCommand = new AddItemCommand(
                            newItem,
                            targetIndex,
                            (item, index) => ListPanelViewModel.InsertAt(index, item),
                            (index) => ListPanelViewModel.RemoveAt(index)
                        );
                        _commandHistory.ExecuteCommand(addCommand);
                    }
                }
                else
                {
                    ListPanelViewModel.Add(itemType);
                }
                
                EditPanelViewModel.SetListCount(ListPanelViewModel.GetCount());
            });
            WeakReferenceMessenger.Default.Register<UpMessage>(this, (sender, message) =>
            {
                var fromIndex = ListPanelViewModel.SelectedLineNumber;
                var toIndex = fromIndex - 1;
                
                if (toIndex >= 0 && _commandHistory != null)
                {
                    var moveCommand = new MoveItemCommand(
                        fromIndex, toIndex,
                        (from, to) => ListPanelViewModel.MoveItem(from, to)
                    );
                    _commandHistory.ExecuteCommand(moveCommand);
                }
                else
                {
                    ListPanelViewModel.Up();
                }
            });
            WeakReferenceMessenger.Default.Register<DownMessage>(this, (sender, message) =>
            {
                var fromIndex = ListPanelViewModel.SelectedLineNumber;
                var toIndex = fromIndex + 1;
                
                if (toIndex < ListPanelViewModel.GetCount() && _commandHistory != null)
                {
                    var moveCommand = new MoveItemCommand(
                        fromIndex, toIndex,
                        (from, to) => ListPanelViewModel.MoveItem(from, to)
                    );
                    _commandHistory.ExecuteCommand(moveCommand);
                }
                else
                {
                    ListPanelViewModel.Down();
                }
            });
            WeakReferenceMessenger.Default.Register<DeleteMessage>(this, (sender, message) =>
            {
                var selectedItem = ListPanelViewModel.SelectedItem;
                var selectedIndex = ListPanelViewModel.SelectedLineNumber;
                
                if (selectedItem != null && _commandHistory != null)
                {
                    var removeCommand = new RemoveItemCommand(
                        selectedItem.Clone(),
                        selectedIndex,
                        (item, index) => ListPanelViewModel.InsertAt(index, item),
                        (index) => ListPanelViewModel.RemoveAt(index)
                    );
                    _commandHistory.ExecuteCommand(removeCommand);
                }
                else
                {
                    ListPanelViewModel.Delete();
                }
                
                EditPanelViewModel.SetListCount(ListPanelViewModel.GetCount());
            });

            // From ListPanelViewModel
            WeakReferenceMessenger.Default.Register<ChangeSelectedMessage>(this, (sender, message) =>
            {
                EditPanelViewModel.SetItem((message as ChangeSelectedMessage).Item);
            });

            // From EditPanelViewModel
            WeakReferenceMessenger.Default.Register<EditCommandMessage>(this, (sender, message) =>
            {
                var item = (message as EditCommandMessage).Item;
                if (item != null)
                {
                    var oldItem = ListPanelViewModel.SelectedItem;
                    var index = item.LineNumber - 1;
                    
                    // 編集操作をUndoスタックに追加
                    if (oldItem != null && _commandHistory != null)
                    {
                        var editCommand = new EditItemCommand(
                            oldItem, item, index,
                            (editedItem, editIndex) => ListPanelViewModel.ReplaceAt(editIndex, editedItem)
                        );
                        _commandHistory.ExecuteCommand(editCommand);
                    }
                    else
                    {
                        ListPanelViewModel.SetSelectedItem(item);
                        ListPanelViewModel.SetSelectedLineNumber(item.LineNumber - 1);
                    }
                }
            });
            WeakReferenceMessenger.Default.Register<RefreshListViewMessage>(this, (sender, message) =>
            {
                ListPanelViewModel.Refresh();
            });

            // From Commands
            WeakReferenceMessenger.Default.Register<StartCommandMessage>(this, (sender, message) =>
            {
                var command = (message as StartCommandMessage).Command;
                var lineNumber = command.LineNumber.ToString().PadLeft(2, ' ');
                var commandName = command.GetType().ToString().Split('.').Last().Replace("Command", "").PadRight(20, ' ');

                var settingDict = command.Settings.GetType().GetProperties().ToDictionary(x => x.Name, x => x.GetValue(command.Settings, null));
                var logString = string.Empty;
                foreach (var setting in settingDict)
                {
                    logString += $"({setting.Key} = {setting.Value}), ";
                }

                LogPanelViewModel.WriteLog(lineNumber, commandName, logString);

                _logService.Write(lineNumber, commandName, logString);

                var commandItem = ListPanelViewModel.GetItem(command.LineNumber);

                if (commandItem != null)
                {
                    commandItem.Progress = 0;
                    commandItem.IsRunning = true;
                }
            });
            WeakReferenceMessenger.Default.Register<FinishCommandMessage>(this, (sender, message) =>
            {
                var command = (message as FinishCommandMessage).Command;
                var commandItem = ListPanelViewModel.GetItem(command.LineNumber);

                if (commandItem != null)
                {
                    commandItem.Progress = 0;
                    commandItem.IsRunning = false;
                }
            });
            WeakReferenceMessenger.Default.Register<DoingCommandMessage>(this, (sender, message) =>
            {
                var command = (message as DoingCommandMessage).Command;
                var lineNumber = command.LineNumber.ToString().PadLeft(2, ' ');
                var commandName = command.GetType().ToString().Split('.').Last().Replace("Command", "").PadRight(20, ' ');
                var detail = (message as DoingCommandMessage).Detail;
                LogPanelViewModel.WriteLog(lineNumber, commandName, detail);

                _logService.Write(lineNumber, commandName, detail);
            });
            WeakReferenceMessenger.Default.Register<UpdateProgressMessage>(this, (sender, message) =>
            {
                var command = (message as UpdateProgressMessage).Command;
                var progress = (message as UpdateProgressMessage).Progress;

                var commandItem = ListPanelViewModel.GetItem(command.LineNumber);

                if (commandItem != null)
                {
                    commandItem.Progress = progress;
                }
            });

            // From Other
            WeakReferenceMessenger.Default.Register<LogMessage>(this, (sender, message) =>
            {
                LogPanelViewModel.WriteLog((message as LogMessage).Text);

                _logService.Write((message as LogMessage).Text);
            });
        }

        /// <summary>
        /// アイテムリストを復元（Undo用）
        /// </summary>
        private void RestoreItems(IEnumerable<ICommandListItem> items)
        {
            ListPanelViewModel.Clear();
            foreach (var item in items)
            {
                ListPanelViewModel.AddItem(item.Clone());
            }
            EditPanelViewModel.SetListCount(ListPanelViewModel.GetCount());
        }

        public async Task Run()
        {
            var listItems = ListPanelViewModel.CommandList.Items;
            var macro = MacroFactory.CreateMacro(listItems) as LoopCommand;

            if (macro == null)
            {
                return;
            }

            try
            {
                SetRunningState(true);

                _cts = new CancellationTokenSource();

                await macro.Execute(_cts.Token);
            }
            catch (Exception ex)
            {
                if (_cts != null && !_cts.Token.IsCancellationRequested)
                {
                    _notificationService.ShowError(ex.Message, "Error");
                }
            }
            finally
            {
                listItems.Where(x => x.IsRunning).ToList().ForEach(x => x.IsRunning = false);
                ListPanelViewModel.CommandList.Items.ToList().ForEach(x => x.Progress = 0);

                _cts?.Dispose();
                _cts = null;

                SetRunningState(false);
            }
        }

        public void SetRunningState(bool isRunning)
        {
            IsRunning = isRunning;

            ButtonPanelViewModel.SetRunningState(isRunning);
            EditPanelViewModel.SetRunningState(isRunning);
            FavoritePanelViewModel.SetRunningState(isRunning);
            ListPanelViewModel.SetRunningState(isRunning);
            LogPanelViewModel.SetRunningState(isRunning);
        }

        public void SaveMacroFile(string filePath) => ListPanelViewModel.Save(filePath);

        public void LoadMacroFile(string filePath)
        { 
            ListPanelViewModel.Load(filePath); 
            EditPanelViewModel.SetListCount(ListPanelViewModel.GetCount()); 
        }
    }
}
