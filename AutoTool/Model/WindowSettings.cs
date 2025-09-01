using System;
using System.ComponentModel;
using System.IO;
using System.Text.Json;
using System.Windows;

namespace AutoTool.Model
{
    /// <summary>
    /// �E�B���h�E�̐ݒ���Ǘ�����N���X
    /// </summary>
    public class WindowSettings
    {
        public double Left { get; set; } = 100;
        public double Top { get; set; } = 100;
        public double Width { get; set; } = 1000;
        public double Height { get; set; } = 700;
        public WindowState WindowState { get; set; } = WindowState.Normal;

        // EditPanel�̃X�v���b�^�[�ʒu�i�����̊g���p�j
        public double EditPanelSplitterPosition { get; set; } = 300;
        
        // ���̑���UI�ݒ�i�����̊g���p�j
        public int SelectedTabIndex { get; set; } = 0;

        // �Ō�ɊJ�����t�@�C���̃p�X
        public string LastOpenedFilePath { get; set; } = string.Empty;
        
        // �N�����ɑO��̃t�@�C�����J�����ǂ���
        public bool OpenLastFileOnStartup { get; set; } = true;

        private static readonly string SettingsDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
            "AutoTool");
        private static readonly string SettingsFilePath = Path.Combine(SettingsDirectory, "window_settings.json");

        /// <summary>
        /// �ݒ���t�@�C���ɕۑ�
        /// </summary>
        public void Save()
        {
            try
            {
                // �f�B���N�g�������݂��Ȃ��ꍇ�͍쐬
                if (!Directory.Exists(SettingsDirectory))
                {
                    Directory.CreateDirectory(SettingsDirectory);
                    System.Diagnostics.Debug.WriteLine($"�ݒ�f�B���N�g�����쐬: {SettingsDirectory}");
                }

                var json = JsonSerializer.Serialize(this, new JsonSerializerOptions 
                { 
                    WriteIndented = true 
                });
                File.WriteAllText(SettingsFilePath, json);
                
                System.Diagnostics.Debug.WriteLine($"�E�B���h�E�ݒ��ۑ����܂���: {SettingsFilePath}");
                System.Diagnostics.Debug.WriteLine($"�ۑ����ꂽ LastOpenedFilePath: '{LastOpenedFilePath}'");
            }
            catch (Exception ex)
            {
                // �ݒ�ۑ��G���[�̓A�v���P�[�V�����̓���ɉe�����Ȃ����߁A���O�̂ݏo��
                System.Diagnostics.Debug.WriteLine($"�E�B���h�E�ݒ�̕ۑ��Ɏ��s���܂���: {ex.Message}");
            }
        }

        /// <summary>
        /// �ݒ���t�@�C������ǂݍ���
        /// </summary>
        public static WindowSettings Load()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"�ݒ�t�@�C���̓ǂݍ��݂����s: {SettingsFilePath}");
                
                if (File.Exists(SettingsFilePath))
                {
                    var json = File.ReadAllText(SettingsFilePath);
                    var settings = JsonSerializer.Deserialize<WindowSettings>(json);
                    if (settings != null)
                    {
                        // ��ʔ͈͊O�̏ꍇ�͒���
                        ValidatePosition(settings);
                        System.Diagnostics.Debug.WriteLine($"�E�B���h�E�ݒ��ǂݍ��݂܂���: {SettingsFilePath}");
                        System.Diagnostics.Debug.WriteLine($"�ǂݍ��܂ꂽ LastOpenedFilePath: '{settings.LastOpenedFilePath}'");
                        return settings;
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("�ݒ�t�@�C�������݂��܂���");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"�E�B���h�E�ݒ�̓ǂݍ��݂Ɏ��s���܂���: {ex.Message}");
            }

            // �f�t�H���g�ݒ��Ԃ�
            System.Diagnostics.Debug.WriteLine("�f�t�H���g�̃E�B���h�E�ݒ���g�p���܂�");
            return new WindowSettings();
        }

        /// <summary>
        /// �E�B���h�E�ʒu����ʔ͈͓����ǂ��������؂��A�K�v�ɉ����Ē���
        /// </summary>
        private static void ValidatePosition(WindowSettings settings)
        {
            // �V�X�e���̍�Ɨ̈���擾
            var workingArea = SystemParameters.WorkArea;

            // �E�B���h�E����ʊO�ɏo�Ă���ꍇ�͒���
            if (settings.Left < 0 || settings.Left + settings.Width > workingArea.Width)
            {
                settings.Left = Math.Max(0, (workingArea.Width - settings.Width) / 2);
            }

            if (settings.Top < 0 || settings.Top + settings.Height > workingArea.Height)
            {
                settings.Top = Math.Max(0, (workingArea.Height - settings.Height) / 2);
            }

            // �T�C�Y������������ꍇ�͒���
            if (settings.Width < 600)
            {
                settings.Width = 1000;
            }

            if (settings.Height < 400)
            {
                settings.Height = 700;
            }
            
            // EditPanel�X�v���b�^�[�ʒu�̌���
            if (settings.EditPanelSplitterPosition < 200 || settings.EditPanelSplitterPosition > 600)
            {
                settings.EditPanelSplitterPosition = 300;
            }
        }

        /// <summary>
        /// Window�I�u�W�F�N�g����ݒ���X�V
        /// </summary>
        public void UpdateFromWindow(Window window)
        {
            if (window.WindowState == WindowState.Normal)
            {
                Left = window.Left;
                Top = window.Top;
                Width = window.Width;
                Height = window.Height;
                
                System.Diagnostics.Debug.WriteLine($"�E�B���h�E�ݒ���X�V: Left={Left}, Top={Top}, Width={Width}, Height={Height}");
            }
            WindowState = window.WindowState;
        }

        /// <summary>
        /// Window�I�u�W�F�N�g�ɐݒ��K�p
        /// </summary>
        public void ApplyToWindow(Window window)
        {
            window.Left = Left;
            window.Top = Top;
            window.Width = Width;
            window.Height = Height;
            window.WindowState = WindowState;
            
            System.Diagnostics.Debug.WriteLine($"�E�B���h�E�ݒ��K�p: Left={Left}, Top={Top}, Width={Width}, Height={Height}");
        }

        /// <summary>
        /// �Ō�ɊJ�����t�@�C���̃p�X���X�V
        /// </summary>
        public void UpdateLastOpenedFile(string filePath)
        {
            var oldPath = LastOpenedFilePath;
            LastOpenedFilePath = filePath ?? string.Empty;
            System.Diagnostics.Debug.WriteLine($"�Ō�ɊJ�����t�@�C�����X�V: '{oldPath}' -> '{LastOpenedFilePath}'");
        }

        /// <summary>
        /// �Ō�ɊJ�����t�@�C�������݂��邩�`�F�b�N
        /// </summary>
        public bool IsLastOpenedFileValid()
        {
            var isValid = !string.IsNullOrEmpty(LastOpenedFilePath) && File.Exists(LastOpenedFilePath);
            System.Diagnostics.Debug.WriteLine($"LastOpenedFile �L�����`�F�b�N: '{LastOpenedFilePath}' -> {isValid}");
            return isValid;
        }
    }
}