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

namespace AutoTool.ViewModel
{
    public partial class MonitoringPanelViewModel : ObservableObject
    {
        public MonitoringPanelViewModel()
        {
           
        }
    }
}
