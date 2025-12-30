using System.Windows;
using AutoTool.Services.Interfaces;

namespace AutoTool.Services.Implementations
{
    public class WpfNotificationService : INotificationService
    {
        public void ShowInfo(string message, string title)
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public void ShowError(string message, string title)
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
