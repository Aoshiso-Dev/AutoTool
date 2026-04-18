using System.Text.Json;
using System.Windows;
using System.IO;

namespace AutoTool.Desktop.Model;

public class WindowSettings
{
    private const int CurrentSchemaVersion = 1;
    private TimeProvider _timeProvider = TimeProvider.System;

    public int SchemaVersion { get; set; } = CurrentSchemaVersion;
    public double Left { get; set; } = 100;
    public double Top { get; set; } = 100;
    public double Width { get; set; } = 1000;
    public double Height { get; set; } = 700;
    public WindowState WindowState { get; set; } = WindowState.Normal;
    public double EditPanelSplitterPosition { get; set; } = 300;
    public int SelectedTabIndex { get; set; }
    public int SelectedMacroListTabIndex { get; set; }
    public bool IsFavoritePanelOpen { get; set; }
    public bool IsLogPanelOpen { get; set; }
    public double FavoritePanelWidth { get; set; } = 340;
    public string? LastOpenedMacroFilePath { get; set; }

    private static readonly string SettingsDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "AutoTool");

    private static readonly string SettingsFilePath = Path.Combine(SettingsDirectory, "window_settings.json");
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    public WindowSettings()
    {
    }

    public WindowSettings(TimeProvider? timeProvider)
    {
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public void Save()
    {
        try
        {
            if (!Directory.Exists(SettingsDirectory))
            {
                Directory.CreateDirectory(SettingsDirectory);
            }

            var json = JsonSerializer.Serialize(this, SerializerOptions);
            File.WriteAllText(SettingsFilePath, json);
            System.Diagnostics.Debug.WriteLine($"[{_timeProvider.GetLocalNow():O}] ウィンドウ設定を保存しました: {SettingsFilePath}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[{_timeProvider.GetLocalNow():O}] ウィンドウ設定の保存に失敗しました: {ex.Message}");
        }
    }

    public static WindowSettings Load(TimeProvider? timeProvider = null)
    {
        var resolvedTimeProvider = timeProvider ?? TimeProvider.System;

        try
        {
            if (File.Exists(SettingsFilePath))
            {
                var json = File.ReadAllText(SettingsFilePath);
                var settings = JsonSerializer.Deserialize<WindowSettings>(json, SerializerOptions);
                if (settings is not null)
                {
                    settings._timeProvider = resolvedTimeProvider;
                    ValidatePosition(settings);
                    System.Diagnostics.Debug.WriteLine($"[{resolvedTimeProvider.GetLocalNow():O}] ウィンドウ設定を読み込みました: {SettingsFilePath}");
                    return settings;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[{resolvedTimeProvider.GetLocalNow():O}] ウィンドウ設定の読み込みに失敗しました: {ex.Message}");
        }

        return new WindowSettings(resolvedTimeProvider);
    }

    private static void ValidatePosition(WindowSettings settings)
    {
        if (settings.SchemaVersion <= 0)
        {
            settings.SchemaVersion = CurrentSchemaVersion;
        }

        var workingArea = SystemParameters.WorkArea;

        if (settings.Left < 0 || settings.Left + settings.Width > workingArea.Width)
        {
            settings.Left = Math.Max(0, (workingArea.Width - settings.Width) / 2);
        }

        if (settings.Top < 0 || settings.Top + settings.Height > workingArea.Height)
        {
            settings.Top = Math.Max(0, (workingArea.Height - settings.Height) / 2);
        }

        if (settings.Width < 600)
        {
            settings.Width = 1000;
        }

        if (settings.Height < 400)
        {
            settings.Height = 700;
        }

        if (settings.EditPanelSplitterPosition < 200 || settings.EditPanelSplitterPosition > 600)
        {
            settings.EditPanelSplitterPosition = 300;
        }

        if (settings.SelectedTabIndex is not (AutoTool.Desktop.TabIndexes.Macro or AutoTool.Desktop.TabIndexes.Monitor))
        {
            settings.SelectedTabIndex = AutoTool.Desktop.TabIndexes.Macro;
        }

        if (settings.SelectedMacroListTabIndex < 0)
        {
            settings.SelectedMacroListTabIndex = 0;
        }

        if (settings.FavoritePanelWidth is < 240 or > 700)
        {
            settings.FavoritePanelWidth = settings.EditPanelSplitterPosition is >= 240 and <= 700
                ? settings.EditPanelSplitterPosition
                : 340;
        }
    }

    public void UpdateFromWindow(Window window)
    {
        if (window.WindowState == WindowState.Normal)
        {
            Left = window.Left;
            Top = window.Top;
            Width = window.Width;
            Height = window.Height;
        }

        WindowState = window.WindowState;
    }

    public void UpdateFromViewModel(AutoTool.Desktop.MainWindowViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);

        SelectedTabIndex = viewModel.SelectedTabIndex;
        SelectedMacroListTabIndex = viewModel.MacroPanelViewModel.SelectedListTabIndex;
        IsFavoritePanelOpen = viewModel.MacroPanelViewModel.IsFavoritePanelOpen;
        IsLogPanelOpen = viewModel.MacroPanelViewModel.IsLogPanelOpen;
        FavoritePanelWidth = viewModel.MacroPanelViewModel.FavoritePanelWidth;
        EditPanelSplitterPosition = FavoritePanelWidth;
        LastOpenedMacroFilePath = viewModel.GetLastOpenedMacroFilePath();
    }

    public void ApplyToWindow(Window window)
    {
        window.Left = Left;
        window.Top = Top;
        window.Width = Width;
        window.Height = Height;
        window.WindowState = WindowState;
    }

    public void ApplyToViewModel(AutoTool.Desktop.MainWindowViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);

        viewModel.RestoreSessionState(
            SelectedTabIndex,
            IsFavoritePanelOpen,
            IsLogPanelOpen,
            SelectedMacroListTabIndex,
            FavoritePanelWidth,
            LastOpenedMacroFilePath);
    }

    public void ClearSessionState()
    {
        SelectedTabIndex = AutoTool.Desktop.TabIndexes.Macro;
        SelectedMacroListTabIndex = 0;
        IsFavoritePanelOpen = false;
        IsLogPanelOpen = false;
        LastOpenedMacroFilePath = null;
    }
}
