using AutoTool.Services.Implementations;
using AutoTool.Services.Interfaces;

namespace AutoTool.Services
{
    public static class AppServices
    {
        private static readonly Lazy<INotificationService> NotificationServiceLazy =
            new(() => new WpfNotificationService());

        private static readonly Lazy<IStatusMessageScheduler> StatusMessageSchedulerLazy =
            new(() => new DispatcherStatusMessageScheduler());

        private static readonly Lazy<IFileDialogService> FileDialogServiceLazy =
            new(() => new WpfFileDialogService());

        private static readonly Lazy<IRecentFileStore> RecentFileStoreLazy =
            new(() => new XmlRecentFileStore());

        private static readonly Lazy<ILogService> LogServiceLazy =
            new(() => new LogHelperLogger());

        public static INotificationService NotificationService => NotificationServiceLazy.Value;

        public static ILogService LogService => LogServiceLazy.Value;

        public static MainWindowViewModel CreateMainWindowViewModel()
        {
            return new MainWindowViewModel(
                NotificationServiceLazy.Value,
                StatusMessageSchedulerLazy.Value,
                FileDialogServiceLazy.Value,
                RecentFileStoreLazy.Value,
                LogServiceLazy.Value);
        }
    }
}
