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
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.IO;
using System.Xml.Serialization;
using System.Windows.Shapes;
using System.Security.Policy;
using AutoTool.ViewModel;
using AutoTool.Model;
using static AutoTool.Model.FileManager;

namespace AutoTool
{
    public static class TabIndexes
    {
        public static readonly int Macro = 0;
        public static readonly int Monitor = 1;
    }

    public partial class MainWindowViewModel : ObservableObject
    {
        private Dictionary<int, FileManager> _fileManagers = [];

        public string AutoToolTitle
        {
            get { return IsFileOperationEnable && _fileManagers[SelectedTabIndex].IsFileOpened ? $"AutoTool - {CurrentFileName}" : "AutoTool"; }
        }

        private int _selectedTabIndex = TabIndexes.Macro;
        public int SelectedTabIndex
        {
            get { return _selectedTabIndex; }
            set
            {
                SetProperty(ref _selectedTabIndex, value);

                MessageBox.Show($"TabIndex: {SelectedTabIndex}");

                UpdateProperties();
            }
        }

        public bool IsFileOperationEnable
        {
            get { return _fileManagers.ContainsKey(SelectedTabIndex); }
        }

        public bool IsFileOpened
        {
            get { return IsFileOperationEnable && _fileManagers[SelectedTabIndex].IsFileOpened; }
        }

        public string CurrentFileName
        {
            get { return _fileManagers[SelectedTabIndex].CurrentFileName; }
            set
            {
                _fileManagers[SelectedTabIndex].CurrentFileName = value;
                OnPropertyChanged(nameof(CurrentFileName));
            }
        }

        public string CurrentFilePath
        {
            get { return _fileManagers[SelectedTabIndex].CurrentFilePath; }
            set
            {
                _fileManagers[SelectedTabIndex].CurrentFilePath = value;
                OnPropertyChanged(nameof(CurrentFilePath));
            }
        }

        public ObservableCollection<RecentFile>? RecentFiles
        {
            get { return _fileManagers[SelectedTabIndex].RecentFiles; }
        }

        public string MenuItemHeader_SaveFile
        {
            get { return IsFileOperationEnable && _fileManagers[SelectedTabIndex].IsFileOpened ? $"{CurrentFileName} を保存" : "保存"; }
        }

        public string MenuItemHeader_SaveFileAs
        {
            get { return IsFileOperationEnable && _fileManagers[SelectedTabIndex].IsFileOpened ? $"{CurrentFileName} を名前を付けて保存" : "名前を付けて保存"; }
        }

        [ObservableProperty]
        private MacroPanelViewModel _macroPanelViewModel;

        public MainWindowViewModel()
        {
            MacroPanelViewModel = new MacroPanelViewModel();

            InitializeFileManager();
        }

        private void InitializeFileManager()
        {
            _fileManagers.Add(
                TabIndexes.Macro,
                new FileManager(
                    new FileManager.FileTypeInfo()
                    {
                        Filter = "AutoTool マクロファイル(*.macro)|*.macro",
                        FilterIndex = 1,
                        RestoreDirectory = true,
                        DefaultExt = "macro",
                        Title = "マクロファイルを開く",
                    },
                    SaveFile,
                    LoadFile
                    )
                );
        }


        private void UpdateProperties()
        {
            OnPropertyChanged(nameof(IsFileOperationEnable));
            OnPropertyChanged(nameof(IsFileOpened));
            OnPropertyChanged(nameof(CurrentFilePath));
            OnPropertyChanged(nameof(CurrentFileName));
            OnPropertyChanged(nameof(RecentFiles));
            OnPropertyChanged(nameof(MenuItemHeader_SaveFile));
            OnPropertyChanged(nameof(MenuItemHeader_SaveFileAs));
            OnPropertyChanged(nameof(AutoToolTitle));
        }


        [RelayCommand]
        private void VersionInfo()
        {
            var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            var appName = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;
            string githubUrl = "https://github.com/Aoshiso-Dev/AutoTool";
            var versionString = $"{version.Major}.{version.Minor}.{version.Build}";

            MessageBox.Show($"{appName}\nVer.{versionString}\n{githubUrl}", "バージョン情報", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        [RelayCommand]
        private void OpenFile(string filePath)
        {
            _fileManagers[SelectedTabIndex].OpenFile(filePath);
            UpdateProperties();
        }

        [RelayCommand]
        private void SaveFile()
        {
            _fileManagers[SelectedTabIndex].SaveFile();
            UpdateProperties();
        }

        [RelayCommand]
        private void SaveFileAs()
        {
            _fileManagers[SelectedTabIndex].SaveFileAs();
            UpdateProperties();
        }


        private void SaveFile(string filePath)
        {
            if (SelectedTabIndex == TabIndexes.Macro)
            {
                MacroPanelViewModel.SaveMacroFile(filePath);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        private void LoadFile(string filePath)
        {
            if (SelectedTabIndex == TabIndexes.Macro)
            {
                MacroPanelViewModel.LoadMacroFile(filePath);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

    }
}
