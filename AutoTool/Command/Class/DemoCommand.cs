using AutoTool.Command.Class;
using AutoTool.Command.Interface;
using AutoTool.Model.CommandDefinition;
using Microsoft.Extensions.Logging;

namespace AutoTool.Command.Class
{
    [DirectCommand("Demo", "�f���R�}���h", description: "�f���p�̕�I�ȃR�}���h�ł�")]
    public class DemoCommand : BaseCommand
    {
        private readonly ILogger<DemoCommand>? _logger;

        [SettingProperty("���b�Z�[�W", SettingControlType.TextBox,
            category: "��{�ݒ�",
            description: "�\�����郁�b�Z�[�W",
            defaultValue: "Demo Command")]
        public string Message { get; set; } = "Demo Command";

        [SettingProperty("�ҋ@����", SettingControlType.NumberBox,
            category: "��{�ݒ�",
            description: "�ҋ@���ԁi�~���b�j",
            defaultValue: 500)]
        public int WaitTime { get; set; } = 500;

        [SettingProperty("�摜�t�@�C��", SettingControlType.FilePicker,
            description: "�Q�Ƃ���摜�t�@�C��",
            category: "�t�@�C��",
            fileFilter: "�摜�t�@�C�� (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp|���ׂẴt�@�C�� (*.*)|*.*")]
        public string ImagePath { get; set; } = string.Empty;

        [SettingProperty("�ۑ��t�H���_", SettingControlType.FolderPicker,
            description: "���ʂ�ۑ�����t�H���_",
            category: "�t�@�C��")]
        public string SaveFolder { get; set; } = string.Empty;

        [SettingProperty("�E�B���h�E�^�C�g��", SettingControlType.WindowPicker,
            description: "�ΏۃE�B���h�E�̃^�C�g��",
            category: "�E�B���h�E")]
        public string WindowTitle { get; set; } = string.Empty;

        [SettingProperty("X���W", SettingControlType.NumberBox,
            description: "X���W",
            category: "���W",
            defaultValue: 100)]
        public int X { get; set; } = 100;

        [SettingProperty("Y���W", SettingControlType.NumberBox,
            description: "Y���W",
            category: "���W",
            defaultValue: 100)]
        public int Y { get; set; } = 100;

        [SettingProperty("�}�E�X�ʒu�ݒ�", SettingControlType.CoordinatePicker,
            description: "�}�E�X�ʒu��ݒ�",
            category: "���W")]
        public System.Drawing.Point MousePosition { get; set; } = new(0, 0);

        [SettingProperty("�}�E�X�{�^��", SettingControlType.ComboBox,
            category: "����",
            description: "�g�p����}�E�X�{�^��",
            sourceCollection: "MouseButtons",
            defaultValue: System.Windows.Input.MouseButton.Left)]
        public System.Windows.Input.MouseButton MouseButton { get; set; } = System.Windows.Input.MouseButton.Left;

        [SettingProperty("�L�[", SettingControlType.ComboBox,
            category: "����",
            description: "�g�p����L�[",
            sourceCollection: "Keys",
            defaultValue: System.Windows.Input.Key.Enter)]
        public System.Windows.Input.Key Key { get; set; } = System.Windows.Input.Key.Enter;

        [SettingProperty("�L��", SettingControlType.CheckBox,
            category: "��{�ݒ�",
            description: "���̃R�}���h��L���ɂ���",
            defaultValue: true)]
        public bool IsActive { get; set; } = true;

        [SettingProperty("�������l", SettingControlType.Slider,
            category: "�ڍאݒ�",
            description: "�摜�F���̂������l",
            defaultValue: 0.8)]
        public double Threshold { get; set; } = 0.8;

        [SettingProperty("�p�X���[�h", SettingControlType.PasswordBox,
            category: "�F��",
            description: "�ڑ��p�p�X���[�h")]
        public string Password { get; set; } = string.Empty;

        [SettingProperty("ONNX���f��", SettingControlType.OnnxPicker,
            category: "AI",
            description: "�g�p����ONNX���f���t�@�C��")]
        public string OnnxModelPath { get; set; } = string.Empty;

        [SettingProperty("�J�n��", SettingControlType.DatePicker,
            category: "�X�P�W���[��",
            description: "�����J�n��")]
        public DateTime StartDate { get; set; } = DateTime.Today;

        [SettingProperty("���s����", SettingControlType.TimePicker,
            category: "�X�P�W���[��",
            description: "���s���鎞��")]
        public TimeSpan ExecutionTime { get; set; } = new TimeSpan(9, 0, 0);

        [SettingProperty("�z�b�g�L�[", SettingControlType.KeyPicker,
            category: "����",
            description: "�ݒ肷��z�b�g�L�[")]
        public System.Windows.Input.Key HotKey { get; set; } = System.Windows.Input.Key.F1;

        [SettingProperty("�w�i�F", SettingControlType.ColorPicker,
            category: "�\��",
            description: "�w�i�Ɏg�p����F")]
        public System.Drawing.Color BackgroundColor { get; set; } = System.Drawing.Color.White;

        public DemoCommand(ICommand? parent = null, UniversalCommandItem? item = null, IServiceProvider? serviceProvider = null)
            : base(parent, null, serviceProvider)
        {
            _logger = (serviceProvider?.GetService(typeof(ILoggerFactory)) as ILoggerFactory)?.CreateLogger<DemoCommand>();
            Description = "�f���R�}���h";

            // UniversalCommandItem����ݒ�l�𕜌�
            if (item != null)
            {
                Message = item.GetSetting("Message", "Demo Command");
                WaitTime = item.GetSetting("WaitTime", 500);
                ImagePath = item.GetSetting("ImagePath", string.Empty);
                SaveFolder = item.GetSetting("SaveFolder", string.Empty);
                WindowTitle = item.GetSetting("WindowTitle", string.Empty);
                X = item.GetSetting("X", 100);
                Y = item.GetSetting("Y", 100);
                MousePosition = item.GetSetting("MousePosition", new System.Drawing.Point(0, 0));
                MouseButton = item.GetSetting("MouseButton", System.Windows.Input.MouseButton.Left);
                Key = item.GetSetting("Key", System.Windows.Input.Key.Enter);
                IsActive = item.GetSetting("IsActive", true);
                Threshold = item.GetSetting("Threshold", 0.8);
                Password = item.GetSetting("Password", string.Empty);
                OnnxModelPath = item.GetSetting("OnnxModelPath", string.Empty);
                StartDate = item.GetSetting("StartDate", DateTime.Today);
                ExecutionTime = item.GetSetting("ExecutionTime", new TimeSpan(9, 0, 0));
                HotKey = item.GetSetting("HotKey", System.Windows.Input.Key.F1);
                BackgroundColor = item.GetSetting("BackgroundColor", System.Drawing.Color.White);
            }
        }

        protected override async Task<bool> DoExecuteAsync(CancellationToken cancellationToken)
        {
            try
            {
                _logger?.LogInformation("DemoCommand���s�J�n: {Message}", Message);

                LogMessage($"�f���J�n: {Message}");

                if (!IsActive)
                {
                    LogMessage("�R�}���h�������̂��߁A�X�L�b�v���܂�");
                    return true;
                }

                LogMessage($"�ݒ�l�`�F�b�N:");
                LogMessage($"  - �ҋ@����: {WaitTime}ms");
                LogMessage($"  - �摜�p�X: {(string.IsNullOrEmpty(ImagePath) ? "���ݒ�" : ImagePath)}");
                LogMessage($"  - �ۑ��t�H���_: {(string.IsNullOrEmpty(SaveFolder) ? "���ݒ�" : SaveFolder)}");
                LogMessage($"  - �E�B���h�E: {(string.IsNullOrEmpty(WindowTitle) ? "���ݒ�" : WindowTitle)}");
                LogMessage($"  - ���W: ({X}, {Y})");
                LogMessage($"  - �}�E�X�ʒu: ({MousePosition.X}, {MousePosition.Y})");
                LogMessage($"  - �}�E�X�{�^��: {MouseButton}");
                LogMessage($"  - �L�[: {Key}");
                LogMessage($"  - �������l: {Threshold:F2}");
                LogMessage($"  - �p�X���[�h: {(string.IsNullOrEmpty(Password) ? "���ݒ�" : "[�ݒ�ς�]")}");
                LogMessage($"  - ONNX���f��: {(string.IsNullOrEmpty(OnnxModelPath) ? "���ݒ�" : OnnxModelPath)}");
                LogMessage($"  - �J�n��: {StartDate:yyyy-MM-dd}");
                LogMessage($"  - ���s����: {ExecutionTime:hh\\:mm\\:ss}");
                LogMessage($"  - �z�b�g�L�[: {HotKey}");
                LogMessage($"  - �w�i�F: {BackgroundColor}");

                LogMessage($"�ҋ@��... ({WaitTime}ms)");
                await Task.Delay(WaitTime, cancellationToken);

                if (!string.IsNullOrEmpty(ImagePath))
                {
                    var resolvedPath = ResolvePath(ImagePath);
                    if (System.IO.File.Exists(resolvedPath))
                    {
                        var fileInfo = new System.IO.FileInfo(resolvedPath);
                        LogMessage($"�摜�t�@�C���m�F: {resolvedPath} ({fileInfo.Length} bytes)");
                    }
                    else
                    {
                        LogMessage($"�摜�t�@�C����������܂���: {resolvedPath}");
                    }
                }

                if (!string.IsNullOrEmpty(SaveFolder))
                {
                    var resolvedFolder = ResolvePath(SaveFolder);
                    if (System.IO.Directory.Exists(resolvedFolder))
                    {
                        LogMessage($"�ۑ��t�H���_�m�F: {resolvedFolder}");
                    }
                    else
                    {
                        LogMessage($"�ۑ��t�H���_��������܂���: {resolvedFolder}");
                    }
                }

                LogMessage("DemoCommand���s����");
                _logger?.LogInformation("DemoCommand���s����: {Message}", Message);
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "DemoCommand���s���ɃG���[: {Message}", ex.Message);
                LogMessage($"DemoCommand�G���[: {ex.Message}");
                return false;
            }
        }

        protected override void ValidateSettings()
        {
            if (WaitTime < 0)
            {
                throw new ArgumentException("�ҋ@���Ԃ�0�ȏ�ł���K�v������܂�");
            }

            if (!string.IsNullOrEmpty(ImagePath))
            {
                ValidateFileExists(ImagePath, "�摜�t�@�C��");
            }

            if (!string.IsNullOrEmpty(OnnxModelPath))
            {
                ValidateFileExists(OnnxModelPath, "ONNX���f���t�@�C��");
            }

            if (Threshold < 0.0 || Threshold > 1.0)
            {
                throw new ArgumentException("�������l��0.0�`1.0�͈̔͂Őݒ肵�Ă�������");
            }

            if (StartDate < DateTime.Today.AddYears(-10) || StartDate > DateTime.Today.AddYears(10))
            {
                throw new ArgumentException("�J�n���͑Ó��Ȕ͈͂Őݒ肵�Ă�������");
            }

            if (ExecutionTime < TimeSpan.Zero || ExecutionTime >= TimeSpan.FromDays(1))
            {
                throw new ArgumentException("���s������0:00:00?23:59:59�͈̔͂Őݒ肵�Ă�������");
            }
        }
    }
}