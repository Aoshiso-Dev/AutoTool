using System.Windows;

namespace AutoTool.Services
{
    /// <summary>
    /// Phase 5完全統合版：メッセージサービスインターフェース
    /// </summary>
    public interface IMessageService
    {
        void ShowError(string message, string title = "エラー");
        void ShowWarning(string message, string title = "警告");
        void ShowInformation(string message, string title = "情報");
        bool ShowConfirmation(string message, string title = "確認");
    }

    /// <summary>
    /// MessageBoxを使用したメッセージサービスの実装
    /// </summary>
    public class MessageBoxService : IMessageService
    {
        public void ShowError(string message, string title = "エラー")
        {
            System.Windows.MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
        }

        public void ShowWarning(string message, string title = "警告")
        {
            System.Windows.MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        public void ShowInformation(string message, string title = "情報")
        {
            System.Windows.MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public bool ShowConfirmation(string message, string title = "確認")
        {
            return System.Windows.MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;
        }
    }
}