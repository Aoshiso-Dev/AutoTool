using System;
using System.Collections.Generic;
using System.Linq;
using AutoTool.Model.List.Interface;
using AutoTool.Model.List.Type;

namespace AutoTool.Model.CommandDefinition
{
    /// <summary>
    /// Phase 5���S�����ŁF�R�}���h���W�X�g��
    /// MacroPanels�ˑ����폜���AAutoTool�����ł̂ݎg�p
    /// </summary>
    public static class CommandRegistry
    {
        private static readonly Dictionary<string, Type> _commandTypes = new();
        private static bool _initialized = false;

        /// <summary>
        /// �R�}���h�^�C�v�̒萔�iPhase 5�����Łj
        /// </summary>
        public static class CommandTypes
        {
            public const string Click = "Click";
            public const string ClickImage = "Click_Image";
            public const string ClickImageAI = "Click_Image_AI";
            public const string Wait = "Wait";
            public const string WaitImage = "Wait_Image";
            public const string Hotkey = "Hotkey";
            public const string Loop = "Loop";
            public const string LoopEnd = "Loop_End";
            public const string LoopBreak = "Loop_Break";
            public const string IfImageExist = "IF_ImageExist";
            public const string IfImageNotExist = "IF_ImageNotExist";
            public const string IfImageExistAI = "IF_ImageExist_AI";
            public const string IfImageNotExistAI = "IF_ImageNotExist_AI";
            public const string IfEnd = "IF_End";
            public const string Execute = "Execute";
            public const string SetVariable = "SetVariable";
            public const string SetVariableAI = "SetVariable_AI";
            public const string IfVariable = "IF_Variable";
            public const string Screenshot = "Screenshot";
            
            // �ǉ��̊�{�R�}���h
            public const string DoubleClick = "DoubleClick";
            public const string RightClick = "RightClick";
            public const string Drag = "Drag";
            public const string Scroll = "Scroll";
            public const string TypeText = "TypeText";
            public const string KeyPress = "KeyPress";
            public const string KeyCombo = "KeyCombo";
            public const string FindImage = "FindImage";
            public const string CaptureImage = "CaptureImage";
            public const string ActivateWindow = "ActivateWindow";
            public const string CloseWindow = "CloseWindow";
            public const string MoveWindow = "MoveWindow";
            public const string ResizeWindow = "ResizeWindow";
            public const string OpenFile = "OpenFile";
            public const string SaveFile = "SaveFile";
            public const string CopyFile = "CopyFile";
            public const string DeleteFile = "DeleteFile";
            public const string IfElse = "IfElse";
            public const string GetVariable = "GetVariable";
            public const string CalculateExpression = "CalculateExpression";
            public const string RandomNumber = "RandomNumber";
            public const string Command = "Command";
            public const string Beep = "Beep";
            public const string HttpRequest = "HttpRequest";
            public const string DownloadFile = "DownloadFile";
            public const string SendEmail = "SendEmail";
            public const string ImageAI = "ImageAI";
            public const string TextOCR = "TextOCR";
            public const string VoiceRecognition = "VoiceRecognition";
            public const string LogMessage = "LogMessage";
            public const string Assert = "Assert";
            public const string Breakpoint = "Breakpoint";
            public const string Comment = "Comment";
        }

        /// <summary>
        /// �\�������ƃJ�e�S�����
        /// </summary>
        public static class DisplayOrder
        {
            private static readonly Dictionary<string, (string DisplayName, string Category, int Order)> _displayInfo = new()
            {
                // ��{����
                { CommandTypes.Wait, ("�ҋ@", "��{����", 10) },
                { CommandTypes.Click, ("�N���b�N", "��{����", 20) },
                { CommandTypes.DoubleClick, ("�_�u���N���b�N", "��{����", 21) },
                { CommandTypes.RightClick, ("�E�N���b�N", "��{����", 22) },
                { CommandTypes.Drag, ("�h���b�O", "��{����", 30) },
                { CommandTypes.Scroll, ("�X�N���[��", "��{����", 40) },
                
                // �L�[�{�[�h����
                { CommandTypes.Hotkey, ("�z�b�g�L�[", "�L�[�{�[�h", 50) },
                { CommandTypes.TypeText, ("�e�L�X�g����", "�L�[�{�[�h", 60) },
                { CommandTypes.KeyPress, ("�L�[����", "�L�[�{�[�h", 70) },
                { CommandTypes.KeyCombo, ("�L�[�g�ݍ��킹", "�L�[�{�[�h", 80) },
                
                // �摜�F��
                { CommandTypes.WaitImage, ("�摜�ҋ@", "�摜�F��", 90) },
                { CommandTypes.ClickImage, ("�摜�N���b�N", "�摜�F��", 100) },
                { CommandTypes.FindImage, ("�摜����", "�摜�F��", 110) },
                { CommandTypes.CaptureImage, ("�摜�L���v�`��", "�摜�F��", 120) },
                
                // �E�B���h�E����
                { CommandTypes.ActivateWindow, ("�E�B���h�E�A�N�e�B�u", "�E�B���h�E", 130) },
                { CommandTypes.CloseWindow, ("�E�B���h�E����", "�E�B���h�E", 140) },
                { CommandTypes.MoveWindow, ("�E�B���h�E�ړ�", "�E�B���h�E", 150) },
                { CommandTypes.ResizeWindow, ("�E�B���h�E�T�C�Y�ύX", "�E�B���h�E", 160) },
                
                // �t�@�C������
                { CommandTypes.OpenFile, ("�t�@�C���J��", "�t�@�C��", 170) },
                { CommandTypes.SaveFile, ("�t�@�C���ۑ�", "�t�@�C��", 180) },
                { CommandTypes.CopyFile, ("�t�@�C���R�s�[", "�t�@�C��", 190) },
                { CommandTypes.DeleteFile, ("�t�@�C���폜", "�t�@�C��", 200) },
                
                // ����\��
                { CommandTypes.Loop, ("���[�v", "����", 210) },
                { CommandTypes.LoopEnd, ("���[�v�I��", "����", 211) },
                { CommandTypes.LoopBreak, ("���[�v���f", "����", 212) },
                { CommandTypes.IfImageExist, ("�摜���ݔ���", "��������", 220) },
                { CommandTypes.IfImageNotExist, ("�摜�񑶍ݔ���", "��������", 221) },
                { CommandTypes.IfVariable, ("�ϐ���������", "��������", 222) },
                { CommandTypes.IfEnd, ("�����I��", "��������", 223) },
                { CommandTypes.IfElse, ("�����łȂ����", "��������", 224) },
                
                // �ϐ��E�f�[�^
                { CommandTypes.SetVariable, ("�ϐ��ݒ�", "�ϐ�", 230) },
                { CommandTypes.GetVariable, ("�ϐ��擾", "�ϐ�", 240) },
                { CommandTypes.CalculateExpression, ("�v�Z��", "�ϐ�", 250) },
                { CommandTypes.RandomNumber, ("��������", "�ϐ�", 260) },
                
                // �V�X�e��
                { CommandTypes.Execute, ("�v���O�������s", "�V�X�e��", 270) },
                { CommandTypes.Command, ("�R�}���h���s", "�V�X�e��", 280) },
                { CommandTypes.Screenshot, ("�X�N���[���V���b�g", "�V�X�e��", 290) },
                { CommandTypes.Beep, ("�r�[�v��", "�V�X�e��", 300) },
                
                // �l�b�g���[�N
                { CommandTypes.HttpRequest, ("HTTP�v��", "�l�b�g���[�N", 310) },
                { CommandTypes.DownloadFile, ("�t�@�C���_�E�����[�h", "�l�b�g���[�N", 320) },
                { CommandTypes.SendEmail, ("���[�����M", "�l�b�g���[�N", 330) },
                
                // AI�E������
                { CommandTypes.ClickImageAI, ("AI�摜�N���b�N", "AI", 340) },
                { CommandTypes.IfImageExistAI, ("AI�摜���ݔ���", "AI", 341) },
                { CommandTypes.IfImageNotExistAI, ("AI�摜�񑶍ݔ���", "AI", 342) },
                { CommandTypes.SetVariableAI, ("AI�ϐ��ݒ�", "AI", 343) },
                { CommandTypes.ImageAI, ("AI�摜�F��", "AI", 344) },
                { CommandTypes.TextOCR, ("�����F��", "AI", 350) },
                { CommandTypes.VoiceRecognition, ("�����F��", "AI", 360) },
                
                // �f�o�b�O�E�e�X�g
                { CommandTypes.LogMessage, ("���O�o��", "�f�o�b�O", 370) },
                { CommandTypes.Assert, ("�A�T�[�V����", "�f�o�b�O", 380) },
                { CommandTypes.Breakpoint, ("�u���[�N�|�C���g", "�f�o�b�O", 390) },
                { CommandTypes.Comment, ("�R�����g", "�f�o�b�O", 400) }
            };

            public static string GetDisplayName(string typeName)
            {
                return _displayInfo.TryGetValue(typeName, out var info) ? info.DisplayName : typeName;
            }

            public static string GetCategoryName(string typeName)
            {
                return _displayInfo.TryGetValue(typeName, out var info) ? info.Category : "���̑�";
            }

            public static int GetOrder(string typeName)
            {
                return _displayInfo.TryGetValue(typeName, out var info) ? info.Order : 999;
            }

            public static IEnumerable<string> GetOrderedTypeNames()
            {
                return _displayInfo
                    .OrderBy(kvp => kvp.Value.Order)
                    .ThenBy(kvp => kvp.Value.Category)
                    .ThenBy(kvp => kvp.Value.DisplayName)
                    .Select(kvp => kvp.Key);
            }

            public static IEnumerable<string> GetCategorizedTypeNames()
            {
                return _displayInfo
                    .GroupBy(kvp => kvp.Value.Category)
                    .OrderBy(g => g.Key)
                    .SelectMany(g => g.OrderBy(kvp => kvp.Value.Order).Select(kvp => kvp.Key));
            }
        }

        /// <summary>
        /// CommandRegistry��������
        /// </summary>
        public static void Initialize()
        {
            if (_initialized) return;

            try
            {
                // BasicCommandItem����{�R�}���h�^�C�v�Ƃ��ēo�^
                _commandTypes.Clear();
                
                // ��{�R�}���h�^�C�v��o�^
                foreach (var typeName in DisplayOrder.GetOrderedTypeNames())
                {
                    _commandTypes[typeName] = typeof(BasicCommandItem);
                }

                _initialized = true;
                System.Diagnostics.Debug.WriteLine($"[CommandRegistry] ����������: {_commandTypes.Count}�̃R�}���h�^�C�v��o�^");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CommandRegistry] �������G���[: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// �R�}���h�A�C�e�����쐬
        /// </summary>
        public static ICommandListItem? CreateCommandItem(string typeName)
        {
            try
            {
                if (!_initialized)
                {
                    Initialize();
                }

                if (_commandTypes.TryGetValue(typeName, out var type))
                {
                    if (Activator.CreateInstance(type) is ICommandListItem item)
                    {
                        item.ItemType = typeName;
                        item.Comment = $"{DisplayOrder.GetDisplayName(typeName)}�R�}���h";
                        item.IsEnable = true;
                        
                        System.Diagnostics.Debug.WriteLine($"[CommandRegistry] �R�}���h�A�C�e���쐬: {typeName}");
                        return item;
                    }
                }

                System.Diagnostics.Debug.WriteLine($"[CommandRegistry] ���m�̃R�}���h�^�C�v: {typeName}");
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CommandRegistry] �R�}���h�A�C�e���쐬�G���[: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// �o�^����Ă���R�}���h�^�C�v���������t���Ŏ擾
        /// </summary>
        public static IEnumerable<string> GetOrderedTypeNames()
        {
            if (!_initialized)
            {
                Initialize();
            }

            return DisplayOrder.GetOrderedTypeNames().Where(typeName => _commandTypes.ContainsKey(typeName));
        }

        /// <summary>
        /// �J�e�S���ʂ̃R�}���h�^�C�v�����擾
        /// </summary>
        public static IEnumerable<(string Category, IEnumerable<string> TypeNames)> GetCategorizedTypeNames()
        {
            if (!_initialized)
            {
                Initialize();
            }

            return DisplayOrder.GetOrderedTypeNames()
                .Where(typeName => _commandTypes.ContainsKey(typeName))
                .GroupBy(typeName => DisplayOrder.GetCategoryName(typeName))
                .OrderBy(g => g.Key)
                .Select(g => (g.Key, g.AsEnumerable()));
        }

        /// <summary>
        /// �R�}���h�^�C�v���o�^����Ă��邩�`�F�b�N
        /// </summary>
        public static bool IsRegistered(string typeName)
        {
            if (!_initialized)
            {
                Initialize();
            }

            return _commandTypes.ContainsKey(typeName);
        }

        /// <summary>
        /// �J�n�R�}���h���ǂ�������
        /// </summary>
        public static bool IsStartCommand(string typeName)
        {
            return typeName == CommandTypes.Loop ||
                   IsIfCommand(typeName);
        }

        /// <summary>
        /// �I���R�}���h���ǂ�������
        /// </summary>
        public static bool IsEndCommand(string typeName)
        {
            return typeName == CommandTypes.LoopEnd ||
                   typeName == CommandTypes.IfEnd;
        }

        /// <summary>
        /// If�R�}���h���ǂ�������
        /// </summary>
        public static bool IsIfCommand(string typeName)
        {
            return typeName == CommandTypes.IfImageExist ||
                   typeName == CommandTypes.IfImageNotExist ||
                   typeName == CommandTypes.IfImageExistAI ||
                   typeName == CommandTypes.IfImageNotExistAI ||
                   typeName == CommandTypes.IfVariable;
        }

        /// <summary>
        /// Loop�R�}���h���ǂ�������
        /// </summary>
        public static bool IsLoopCommand(string typeName)
        {
            return typeName == CommandTypes.Loop;
        }

        /// <summary>
        /// �S�R�}���h�^�C�v���擾
        /// </summary>
        public static IEnumerable<string> GetAllTypeNames()
        {
            return _commandTypes.Keys;
        }
    }
}