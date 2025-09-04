using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using AutoTool.Services.Configuration;
using AutoTool.Services.UI;

namespace AutoTool.Tests.Integration
{
    /// <summary>
    /// 設定サービス統合テストサンプル
    /// </summary>
    public class ConfigurationServiceIntegrationTest
    {
        /// <summary>
        /// 設定サービスの基本動作テスト
        /// </summary>
        public static void TestBasicConfiguration()
        {
            // DIコンテナの構築（テスト用）
            var services = new ServiceCollection();
            services.AddLogging(builder => builder.AddConsole());
            services.AddSingleton<IEnhancedConfigurationService, EnhancedConfigurationService>();
            
            var serviceProvider = services.BuildServiceProvider();
            var configService = serviceProvider.GetRequiredService<IEnhancedConfigurationService>();
            
            // 設定の読み書きテスト
            configService.SetValue("Test:StringValue", "Hello AutoTool");
            configService.SetValue("Test:IntValue", 42);
            configService.SetValue("Test:BoolValue", true);
            configService.SetValue("Test:DoubleValue", 3.14);
            
            // 値の取得テスト
            var stringValue = configService.GetValue("Test:StringValue", "default");
            var intValue = configService.GetValue("Test:IntValue", 0);
            var boolValue = configService.GetValue("Test:BoolValue", false);
            var doubleValue = configService.GetValue("Test:DoubleValue", 0.0);
            
            System.Diagnostics.Debug.WriteLine($"String: {stringValue}");
            System.Diagnostics.Debug.WriteLine($"Int: {intValue}");
            System.Diagnostics.Debug.WriteLine($"Bool: {boolValue}");
            System.Diagnostics.Debug.WriteLine($"Double: {doubleValue}");
            
            // ファイル保存テスト
            configService.Save();
            
            System.Diagnostics.Debug.WriteLine("設定サービステスト完了");
        }

        /// <summary>
        /// UIStateServiceとの連携テスト
        /// </summary>
        public static void TestUIStateServiceIntegration()
        {
            // DIコンテナの構築
            var services = new ServiceCollection();
            services.AddLogging(builder => builder.AddConsole());
            services.AddSingleton<IEnhancedConfigurationService, EnhancedConfigurationService>();
            services.AddSingleton<IUIStateService, UIStateService>();
            
            var serviceProvider = services.BuildServiceProvider();
            var uiStateService = serviceProvider.GetRequiredService<IUIStateService>();
            
            // ウィンドウ設定の変更テスト
            uiStateService.WindowWidth = 1400;
            uiStateService.WindowHeight = 900;
            uiStateService.WindowState = System.Windows.WindowState.Maximized;
            
            // ログエントリの追加テスト
            uiStateService.AddLogEntry("テストログエントリ1");
            uiStateService.AddLogEntry("テストログエントリ2");
            uiStateService.AddLogEntry("統合テスト実行中");
            
            System.Diagnostics.Debug.WriteLine($"ログエントリ数: {uiStateService.LogEntries.Count}");
            System.Diagnostics.Debug.WriteLine($"ウィンドウサイズ: {uiStateService.WindowWidth}x{uiStateService.WindowHeight}");
            
            System.Diagnostics.Debug.WriteLine("UIStateService統合テスト完了");
        }

        /// <summary>
        /// テーマ・設定サービス統合テスト
        /// </summary>
        public static void TestThemeServiceIntegration()
        {
            // DIコンテナの構築
            var services = new ServiceCollection();
            services.AddLogging(builder => builder.AddConsole());
            services.AddSingleton<IEnhancedConfigurationService, EnhancedConfigurationService>();
            services.AddSingleton<IEnhancedThemeService, EnhancedThemeService>();
            services.AddSingleton<IAppSettingsService, AppSettingsService>();
            
            var serviceProvider = services.BuildServiceProvider();
            var themeService = serviceProvider.GetRequiredService<IEnhancedThemeService>();
            var appSettingsService = serviceProvider.GetRequiredService<IAppSettingsService>();
            
            // テーマ変更イベントのテスト
            themeService.ThemeChanged += (s, e) =>
            {
                System.Diagnostics.Debug.WriteLine($"テーマ変更: {e.OldTheme} → {e.NewTheme}");
            };
            
            // 言語変更イベントのテスト
            themeService.LanguageChanged += (s, e) =>
            {
                System.Diagnostics.Debug.WriteLine($"言語変更: {e.OldLanguage} → {e.NewLanguage}");
            };
            
            // テーマと言語の変更テスト
            themeService.SetTheme(AppTheme.Dark);
            themeService.SetLanguage("en-US");
            
            // アプリ設定の変更テスト
            appSettingsService.AutoSave = false;
            appSettingsService.AutoSaveInterval = 600;
            appSettingsService.DefaultTimeout = 10000;
            
            System.Diagnostics.Debug.WriteLine($"現在のテーマ: {themeService.CurrentTheme}");
            System.Diagnostics.Debug.WriteLine($"現在の言語: {themeService.CurrentLanguage}");
            System.Diagnostics.Debug.WriteLine($"自動保存: {appSettingsService.AutoSave}");
            System.Diagnostics.Debug.WriteLine($"自動保存間隔: {appSettingsService.AutoSaveInterval}秒");
            
            System.Diagnostics.Debug.WriteLine("テーマサービス統合テスト完了");
        }

        /// <summary>
        /// テーマ・設定サービス統合テスト（強化版）
        /// </summary>
        public static void TestEnhancedThemeServiceIntegration()
        {
            // DIコンテナの構築
            var services = new ServiceCollection();
            services.AddLogging(builder => builder.AddConsole());
            services.AddSingleton<IEnhancedConfigurationService, EnhancedConfigurationService>();
            services.AddSingleton<IEnhancedThemeService, EnhancedThemeService>();
            services.AddSingleton<IAppSettingsService, AppSettingsService>();
            
            var serviceProvider = services.BuildServiceProvider();
            var themeService = serviceProvider.GetRequiredService<IEnhancedThemeService>();
            var appSettingsService = serviceProvider.GetRequiredService<IAppSettingsService>();
            
            // テーマ変更イベントのテスト
            themeService.ThemeChanged += (s, e) =>
            {
                System.Diagnostics.Debug.WriteLine($"テーマ変更: {e.OldTheme} → {e.NewTheme} ({e.NewThemeDefinition.DisplayName})");
            };
            
            // 言語変更イベントのテスト
            themeService.LanguageChanged += (s, e) =>
            {
                System.Diagnostics.Debug.WriteLine($"言語変更: {e.OldLanguage} → {e.NewLanguage}");
            };
            
            // すべての利用可能テーマをテスト
            var availableThemes = themeService.GetAvailableThemes();
            System.Diagnostics.Debug.WriteLine($"利用可能テーマ: {string.Join(", ", availableThemes)}");
            
            // 各テーマの定義情報を確認
            foreach (var theme in availableThemes)
            {
                var definition = themeService.GetThemeDefinition(theme);
                System.Diagnostics.Debug.WriteLine($"テーマ定義 [{theme}]: {definition.DisplayName} - {definition.Description}");
            }
            
            // テーマの切り替えテスト
            themeService.SetTheme(AppTheme.Dark);
            System.Diagnostics.Debug.WriteLine($"現在のテーマ: {themeService.CurrentTheme} ({themeService.CurrentThemeDefinition.DisplayName})");
            
            themeService.SetTheme(AppTheme.Light);
            System.Diagnostics.Debug.WriteLine($"現在のテーマ: {themeService.CurrentTheme} ({themeService.CurrentThemeDefinition.DisplayName})");
            
            themeService.SetTheme(AppTheme.Auto);
            System.Diagnostics.Debug.WriteLine($"現在のテーマ: {themeService.CurrentTheme} ({themeService.CurrentThemeDefinition.DisplayName})");
            
            // 言語の変更テスト
            themeService.SetLanguage("en-US");
            themeService.SetLanguage("ja-JP");
            
            // アプリ設定のテスト
            appSettingsService.AutoSave = false;
            appSettingsService.AutoSaveInterval = 600;
            appSettingsService.DefaultTimeout = 10000;
            
            System.Diagnostics.Debug.WriteLine($"現在の言語: {themeService.CurrentLanguage}");
            System.Diagnostics.Debug.WriteLine($"自動保存: {appSettingsService.AutoSave}");
            System.Diagnostics.Debug.WriteLine($"自動保存間隔: {appSettingsService.AutoSaveInterval}秒");
            
            System.Diagnostics.Debug.WriteLine("強化されたテーマサービス統合テスト完了");
        }

        /// <summary>
        /// 全体統合テストの実行（更新版）
        /// </summary>
        public static void RunAllTests()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("=== 設定サービス統合テスト開始 ===");
                
                TestBasicConfiguration();
                TestUIStateServiceIntegration();
                TestEnhancedThemeServiceIntegration();
                
                System.Diagnostics.Debug.WriteLine("=== すべてのテストが正常に完了しました ===");
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"テスト中にエラーが発生: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"スタックトレース: {ex.StackTrace}");
            }
        }
    }
}