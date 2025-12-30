namespace AutoTool.Services.Interfaces
{
    public interface INotificationService
    {
        void ShowInfo(string message, string title);
        void ShowError(string message, string title);
    }
}
