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
        private LogPanelViewModel _logPanelViewModel;

        public MainWindowViewModel()
        {
            ListPanelViewModel = new ListPanelViewModel();
            ButtonPanelViewModel = new ButtonPanelViewModel();
            LogPanelViewModel = new LogPanelViewModel();

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
            });

            WeakReferenceMessenger.Default.Register<AddMessage>(this, (sender, message) =>
            {
                ListPanelViewModel.Add(message.ItemType);
            });

            WeakReferenceMessenger.Default.Register<LogMessage>(this, (sender, message) =>
            {
                LogPanelViewModel.Log += message.Text + Environment.NewLine;
            });

            WeakReferenceMessenger.Default.Register<ExecuteCommandMessage>(this, (sender, message) =>
            {
                var command = (message as ExecuteCommandMessage).Command;
                if (command != null)
                {
                    LogPanelViewModel.Log += $"[{DateTime.Now}] {command.LineNumber} : {command.GetType()}\n";

                    ListPanelViewModel.ExecutedLineNumber = command.LineNumber;
                }
            });
        }

        public async Task Run()
        {
            var listItems = ListPanelViewModel.CommandList.Items;
            var macro = MacroFactory.CreateMacro(listItems) as LoopCommand;

            try
            {
                ButtonPanelViewModel.IsRunning = true;

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
            }
        }
    }
}
