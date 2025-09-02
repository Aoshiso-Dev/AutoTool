using System.Windows;
using AutoTool.ViewModel;

namespace AutoTool.Services
{
    /// <summary>
    /// メッセージサービスの実装
    /// </summary>
    public class MessageService : IMessageService
    {
        public void ShowError(string message, string title = "エラー")
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
        }

        public void ShowWarning(string message, string title = "警告")
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        public void ShowInformation(string message, string title = "情報")
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public bool ShowConfirmation(string message, string title = "確認")
        {
            var result = MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question);
            return result == MessageBoxResult.Yes;
        }
    }
}