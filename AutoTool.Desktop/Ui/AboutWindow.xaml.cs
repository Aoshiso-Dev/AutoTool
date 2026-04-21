using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace AutoTool.Desktop.Ui;

/// <summary>
/// AutoTool のバージョン情報とサポート導線を表示するダイアログです。
/// </summary>
public partial class AboutWindow : Window
{
    private const string RepositoryUrl = "https://github.com/Aoshiso-Dev/AutoTool";
    private const string ReleasesUrl = "https://github.com/Aoshiso-Dev/AutoTool/releases";
    private const string LatestReleaseUrl = "https://github.com/Aoshiso-Dev/AutoTool/releases/latest";
    private const string IssueUrl = "https://github.com/Aoshiso-Dev/AutoTool/issues/new/choose";
    private static readonly Uri AboutIconUri = new("pack://application:,,,/AutoTool.Desktop;component/Resource/AutoTool.ico");
    private readonly string _versionText;

    public string AppName { get; }
    public ImageSource? AboutIconSource { get; }
    public string VersionLabel => $"バージョン: {_versionText}";
    public string RuntimeLabel { get; }
    public string BuildInfoLabel { get; }
    public string CommitLabel { get; }
    public string LicenseLabel { get; } =
        "このアプリには OSS ライブラリを含みます。各ライブラリのライセンスはリポジトリの関連ファイルで確認できます。";

    public AboutWindow()
    {
        InitializeComponent();

        var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
        var fileVersionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);

        AppName = assembly.GetName().Name ?? "AutoTool";
        AboutIconSource = LoadLargestIconFrame();
        _versionText = string.IsNullOrWhiteSpace(fileVersionInfo.FileVersion)
            ? assembly.GetName().Version?.ToString() ?? "不明"
            : fileVersionInfo.FileVersion;

        RuntimeLabel = $".NET Runtime: {Environment.Version}";
        BuildInfoLabel = $"ビルド日時: {File.GetLastWriteTime(assembly.Location):yyyy/MM/dd HH:mm}";

        var commit = ExtractCommit(fileVersionInfo.ProductVersion);
        CommitLabel = commit is null
            ? "コミット: 取得不可"
            : $"コミット: {commit}";

        DataContext = this;
    }

    private void CheckUpdateButton_Click(object sender, RoutedEventArgs e) => OpenUrl(LatestReleaseUrl);

    private void OpenReleaseButton_Click(object sender, RoutedEventArgs e) => OpenUrl(ReleasesUrl);

    private void ReportIssueButton_Click(object sender, RoutedEventArgs e) => OpenUrl(IssueUrl);

    private void OpenRepositoryButton_Click(object sender, RoutedEventArgs e) => OpenUrl(RepositoryUrl);

    private void OpenLicenseButton_Click(object sender, RoutedEventArgs e) => OpenUrl(RepositoryUrl);

    private void CopyVersionButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Clipboard.SetText(_versionText);
        }
        catch
        {
            return;
        }
    }

    private static string? ExtractCommit(string? productVersion)
    {
        if (string.IsNullOrWhiteSpace(productVersion))
        {
            return null;
        }

        var separatorIndex = productVersion.IndexOf('+');
        if (separatorIndex < 0 || separatorIndex >= productVersion.Length - 1)
        {
            return null;
        }

        var commit = productVersion[(separatorIndex + 1)..].Trim();
        return commit.Length > 10 ? commit[..10] : commit;
    }

    private static ImageSource? LoadLargestIconFrame()
    {
        try
        {
            var streamInfo = System.Windows.Application.GetResourceStream(AboutIconUri);
            if (streamInfo?.Stream is null)
            {
                return LoadFallbackIcon();
            }

            using var iconStream = streamInfo.Stream;
            var decoder = new IconBitmapDecoder(
                iconStream,
                BitmapCreateOptions.PreservePixelFormat,
                BitmapCacheOption.OnLoad);

            var bestFrame = decoder.Frames
                .OrderByDescending(frame => frame.PixelWidth * frame.PixelHeight)
                .ThenByDescending(frame => frame.Format.BitsPerPixel)
                .FirstOrDefault();

            if (bestFrame is not null)
            {
                bestFrame.Freeze();
                return bestFrame;
            }
        }
        catch
        {
            // 読み込み失敗時はフォールバックを返す。
        }

        return LoadFallbackIcon();
    }

    private static ImageSource? LoadFallbackIcon()
    {
        try
        {
            var fallback = new BitmapImage();
            fallback.BeginInit();
            fallback.UriSource = AboutIconUri;
            fallback.CacheOption = BitmapCacheOption.OnLoad;
            fallback.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
            fallback.EndInit();
            fallback.Freeze();
            return fallback;
        }
        catch
        {
            return null;
        }
    }

    private static void OpenUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
        catch
        {
            // 外部ブラウザ起動失敗時は何もしない。
        }
    }
}