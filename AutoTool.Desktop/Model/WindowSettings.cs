using System.Text.Json;
using System.Windows;
using System.IO;
using AutoTool.Infrastructure.Paths;

namespace AutoTool.Desktop.Model;

/// <summary>
/// コマンド実行や画面表示で参照する設定値を保持し、入力値を型安全に扱えるようにします。
/// </summary>
public class WindowSettings
{
    private const int CurrentSchemaVersion = 3;
    private const double MinimumWindowWidth = 640;
    private const double MinimumWindowHeight = 420;
    private const double CompactSizeRatio = 0.30;
    private const double StandardSizeRatio = 0.50;
    private const double LargeSizeRatio = 0.70;
    private TimeProvider _timeProvider = TimeProvider.System;

    public int SchemaVersion { get; set; } = CurrentSchemaVersion;
    public double Left { get; set; } = 100;
    public double Top { get; set; } = 100;
    public double Width { get; set; } = 960;
    public double Height { get; set; } = 600;
    public WindowState WindowState { get; set; } = WindowState.Normal;
    public WindowSizePreset WindowSizePreset { get; set; } = WindowSizePreset.Standard;
    public bool IsWindowSizePresetInitialized { get; set; }
    public double EditPanelSplitterPosition { get; set; } = 300;
    public int SelectedTabIndex { get; set; }
    public int SelectedMacroListTabIndex { get; set; }
    public bool IsFavoritePanelOpen { get; set; }
    public bool IsLogPanelOpen { get; set; }
    public bool IsVariablePanelOpen { get; set; }
    public bool IsAssistantPanelOpen { get; set; }
    public double FavoritePanelWidth { get; set; } = 340;
    public string? LastOpenedMacroFilePath { get; set; }

    private static readonly string SettingsDirectory = Path.Combine(
        ApplicationPathResolver.GetApplicationDirectory(),
        "Settings");

    private static readonly string SettingsFilePath = Path.Combine(SettingsDirectory, "window_settings.json");
    private static readonly string LegacySettingsDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "AutoTool");
    private static readonly string LegacySettingsFilePath = Path.Combine(LegacySettingsDirectory, "window_settings.json");
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
        }
        catch
        {
        }
    }

    public static WindowSettings Load(TimeProvider? timeProvider = null)
    {
        var resolvedTimeProvider = timeProvider ?? TimeProvider.System;

        if (TryLoadFromPath(SettingsFilePath, resolvedTimeProvider, out var settings))
        {
            return settings;
        }

        if (TryLoadFromPath(LegacySettingsFilePath, resolvedTimeProvider, out settings))
        {
            settings.Save();
            TryDeleteLegacySettingsFile();
            return settings;
        }

        return new WindowSettings(resolvedTimeProvider);
    }

    private static bool TryLoadFromPath(string filePath, TimeProvider resolvedTimeProvider, out WindowSettings settings)
    {
        settings = null!;

        try
        {
            if (!File.Exists(filePath))
            {
                return false;
            }

            var json = File.ReadAllText(filePath);
            var loaded = JsonSerializer.Deserialize<WindowSettings>(json, SerializerOptions);
            if (loaded is null)
            {
                return false;
            }

            loaded._timeProvider = resolvedTimeProvider;
            ValidatePosition(loaded);
            settings = loaded;
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static void TryDeleteLegacySettingsFile()
    {
        try
        {
            if (File.Exists(LegacySettingsFilePath))
            {
                File.Delete(LegacySettingsFilePath);
            }

            if (Directory.Exists(LegacySettingsDirectory) && !Directory.EnumerateFileSystemEntries(LegacySettingsDirectory).Any())
            {
                Directory.Delete(LegacySettingsDirectory);
            }
        }
        catch
        {
            // クリーンアップに失敗しても旧設定は保持し、処理は継続します。
        }
    }

    private static void ValidatePosition(WindowSettings settings)
    {
        if (settings.SchemaVersion <= 0)
        {
            settings.SchemaVersion = CurrentSchemaVersion;
        }

        var workingArea = SystemParameters.WorkArea;

        if (!settings.IsWindowSizePresetInitialized)
        {
            settings.WindowSizePreset = GetDefaultPreset(workingArea);
            settings.IsWindowSizePresetInitialized = true;
        }

        if (!Enum.IsDefined(settings.WindowSizePreset))
        {
            settings.WindowSizePreset = WindowSizePreset.Standard;
        }

        settings.ApplyPresetSize(workingArea);

        if (settings.Left < 0 || settings.Left + settings.Width > workingArea.Width)
        {
            settings.Left = Math.Max(0, (workingArea.Width - settings.Width) / 2);
        }

        if (settings.Top < 0 || settings.Top + settings.Height > workingArea.Height)
        {
            settings.Top = Math.Max(0, (workingArea.Height - settings.Height) / 2);
        }

        settings.Width = Math.Clamp(settings.Width, MinimumWindowWidth, workingArea.Width);
        settings.Height = Math.Clamp(settings.Height, MinimumWindowHeight, workingArea.Height);

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
        IsVariablePanelOpen = viewModel.MacroPanelViewModel.IsVariablePanelOpen;
        IsAssistantPanelOpen = viewModel.MacroPanelViewModel.IsAssistantPanelOpen;
        FavoritePanelWidth = viewModel.MacroPanelViewModel.FavoritePanelWidth;
        EditPanelSplitterPosition = FavoritePanelWidth;
        LastOpenedMacroFilePath = viewModel.GetLastOpenedMacroFilePath();
    }

    public void ApplyToWindow(Window window)
    {
        ArgumentNullException.ThrowIfNull(window);

        var workingArea = SystemParameters.WorkArea;
        ApplyPresetSize(workingArea);

        window.Left = Left;
        window.Top = Top;
        window.Width = Width;
        window.Height = Height;
        window.WindowState = WindowState;
    }

    public void UpdateWindowSizePreset(WindowSizePreset preset)
    {
        if (!Enum.IsDefined(preset))
        {
            preset = WindowSizePreset.Standard;
        }

        WindowSizePreset = preset;
        IsWindowSizePresetInitialized = true;

        var workingArea = SystemParameters.WorkArea;
        ApplyPresetSize(workingArea);
        Left = Math.Max(0, (workingArea.Width - Width) / 2);
        Top = Math.Max(0, (workingArea.Height - Height) / 2);
        WindowState = WindowState.Normal;
    }

    private void ApplyPresetSize(Rect workingArea)
    {
        var ratio = WindowSizePreset switch
        {
            WindowSizePreset.Compact => CompactSizeRatio,
            WindowSizePreset.Large => LargeSizeRatio,
            _ => StandardSizeRatio
        };

        Width = Math.Clamp(Math.Floor(workingArea.Width * ratio), MinimumWindowWidth, workingArea.Width);
        Height = Math.Clamp(Math.Floor(workingArea.Height * ratio), MinimumWindowHeight, workingArea.Height);
    }

    private static WindowSizePreset GetDefaultPreset(Rect workingArea)
    {
        // 4K 以上の作業領域を検出した場合は初期値を広めにする
        return workingArea is { Width: >= 3200, Height: >= 1700 }
            ? WindowSizePreset.Large
            : WindowSizePreset.Standard;
    }

    public void ApplyToViewModel(AutoTool.Desktop.MainWindowViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);

        viewModel.RestoreSessionState(
            SelectedTabIndex,
            IsFavoritePanelOpen,
            IsLogPanelOpen,
            IsVariablePanelOpen,
            IsAssistantPanelOpen,
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
        IsVariablePanelOpen = false;
        IsAssistantPanelOpen = false;
        LastOpenedMacroFilePath = null;
    }
}
