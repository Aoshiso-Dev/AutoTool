using AutoTool.Desktop.Model;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text.Json;
using System.Windows;
using AutoTool.Application.Assistant;
using AutoTool.Application.Ports;
using Wpf.Ui.Controls;

namespace AutoTool.Desktop.View;

/// <summary>
/// アプリ設定の編集画面を表示し、保存・キャンセルなどの設定操作を処理します。
/// </summary>
public partial class SettingsWindow : FluentWindow
{
    private const string LlamaCppReferenceUrl = "https://github.com/ggml-org/llama.cpp/releases";
    private const string LlamaCppRecentReleasesApiUrl = "https://api.github.com/repos/ggml-org/llama.cpp/releases?per_page=10";
    private const string RecommendedModelReferenceUrl = "https://huggingface.co/Qwen/Qwen3-4B-GGUF";
    private const string RecommendedModelApiUrl = "https://huggingface.co/api/models/Qwen/Qwen3-4B-GGUF";
    private const string RecommendedModelFileNameFallback = "Qwen3-4B-Q4_K_M.gguf";

    public bool RestorePreviousSession { get; private set; }
    public WindowSizePreset SelectedWindowSizePreset { get; private set; }
    public AssistantSettings AssistantSettings { get; private set; }
    private readonly IFilePicker _filePicker;
    private bool _isDownloadingAssistantAsset;

    public SettingsWindow(
        bool restorePreviousSession,
        WindowSizePreset windowSizePreset,
        AssistantSettings assistantSettings,
        IFilePicker filePicker)
    {
        ArgumentNullException.ThrowIfNull(assistantSettings);
        ArgumentNullException.ThrowIfNull(filePicker);

        InitializeComponent();
        RestorePreviousSession = restorePreviousSession;
        SelectedWindowSizePreset = windowSizePreset;
        AssistantSettings = assistantSettings.Normalize();
        _filePicker = filePicker;
        RestorePreviousSessionCheckBox.IsChecked = restorePreviousSession;
        WindowSizePresetComboBox.SelectedIndex = windowSizePreset switch
        {
            WindowSizePreset.Compact => 0,
            WindowSizePreset.Standard => 1,
            WindowSizePreset.Large => 2,
            _ => 1
        };
        ApplyAssistantSettingsToView(AssistantSettings);
    }

    private void SaveButton_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        RestorePreviousSession = RestorePreviousSessionCheckBox.IsChecked ?? true;
        SelectedWindowSizePreset = WindowSizePresetComboBox.SelectedIndex switch
        {
            0 => WindowSizePreset.Compact,
            2 => WindowSizePreset.Large,
            _ => WindowSizePreset.Standard
        };
        AssistantSettings = CreateAssistantSettingsFromView();

        DialogResult = true;
    }

    private void CancelButton_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        DialogResult = false;
    }

    private void SelectLlamaServerButton_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        var path = _filePicker.OpenFile(new FileDialogOptions(
            "llama-server.exe を選択",
            "llama-server.exe|llama-server.exe|実行ファイル (*.exe)|*.exe|すべてのファイル (*.*)|*.*",
            1,
            true,
            "exe"));

        if (!string.IsNullOrWhiteSpace(path))
        {
            LlamaServerPathTextBox.Text = path;
        }
    }

    private void SelectModelButton_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        var path = _filePicker.OpenFile(new FileDialogOptions(
            "GGUFモデルを選択",
            "GGUFモデル (*.gguf)|*.gguf|すべてのファイル (*.*)|*.*",
            1,
            true,
            "gguf"));

        if (!string.IsNullOrWhiteSpace(path))
        {
            ModelPathTextBox.Text = path;
        }
    }

    private async void DownloadLlamaCppButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isDownloadingAssistantAsset)
        {
            return;
        }

        try
        {
            _isDownloadingAssistantAsset = true;
            AssistantDownloadStatusTextBlock.Text = "llama.cpp を取得しています...";
            var llamaServerPath = await DownloadLlamaCppAsync();
            LlamaServerPathTextBox.Text = llamaServerPath;
            AssistantStartServerCheckBox.IsChecked = true;
            AssistantDownloadStatusTextBlock.Text = "llama.cpp を取得しました。";
        }
        catch (Exception ex)
        {
            AssistantDownloadStatusTextBlock.Text = "llama.cpp の取得に失敗しました。";
            System.Windows.MessageBox.Show(
                this,
                ex.Message,
                "llama.cpp の取得に失敗しました",
                System.Windows.MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
        finally
        {
            _isDownloadingAssistantAsset = false;
        }
    }

    private void OpenLlamaCppReferenceButton_Click(object sender, RoutedEventArgs e)
    {
        OpenUrl(LlamaCppReferenceUrl);
    }

    private async void DownloadRecommendedModelButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isDownloadingAssistantAsset)
        {
            return;
        }

        try
        {
            _isDownloadingAssistantAsset = true;
            AssistantDownloadStatusTextBlock.Text = "推奨GGUFモデルを取得しています...";
            var model = await ResolveRecommendedModelAsync();
            var modelDirectory = Path.Combine(AppContext.BaseDirectory, "Settings", "models");
            var modelPath = await DownloadFileAsync(model.DownloadUrl, modelDirectory, model.FileName);
            ModelPathTextBox.Text = modelPath;
            if (string.IsNullOrWhiteSpace(ModelNameTextBox.Text))
            {
                ModelNameTextBox.Text = "local-model";
            }

            AssistantDownloadStatusTextBlock.Text = "推奨GGUFモデルを取得しました。";
        }
        catch (Exception ex)
        {
            AssistantDownloadStatusTextBlock.Text = "推奨GGUFモデルの取得に失敗しました。";
            System.Windows.MessageBox.Show(
                this,
                ex.Message,
                "推奨GGUFモデルの取得に失敗しました",
                System.Windows.MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
        finally
        {
            _isDownloadingAssistantAsset = false;
        }
    }

    private void OpenRecommendedModelReferenceButton_Click(object sender, RoutedEventArgs e)
    {
        OpenUrl(RecommendedModelReferenceUrl);
    }

    private void ApplyAssistantSettingsToView(AssistantSettings settings)
    {
        AssistantEnabledCheckBox.IsChecked = settings.IsEnabled;
        AssistantStartServerCheckBox.IsChecked = settings.StartServerAutomatically;
        LlamaServerPathTextBox.Text = settings.LlamaServerPath;
        ModelPathTextBox.Text = settings.ModelPath;
        ModelNameTextBox.Text = settings.ModelName;
        PortTextBox.Text = settings.Port.ToString();
        ContextLengthTextBox.Text = settings.ContextLength.ToString();
        MaxOutputTokensTextBox.Text = settings.MaxOutputTokens.ToString();
        TimeoutSecondsTextBox.Text = settings.TimeoutSeconds.ToString();
        IncludeMacroContextCheckBox.IsChecked = settings.IncludeMacroContext;
        IncludeSelectedCommandContextCheckBox.IsChecked = settings.IncludeSelectedCommandContext;
        IncludeLogContextCheckBox.IsChecked = settings.IncludeLogContext;
    }

    private AssistantSettings CreateAssistantSettingsFromView()
    {
        return new AssistantSettings
        {
            IsEnabled = AssistantEnabledCheckBox.IsChecked ?? false,
            ProviderKind = AssistantProviderKind.LlamaCpp,
            StartServerAutomatically = AssistantStartServerCheckBox.IsChecked ?? false,
            LlamaServerPath = LlamaServerPathTextBox.Text,
            ModelPath = ModelPathTextBox.Text,
            ModelName = ModelNameTextBox.Text,
            Port = ParseIntOrDefault(PortTextBox.Text, 8088),
            ContextLength = ParseIntOrDefault(ContextLengthTextBox.Text, 4096),
            MaxOutputTokens = ParseIntOrDefault(MaxOutputTokensTextBox.Text, 512),
            TimeoutSeconds = ParseIntOrDefault(TimeoutSecondsTextBox.Text, 60),
            IncludeMacroContext = IncludeMacroContextCheckBox.IsChecked ?? true,
            IncludeSelectedCommandContext = IncludeSelectedCommandContextCheckBox.IsChecked ?? true,
            IncludeLogContext = IncludeLogContextCheckBox.IsChecked ?? false
        }.Normalize();
    }

    private static int ParseIntOrDefault(string text, int defaultValue)
    {
        return int.TryParse(text, out var value) ? value : defaultValue;
    }

    private static async Task<string> DownloadLlamaCppAsync()
    {
        using var httpClient = CreateHttpClient();
        using var response = await httpClient.GetAsync(LlamaCppRecentReleasesApiUrl);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(stream);
        var asset = SelectLlamaCppWindowsAsset(document.RootElement)
            ?? throw new InvalidOperationException("Windows x64 用の llama.cpp 配布ZIPが見つかりませんでした。公式ページから手動で取得してください。");

        var baseDirectory = Path.Combine(AppContext.BaseDirectory, "Settings", "llama.cpp");
        var zipPath = await DownloadFileAsync(asset.DownloadUrl, baseDirectory, asset.FileName);
        var extractDirectory = Path.Combine(baseDirectory, "latest");
        if (Directory.Exists(extractDirectory))
        {
            Directory.Delete(extractDirectory, recursive: true);
        }

        ZipFile.ExtractToDirectory(zipPath, extractDirectory);
        return Directory.EnumerateFiles(extractDirectory, "llama-server.exe", SearchOption.AllDirectories).FirstOrDefault()
            ?? throw new InvalidOperationException("取得したZIP内に llama-server.exe が見つかりませんでした。公式ページから手動で取得してください。");
    }

    private static LlamaCppAsset? SelectLlamaCppWindowsAsset(JsonElement releaseRoot)
    {
        return releaseRoot.ValueKind switch
        {
            JsonValueKind.Array => releaseRoot.EnumerateArray()
                .Select(SelectLlamaCppWindowsAssetFromRelease)
                .FirstOrDefault(static asset => asset is not null),
            JsonValueKind.Object => SelectLlamaCppWindowsAssetFromRelease(releaseRoot),
            _ => null
        };
    }

    private static LlamaCppAsset? SelectLlamaCppWindowsAssetFromRelease(JsonElement release)
    {
        if (!release.TryGetProperty("assets", out var assets) || assets.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        return assets.EnumerateArray()
            .Select(asset => new LlamaCppAsset(
                asset.GetProperty("name").GetString() ?? string.Empty,
                asset.GetProperty("browser_download_url").GetString() ?? string.Empty))
            .Where(asset => asset.FileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            .Where(asset => asset.FileName.StartsWith("llama-", StringComparison.OrdinalIgnoreCase))
            .Where(asset => asset.FileName.Contains("win", StringComparison.OrdinalIgnoreCase))
            .Where(asset => asset.FileName.Contains("x64", StringComparison.OrdinalIgnoreCase))
            .OrderBy(asset => asset.FileName.Contains("cpu", StringComparison.OrdinalIgnoreCase) ? 0 : 1)
            .ThenBy(asset => asset.FileName.Contains("cuda", StringComparison.OrdinalIgnoreCase) ? 1 : 0)
            .FirstOrDefault();
    }

    private static async Task<RecommendedModel> ResolveRecommendedModelAsync()
    {
        using var httpClient = CreateHttpClient();
        using var response = await httpClient.GetAsync(RecommendedModelApiUrl);
        if (!response.IsSuccessStatusCode)
        {
            return CreateRecommendedModel(RecommendedModelFileNameFallback);
        }

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(stream);
        if (!document.RootElement.TryGetProperty("siblings", out var siblings) || siblings.ValueKind != JsonValueKind.Array)
        {
            return CreateRecommendedModel(RecommendedModelFileNameFallback);
        }

        var fileName = siblings.EnumerateArray()
            .Select(file => file.TryGetProperty("rfilename", out var rfilename) ? rfilename.GetString() : null)
            .Where(static file => !string.IsNullOrWhiteSpace(file))
            .Cast<string>()
            .Where(static file => file.EndsWith(".gguf", StringComparison.OrdinalIgnoreCase))
            .Where(static file => file.Contains("Q4_K_M", StringComparison.OrdinalIgnoreCase))
            .OrderBy(static file => file.Contains("Qwen3-4B", StringComparison.OrdinalIgnoreCase) ? 0 : 1)
            .ThenBy(static file => file.Length)
            .FirstOrDefault();

        return CreateRecommendedModel(fileName ?? RecommendedModelFileNameFallback);
    }

    private static RecommendedModel CreateRecommendedModel(string fileName)
    {
        var escapedFileName = string.Join("/", fileName.Split('/').Select(Uri.EscapeDataString));
        return new RecommendedModel(
            Path.GetFileName(fileName),
            $"https://huggingface.co/Qwen/Qwen3-4B-GGUF/resolve/main/{escapedFileName}?download=true");
    }

    private static async Task<string> DownloadFileAsync(string url, string directory, string fileName)
    {
        Directory.CreateDirectory(directory);
        var targetPath = Path.Combine(directory, fileName);
        var temporaryPath = targetPath + ".download";

        using var httpClient = CreateHttpClient();
        using var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();
        await using (var input = await response.Content.ReadAsStreamAsync())
        await using (var output = File.Create(temporaryPath))
        {
            await input.CopyToAsync(output);
        }

        File.Move(temporaryPath, targetPath, overwrite: true);
        return targetPath;
    }

    private static HttpClient CreateHttpClient()
    {
        var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("AutoTool");
        return httpClient;
    }

    private static void OpenUrl(string url)
    {
        Process.Start(new ProcessStartInfo(url)
        {
            UseShellExecute = true
        });
    }

    private sealed record LlamaCppAsset(string FileName, string DownloadUrl);

    private sealed record RecommendedModel(string FileName, string DownloadUrl);
}
