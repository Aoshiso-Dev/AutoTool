using System.Text.Json;
using System.Windows;
using System.IO;

namespace AutoTool.Desktop.Model;

public class WindowSettings
{
    private TimeProvider _timeProvider = TimeProvider.System;

    public double Left { get; set; } = 100;
    public double Top { get; set; } = 100;
    public double Width { get; set; } = 1000;
    public double Height { get; set; } = 700;
    public WindowState WindowState { get; set; } = WindowState.Normal;
    public double EditPanelSplitterPosition { get; set; } = 300;
    public int SelectedTabIndex { get; set; }

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

    public void ApplyToWindow(Window window)
    {
        window.Left = Left;
        window.Top = Top;
        window.Width = Width;
        window.Height = Height;
        window.WindowState = WindowState;
    }
}
