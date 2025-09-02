using System.Windows;

namespace AutoTool.Services
{
    /// <summary>
    /// Phase 5���S�����ŁF���b�Z�[�W�T�[�r�X�C���^�[�t�F�[�X
    /// </summary>
    public interface IMessageService
    {
        void ShowError(string message, string title = "�G���[");
        void ShowWarning(string message, string title = "�x��");
        void ShowInformation(string message, string title = "���");
        bool ShowConfirmation(string message, string title = "�m�F");
    }

    /// <summary>
    /// MessageBox���g�p�������b�Z�[�W�T�[�r�X�̎���
    /// </summary>
    public class MessageBoxService : IMessageService
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
            return MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;
        }
    }
}