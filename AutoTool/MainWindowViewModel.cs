using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Panels.ViewModel;
using Panels.Message;
using CommunityToolkit.Mvvm.Input;
using Panels.List.Class;
using Panels.Model.MacroFactory;
using System.Windows;
using Command.Class;
using Command.Interface;
using Command.Message;
using System.Windows.Controls;
using System.Windows.Data;
using Panels.Model.List.Interface;

namespace AutoTool
{
    public partial class MainWindowViewModel : ObservableObject
    {
        private CancellationTokenSource? _cts;

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

        public MainWindowViewModel()
        {
            ListPanelViewModel = new ListPanelViewModel();
            EditPanelViewModel = new EditPanelViewModel();
            ButtonPanelViewModel = new ButtonPanelViewModel();
            LogPanelViewModel = new LogPanelViewModel();
            FavoritePanelViewModel = new FavoritePanelViewModel();

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
            });
            WeakReferenceMessenger.Default.Register<ClearMessage>(this, (sender, message) =>
            {
                ListPanelViewModel.Clear();
            });
            WeakReferenceMessenger.Default.Register<AddMessage>(this, (sender, message) =>
            {
                ListPanelViewModel.Add((message as AddMessage).ItemType);
            });
            WeakReferenceMessenger.Default.Register<UpMessage>(this, (sender, message) =>
            {
                ListPanelViewModel.Up();
            });
            WeakReferenceMessenger.Default.Register<DownMessage>(this, (sender, message) =>
            {
                ListPanelViewModel.Down();
            });
            WeakReferenceMessenger.Default.Register<DeleteMessage>(this, (sender, message) =>
            {
                ListPanelViewModel.Delete();

                if (ListPanelViewModel.GetCount() == 0)
                {
                    EditPanelViewModel.SetItem(null);
                }
            });

            // From ListPanelViewModel
            WeakReferenceMessenger.Default.Register<ChangeSelectedMessage>(this, (sender, message) =>
            {
                EditPanelViewModel.SetItem((message as ChangeSelectedMessage).Item);
            });

            // From EditPanelViewModel
            WeakReferenceMessenger.Default.Register<RefreshListViewMessage>(this, (sender, message) =>
            {
                ListPanelViewModel.Refresh();
            });

            // From Commands
            WeakReferenceMessenger.Default.Register<StartCommandMessage>(this, (sender, message) =>
            {
                var command = (message as StartCommandMessage).Command;

                LogPanelViewModel.WriteLog($"[{DateTime.Now}] {command.LineNumber} : {command.GetType()} Started");

                var commandItem = ListPanelViewModel.GetExecutedItem();

                if (commandItem != null)
                {
                    commandItem.Progress = 0;
                    commandItem.IsRunning = true;
                }
            });
            WeakReferenceMessenger.Default.Register<FinishCommandMessage>(this, (sender, message) =>
            {
                var command = (message as FinishCommandMessage).Command;

                LogPanelViewModel.WriteLog($"[{DateTime.Now}] {command.LineNumber} : {command.GetType()} Finished");

                var commandItem = ListPanelViewModel.GetExecutedItem();

                if (commandItem != null)
                {
                    commandItem.Progress = 0;
                    commandItem.IsRunning = false;
                }
            });
            WeakReferenceMessenger.Default.Register<UpdateProgressMessage>(this, (sender, message) =>
            {
                var command = (message as UpdateProgressMessage).Command;
                var progress = (message as UpdateProgressMessage).Progress;

                var commandItem = ListPanelViewModel.CommandList.Items.Where(x => x.LineNumber == command.LineNumber).FirstOrDefault();

                if (commandItem != null)
                {
                    commandItem.Progress = progress;
                }
            });

            // From Other
            WeakReferenceMessenger.Default.Register<LogMessage>(this, (sender, message) =>
            {
                LogPanelViewModel.WriteLog((message as LogMessage).Text);
            });
        }

        public async Task Run()
        {
            var listItems = ListPanelViewModel.CommandList.Items;
            var macro = MacroFactory.CreateMacro(listItems) as LoopCommand;

            if(macro == null)
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
                    MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            finally
            {
                listItems.Where(x => x.IsRunning).ToList().ForEach(x => x.IsRunning = false);

                _cts?.Dispose();
                _cts = null;

                SetRunningState(false);

                ListPanelViewModel.CommandList.Items.ToList().ForEach(x => x.Progress = 0);
            }
        }

        public void SetRunningState(bool isRunning)
        {
            ButtonPanelViewModel.SetRunningState(isRunning);
            EditPanelViewModel.SetRunningState(isRunning);
            FavoritePanelViewModel.SetRunningState(isRunning);
            ListPanelViewModel.SetRunningState(isRunning);
            LogPanelViewModel.SetRunningState(isRunning);
        }
    }
}
