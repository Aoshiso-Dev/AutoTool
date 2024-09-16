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

namespace AutoTool
{
    public partial class MainWindowViewModel : ObservableObject
    {
        private CancellationTokenSource? _cts;

        [ObservableProperty]
        private ButtonPanelViewModel _buttonPanelViewModel;

        [ObservableProperty]
        private FavoritePanelViewModel _runningPanelViewModel;

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
            RunningPanelViewModel = new FavoritePanelViewModel();
            FavoritePanelViewModel = new FavoritePanelViewModel();

            WeakReferenceMessenger.Default.Register<RunMessage>(this, async (sender, message) =>
            {
                await Run();
            });

            WeakReferenceMessenger.Default.Register<StopMessage>(this, (sender, message) =>
            {
                _cts?.Cancel();
            });

            WeakReferenceMessenger.Default.Register<SaveMessage>(this, (sender, message) =>
            {
                ListPanelViewModel.CommandList.Save();
            });

            WeakReferenceMessenger.Default.Register<LoadMessage>(this, (sender, message) =>
            {
                ListPanelViewModel.CommandList.Load();
            });

            WeakReferenceMessenger.Default.Register<ClearMessage>(this, (sender, message) =>
            {
                ListPanelViewModel.CommandList.Clear();
                EditPanelViewModel.Item = null;
            });

            WeakReferenceMessenger.Default.Register<AddMessage>(this, (sender, message) =>
            {
                ListPanelViewModel.Add((message as AddMessage).ItemType);
                ListPanelViewModel.SelectedLineNumber = ListPanelViewModel.CommandList.Items.Count;
                EditPanelViewModel.Item = ListPanelViewModel.CommandList.Items.Last();
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
                if (ListPanelViewModel.SelectedLineNumber == 0)
                {
                    EditPanelViewModel.Item = null;
                }
            });

            WeakReferenceMessenger.Default.Register<EditMessage>(this, (sender, message) =>
            {
                EditPanelViewModel.Item = (message as EditMessage).Item.Clone();
            });

            WeakReferenceMessenger.Default.Register<ChangeSelectedMessage>(this, (sender, message) =>
            {
                EditPanelViewModel.Item = (message as ChangeSelectedMessage).Item;
            });

            WeakReferenceMessenger.Default.Register<ApplyMessage>(this, (sender, message) =>
            {
                ListPanelViewModel.SelectedItem = EditPanelViewModel.Item;
                ListPanelViewModel.SelectedLineNumber = EditPanelViewModel.Item != null ? EditPanelViewModel.Item.LineNumber - 1 : 0;
            });

            WeakReferenceMessenger.Default.Register<ChangeTabMessage>(this, (sender, message) =>
            {
                SelectedListTabIndex = (message as ChangeTabMessage).TabIndex;
            });

            WeakReferenceMessenger.Default.Register<LogMessage>(this, (sender, message) =>
            {
                LogPanelViewModel.Log += message.Text + Environment.NewLine;
            });

            WeakReferenceMessenger.Default.Register<StartCommandMessage>(this, (sender, message) =>
            {
                var command = (message as StartCommandMessage).Command;

                LogPanelViewModel.Log += $"[{DateTime.Now}] {command.LineNumber} : {command.GetType()} Started\n";
                ListPanelViewModel.ExecutedLineNumber = command.LineNumber;

                
                var commandItem = ListPanelViewModel.CommandList.Items.FirstOrDefault(x => x.LineNumber == command.LineNumber);

                if (commandItem != null)
                {
                    commandItem.Progress = 0;
                    commandItem.IsRunning = true;
                }
            });

            WeakReferenceMessenger.Default.Register<FinishCommandMessage>(this, (sender, message) =>
            {
                var command = (message as FinishCommandMessage).Command;

                LogPanelViewModel.Log += $"[{DateTime.Now}] {command.LineNumber} : {command.GetType()} Finished\n";
                ListPanelViewModel.ExecutedLineNumber = 0;

                var commandItem = ListPanelViewModel.CommandList.Items.FirstOrDefault(x => x.LineNumber == command.LineNumber);

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
        }

        public async Task Run()
        {
            var listItems = ListPanelViewModel.CommandList.Items;
            var macro = MacroFactory.CreateMacro(listItems) as LoopCommand;

            try
            {
                ListPanelViewModel.CommandList.Items.ToList().ForEach(x => x.Progress = 0);

                ButtonPanelViewModel.IsRunning = true;
                EditPanelViewModel.IsRunning = true;

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

                ButtonPanelViewModel.IsRunning = false;
                EditPanelViewModel.IsRunning = false;

                ListPanelViewModel.CommandList.Items.ToList().ForEach(x => x.Progress = 0);
            }
        }
    }
}
