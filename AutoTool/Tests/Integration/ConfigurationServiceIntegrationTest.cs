using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using AutoTool.Services.Configuration;
using AutoTool.Services.UI;

namespace AutoTool.Tests.Integration
{
    /// <summary>
    /// �ݒ�T�[�r�X�����e�X�g�T���v��
    /// </summary>
    public class ConfigurationServiceIntegrationTest
    {
        /// <summary>
        /// �ݒ�T�[�r�X�̊�{����e�X�g
        /// </summary>
        public static void TestBasicConfiguration()
        {
            // DI�R���e�i�̍\�z�i�e�X�g�p�j
            var services = new ServiceCollection();
            services.AddLogging(builder => builder.AddConsole());
            services.AddSingleton<IEnhancedConfigurationService, EnhancedConfigurationService>();
            
            var serviceProvider = services.BuildServiceProvider();
            var configService = serviceProvider.GetRequiredService<IEnhancedConfigurationService>();
            
            // �ݒ�̓ǂݏ����e�X�g
            configService.SetValue("Test:StringValue", "Hello AutoTool");
            configService.SetValue("Test:IntValue", 42);
            configService.SetValue("Test:BoolValue", true);
            configService.SetValue("Test:DoubleValue", 3.14);
            
            // �l�̎擾�e�X�g
            var stringValue = configService.GetValue("Test:StringValue", "default");
            var intValue = configService.GetValue("Test:IntValue", 0);
            var boolValue = configService.GetValue("Test:BoolValue", false);
            var doubleValue = configService.GetValue("Test:DoubleValue", 0.0);
            
            System.Diagnostics.Debug.WriteLine($"String: {stringValue}");
            System.Diagnostics.Debug.WriteLine($"Int: {intValue}");
            System.Diagnostics.Debug.WriteLine($"Bool: {boolValue}");
            System.Diagnostics.Debug.WriteLine($"Double: {doubleValue}");
            
            // �t�@�C���ۑ��e�X�g
            configService.Save();
            
            System.Diagnostics.Debug.WriteLine("�ݒ�T�[�r�X�e�X�g����");
        }

        /// <summary>
        /// UIStateService�Ƃ̘A�g�e�X�g
        /// </summary>
        public static void TestUIStateServiceIntegration()
        {
            // DI�R���e�i�̍\�z
            var services = new ServiceCollection();
            services.AddLogging(builder => builder.AddConsole());
            services.AddSingleton<IEnhancedConfigurationService, EnhancedConfigurationService>();
            services.AddSingleton<IUIStateService, UIStateService>();
            
            var serviceProvider = services.BuildServiceProvider();
            var uiStateService = serviceProvider.GetRequiredService<IUIStateService>();
            
            // �E�B���h�E�ݒ�̕ύX�e�X�g
            uiStateService.WindowWidth = 1400;
            uiStateService.WindowHeight = 900;
            uiStateService.WindowState = System.Windows.WindowState.Maximized;
            
            // ���O�G���g���̒ǉ��e�X�g
            uiStateService.AddLogEntry("�e�X�g���O�G���g��1");
            uiStateService.AddLogEntry("�e�X�g���O�G���g��2");
            uiStateService.AddLogEntry("�����e�X�g���s��");
            
            System.Diagnostics.Debug.WriteLine($"���O�G���g����: {uiStateService.LogEntries.Count}");
            System.Diagnostics.Debug.WriteLine($"�E�B���h�E�T�C�Y: {uiStateService.WindowWidth}x{uiStateService.WindowHeight}");
            
            System.Diagnostics.Debug.WriteLine("UIStateService�����e�X�g����");
        }

        /// <summary>
        /// �e�[�}�E�ݒ�T�[�r�X�����e�X�g
        /// </summary>
        public static void TestThemeServiceIntegration()
        {
            // DI�R���e�i�̍\�z
            var services = new ServiceCollection();
            services.AddLogging(builder => builder.AddConsole());
            services.AddSingleton<IEnhancedConfigurationService, EnhancedConfigurationService>();
            services.AddSingleton<IEnhancedThemeService, EnhancedThemeService>();
            services.AddSingleton<IAppSettingsService, AppSettingsService>();
            
            var serviceProvider = services.BuildServiceProvider();
            var themeService = serviceProvider.GetRequiredService<IEnhancedThemeService>();
            var appSettingsService = serviceProvider.GetRequiredService<IAppSettingsService>();
            
            // �e�[�}�ύX�C�x���g�̃e�X�g
            themeService.ThemeChanged += (s, e) =>
            {
                System.Diagnostics.Debug.WriteLine($"�e�[�}�ύX: {e.OldTheme} �� {e.NewTheme}");
            };
            
            // ����ύX�C�x���g�̃e�X�g
            themeService.LanguageChanged += (s, e) =>
            {
                System.Diagnostics.Debug.WriteLine($"����ύX: {e.OldLanguage} �� {e.NewLanguage}");
            };
            
            // �e�[�}�ƌ���̕ύX�e�X�g
            themeService.SetTheme(AppTheme.Dark);
            themeService.SetLanguage("en-US");
            
            // �A�v���ݒ�̕ύX�e�X�g
            appSettingsService.AutoSave = false;
            appSettingsService.AutoSaveInterval = 600;
            appSettingsService.DefaultTimeout = 10000;
            
            System.Diagnostics.Debug.WriteLine($"���݂̃e�[�}: {themeService.CurrentTheme}");
            System.Diagnostics.Debug.WriteLine($"���݂̌���: {themeService.CurrentLanguage}");
            System.Diagnostics.Debug.WriteLine($"�����ۑ�: {appSettingsService.AutoSave}");
            System.Diagnostics.Debug.WriteLine($"�����ۑ��Ԋu: {appSettingsService.AutoSaveInterval}�b");
            
            System.Diagnostics.Debug.WriteLine("�e�[�}�T�[�r�X�����e�X�g����");
        }

        /// <summary>
        /// �e�[�}�E�ݒ�T�[�r�X�����e�X�g�i�����Łj
        /// </summary>
        public static void TestEnhancedThemeServiceIntegration()
        {
            // DI�R���e�i�̍\�z
            var services = new ServiceCollection();
            services.AddLogging(builder => builder.AddConsole());
            services.AddSingleton<IEnhancedConfigurationService, EnhancedConfigurationService>();
            services.AddSingleton<IEnhancedThemeService, EnhancedThemeService>();
            services.AddSingleton<IAppSettingsService, AppSettingsService>();
            
            var serviceProvider = services.BuildServiceProvider();
            var themeService = serviceProvider.GetRequiredService<IEnhancedThemeService>();
            var appSettingsService = serviceProvider.GetRequiredService<IAppSettingsService>();
            
            // �e�[�}�ύX�C�x���g�̃e�X�g
            themeService.ThemeChanged += (s, e) =>
            {
                System.Diagnostics.Debug.WriteLine($"�e�[�}�ύX: {e.OldTheme} �� {e.NewTheme} ({e.NewThemeDefinition.DisplayName})");
            };
            
            // ����ύX�C�x���g�̃e�X�g
            themeService.LanguageChanged += (s, e) =>
            {
                System.Diagnostics.Debug.WriteLine($"����ύX: {e.OldLanguage} �� {e.NewLanguage}");
            };
            
            // ���ׂĂ̗��p�\�e�[�}���e�X�g
            var availableThemes = themeService.GetAvailableThemes();
            System.Diagnostics.Debug.WriteLine($"���p�\�e�[�}: {string.Join(", ", availableThemes)}");
            
            // �e�e�[�}�̒�`�����m�F
            foreach (var theme in availableThemes)
            {
                var definition = themeService.GetThemeDefinition(theme);
                System.Diagnostics.Debug.WriteLine($"�e�[�}��` [{theme}]: {definition.DisplayName} - {definition.Description}");
            }
            
            // �e�[�}�̐؂�ւ��e�X�g
            themeService.SetTheme(AppTheme.Dark);
            System.Diagnostics.Debug.WriteLine($"���݂̃e�[�}: {themeService.CurrentTheme} ({themeService.CurrentThemeDefinition.DisplayName})");
            
            themeService.SetTheme(AppTheme.Light);
            System.Diagnostics.Debug.WriteLine($"���݂̃e�[�}: {themeService.CurrentTheme} ({themeService.CurrentThemeDefinition.DisplayName})");
            
            themeService.SetTheme(AppTheme.Auto);
            System.Diagnostics.Debug.WriteLine($"���݂̃e�[�}: {themeService.CurrentTheme} ({themeService.CurrentThemeDefinition.DisplayName})");
            
            // ����̕ύX�e�X�g
            themeService.SetLanguage("en-US");
            themeService.SetLanguage("ja-JP");
            
            // �A�v���ݒ�̃e�X�g
            appSettingsService.AutoSave = false;
            appSettingsService.AutoSaveInterval = 600;
            appSettingsService.DefaultTimeout = 10000;
            
            System.Diagnostics.Debug.WriteLine($"���݂̌���: {themeService.CurrentLanguage}");
            System.Diagnostics.Debug.WriteLine($"�����ۑ�: {appSettingsService.AutoSave}");
            System.Diagnostics.Debug.WriteLine($"�����ۑ��Ԋu: {appSettingsService.AutoSaveInterval}�b");
            
            System.Diagnostics.Debug.WriteLine("�������ꂽ�e�[�}�T�[�r�X�����e�X�g����");
        }

        /// <summary>
        /// �S�̓����e�X�g�̎��s�i�X�V�Łj
        /// </summary>
        public static void RunAllTests()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("=== �ݒ�T�[�r�X�����e�X�g�J�n ===");
                
                TestBasicConfiguration();
                TestUIStateServiceIntegration();
                TestEnhancedThemeServiceIntegration();
                
                System.Diagnostics.Debug.WriteLine("=== ���ׂẴe�X�g������Ɋ������܂��� ===");
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"�e�X�g���ɃG���[������: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"�X�^�b�N�g���[�X: {ex.StackTrace}");
            }
        }
    }
}