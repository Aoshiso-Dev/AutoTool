using System;
using System.ComponentModel;
using System.IO;
using System.Text.Json;
using System.Windows;

namespace AutoTool.Model
{
    /// <summary>
    /// ウィンドウの設定を管理するクラス
    /// </summary>
    public class WindowSettings
    {
        public double Left { get; set; } = 100;
        public double Top { get; set; } = 100;
        public double Width { get; set; } = 1000;
        public double Height { get; set; } = 700;
        public WindowState WindowState { get; set; } = WindowState.Normal;

        // EditPanelのスプリッター位置（将来の拡張用）
        public double EditPanelSplitterPosition { get; set; } = 300;
        
        // その他のUI設定（将来の拡張用）
        public int SelectedTabIndex { get; set; } = 0;

        // 最後に開いたファイルのパス
        public string LastOpenedFilePath { get; set; } = string.Empty;
        
        // 起動時に前回のファイルを開くかどうか
        public bool OpenLastFileOnStartup { get; set; } = true;

        private static readonly string SettingsDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
            "AutoTool");
        private static readonly string SettingsFilePath = Path.Combine(SettingsDirectory, "window_settings.json");

        /// <summary>
        /// 設定をファイルに保存
        /// </summary>
        public void Save()
        {
            try
            {
                // ディレクトリが存在しない場合は作成
                if (!Directory.Exists(SettingsDirectory))
                {
                    Directory.CreateDirectory(SettingsDirectory);
                    System.Diagnostics.Debug.WriteLine($"設定ディレクトリを作成: {SettingsDirectory}");
                }

                var json = JsonSerializer.Serialize(this, new JsonSerializerOptions 
                { 
                    WriteIndented = true 
                });
                File.WriteAllText(SettingsFilePath, json);
                
                System.Diagnostics.Debug.WriteLine($"ウィンドウ設定を保存しました: {SettingsFilePath}");
                System.Diagnostics.Debug.WriteLine($"保存された LastOpenedFilePath: '{LastOpenedFilePath}'");
            }
            catch (Exception ex)
            {
                // 設定保存エラーはアプリケーションの動作に影響しないため、ログのみ出力
                System.Diagnostics.Debug.WriteLine($"ウィンドウ設定の保存に失敗しました: {ex.Message}");
            }
        }

        /// <summary>
        /// 設定をファイルから読み込み
        /// </summary>
        public static WindowSettings Load()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"設定ファイルの読み込みを試行: {SettingsFilePath}");
                
                if (File.Exists(SettingsFilePath))
                {
                    var json = File.ReadAllText(SettingsFilePath);
                    var settings = JsonSerializer.Deserialize<WindowSettings>(json);
                    if (settings != null)
                    {
                        // 画面範囲外の場合は調整
                        ValidatePosition(settings);
                        System.Diagnostics.Debug.WriteLine($"ウィンドウ設定を読み込みました: {SettingsFilePath}");
                        System.Diagnostics.Debug.WriteLine($"読み込まれた LastOpenedFilePath: '{settings.LastOpenedFilePath}'");
                        return settings;
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("設定ファイルが存在しません");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ウィンドウ設定の読み込みに失敗しました: {ex.Message}");
            }

            // デフォルト設定を返す
            System.Diagnostics.Debug.WriteLine("デフォルトのウィンドウ設定を使用します");
            return new WindowSettings();
        }

        /// <summary>
        /// ウィンドウ位置が画面範囲内かどうかを検証し、必要に応じて調整
        /// </summary>
        private static void ValidatePosition(WindowSettings settings)
        {
            // システムの作業領域を取得
            var workingArea = SystemParameters.WorkArea;

            // ウィンドウが画面外に出ている場合は調整
            if (settings.Left < 0 || settings.Left + settings.Width > workingArea.Width)
            {
                settings.Left = Math.Max(0, (workingArea.Width - settings.Width) / 2);
            }

            if (settings.Top < 0 || settings.Top + settings.Height > workingArea.Height)
            {
                settings.Top = Math.Max(0, (workingArea.Height - settings.Height) / 2);
            }

            // サイズが小さすぎる場合は調整
            if (settings.Width < 600)
            {
                settings.Width = 1000;
            }

            if (settings.Height < 400)
            {
                settings.Height = 700;
            }
            
            // EditPanelスプリッター位置の検証
            if (settings.EditPanelSplitterPosition < 200 || settings.EditPanelSplitterPosition > 600)
            {
                settings.EditPanelSplitterPosition = 300;
            }
        }

        /// <summary>
        /// Windowオブジェクトから設定を更新
        /// </summary>
        public void UpdateFromWindow(Window window)
        {
            if (window.WindowState == WindowState.Normal)
            {
                Left = window.Left;
                Top = window.Top;
                Width = window.Width;
                Height = window.Height;
                
                System.Diagnostics.Debug.WriteLine($"ウィンドウ設定を更新: Left={Left}, Top={Top}, Width={Width}, Height={Height}");
            }
            WindowState = window.WindowState;
        }

        /// <summary>
        /// Windowオブジェクトに設定を適用
        /// </summary>
        public void ApplyToWindow(Window window)
        {
            window.Left = Left;
            window.Top = Top;
            window.Width = Width;
            window.Height = Height;
            window.WindowState = WindowState;
            
            System.Diagnostics.Debug.WriteLine($"ウィンドウ設定を適用: Left={Left}, Top={Top}, Width={Width}, Height={Height}");
        }

        /// <summary>
        /// 最後に開いたファイルのパスを更新
        /// </summary>
        public void UpdateLastOpenedFile(string filePath)
        {
            var oldPath = LastOpenedFilePath;
            LastOpenedFilePath = filePath ?? string.Empty;
            System.Diagnostics.Debug.WriteLine($"最後に開いたファイルを更新: '{oldPath}' -> '{LastOpenedFilePath}'");
        }

        /// <summary>
        /// 最後に開いたファイルが存在するかチェック
        /// </summary>
        public bool IsLastOpenedFileValid()
        {
            var isValid = !string.IsNullOrEmpty(LastOpenedFilePath) && File.Exists(LastOpenedFilePath);
            System.Diagnostics.Debug.WriteLine($"LastOpenedFile 有効性チェック: '{LastOpenedFilePath}' -> {isValid}");
            return isValid;
        }
    }
}