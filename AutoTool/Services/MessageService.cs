using System.Windows;
using AutoTool.ViewModel;

namespace AutoTool.Services
{
    /// <summary>
    /// ���b�Z�[�W�T�[�r�X�̎���
    /// </summary>
    public class MessageService : IMessageService
    {
        public void ShowError(string message, string title = "�G���[")
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
        }

        public void ShowWarning(string message, string title = "�x��")
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        public void ShowInformation(string message, string title = "���")
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public bool ShowConfirmation(string message, string title = "�m�F")
        {
            var result = MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question);
            return result == MessageBoxResult.Yes;
        }
    }
}