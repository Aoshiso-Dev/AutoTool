using AutoTool.Command.Class;
using AutoTool.Command.Interface;
using AutoTool.Model.CommandDefinition;
using Microsoft.Extensions.Logging;
using System.Drawing;

namespace AutoTool.Command.Class
{
    [DirectCommand("Test", "�e�X�g�R�}���h", description: "�e�X�g�p�̃R�}���h�ł�")]
    public class TestCommand : BaseCommand
    {
        private readonly ILogger<TestCommand>? _logger;
        
        [SettingProperty("�e�X�g���b�Z�[�W", SettingControlType.TextBox, 
            category: "��{�ݒ�", 
            description: "���s���ɕ\�����郁�b�Z�[�W", 
            defaultValue: "Hello World!")]
        public string TestMessage { get; set; } = "Hello World!";
        
        [SettingProperty("�x������", SettingControlType.NumberBox, 
            category: "��{�ݒ�", 
            description: "���s���̒x�����ԁi�~���b�j", 
            defaultValue: 1000)]
        public int DelayMs { get; set; } = 1000;

        [SettingProperty("�e�X�g�L��", SettingControlType.CheckBox,
            category: "��{�ݒ�",
            description: "�e�X�g�@�\��L���ɂ��邩�ǂ���",
            defaultValue: true)]
        public bool IsTestEnabled { get; set; } = true;

        [SettingProperty("�p�X���[�h", SettingControlType.PasswordBox,
            category: "�Z�L�����e�B�ݒ�",
            description: "�e�X�g�p�p�X���[�h")]
        public string Password { get; set; } = string.Empty;
        
        [SettingProperty("�e�X�g�t�@�C��", SettingControlType.FilePicker, 
            description: "�e�X�g�p�t�@�C���̃p�X",
            category: "�t�@�C���ݒ�", 
            fileFilter: "�e�L�X�g�t�@�C�� (*.txt)|*.txt|���ׂẴt�@�C�� (*.*)|*.*")]
        public string TestFilePath { get; set; } = string.Empty;

        [SettingProperty("�t�H���_�p�X", SettingControlType.FolderPicker,
            category: "�t�@�C���ݒ�",
            description: "�e�X�g�p�t�H���_�̃p�X")]
        public string FolderPath { get; set; } = string.Empty;

        [SettingProperty("ONNX���f��", SettingControlType.OnnxPicker,
            category: "AI�ݒ�",
            description: "�e�X�g�pONNX���f���t�@�C��")]
        public string OnnxModelPath { get; set; } = string.Empty;

        [SettingProperty("�D��x", SettingControlType.Slider,
            category: "��{�ݒ�",
            description: "�����̗D��x",
            minValue: 0.0,
            maxValue: 10.0,
            defaultValue: 5.0)]
        public double Priority { get; set; } = 5.0;

        [SettingProperty("���s���[�h", SettingControlType.ComboBox,
            category: "��{�ݒ�",
            sourceCollection: "ExecutionModes",
            description: "���s���[�h��I��")]
        public string ExecutionMode { get; set; } = "Normal";

        [SettingProperty("�}�E�X�ʒu�ݒ�", SettingControlType.CoordinatePicker,
            description: "�}�E�X�ʒu��ݒ�",
            category: "���W�ݒ�")]
        public System.Drawing.Point MousePosition { get; set; } = new(0, 0);

        [SettingProperty("�F�ݒ�", SettingControlType.ColorPicker,
            category: "�\���ݒ�",
            description: "�e�[�}�J���[��ݒ�")]
        public System.Drawing.Color ThemeColor { get; set; } = System.Drawing.Color.Blue;

        [SettingProperty("�J�n��", SettingControlType.DatePicker,
            category: "���Ԑݒ�",
            description: "�����J�n����ݒ�")]
        public DateTime StartDate { get; set; } = DateTime.Today;

        [SettingProperty("���s����", SettingControlType.TimePicker,
            category: "���Ԑݒ�",
            description: "���s������ݒ�")]
        public TimeSpan ExecutionTime { get; set; } = TimeSpan.FromHours(9);

        [SettingProperty("�z�b�g�L�[", SettingControlType.KeyPicker,
            category: "����ݒ�",
            description: "�V���[�g�J�b�g�L�[��ݒ�")]
        public System.Windows.Input.Key HotKey { get; set; } = System.Windows.Input.Key.F1;
        
        [SettingProperty("�ΏۃE�B���h�E", SettingControlType.WindowPicker, 
            category: "�E�B���h�E�ݒ�",
            description: "�ΏۂƂ���E�B���h�E��I��")]
        public string WindowTitle { get; set; } = string.Empty;

        [SettingProperty("�}�E�X�{�^��", SettingControlType.ComboBox,
            category: "����ݒ�",
            sourceCollection: "MouseButtons",
            defaultValue: System.Windows.Input.MouseButton.Left)]
        public System.Windows.Input.MouseButton MouseButton { get; set; } = System.Windows.Input.MouseButton.Left;

        // ���W�̌ʃA�N�Z�X�p�v���p�e�B�iUI�\���p�ł͂Ȃ��A�����v�Z�p�j
        public int ClickX => MousePosition.X;
        public int ClickY => MousePosition.Y;

        public TestCommand(ICommand? parent = null, UniversalCommandItem? item = null, IServiceProvider? serviceProvider = null) 
            : base(parent, null, serviceProvider)
        {
            _logger = (serviceProvider?.GetService(typeof(ILoggerFactory)) as ILoggerFactory)?.CreateLogger<TestCommand>();
            Description = "�e�X�g�R�}���h";

            // UniversalCommandItem����ݒ�l�𕜌��iDirectCommandRegistry�ɂ�莩���K�p�����j
            if (item != null)
            {
                TestMessage = item.GetSetting("TestMessage", "Hello World!");
                DelayMs = item.GetSetting("DelayMs", 1000);
                IsTestEnabled = item.GetSetting("IsTestEnabled", true);
                Password = item.GetSetting("Password", string.Empty);
                TestFilePath = item.GetSetting("TestFilePath", string.Empty);
                FolderPath = item.GetSetting("FolderPath", string.Empty);
                OnnxModelPath = item.GetSetting("OnnxModelPath", string.Empty);
                Priority = item.GetSetting("Priority", 5.0);
                ExecutionMode = item.GetSetting("ExecutionMode", "Normal");
                MousePosition = item.GetSetting("MousePosition", new System.Drawing.Point(0, 0));
                ThemeColor = item.GetSetting("ThemeColor", System.Drawing.Color.Blue);
                StartDate = item.GetSetting("StartDate", DateTime.Today);
                ExecutionTime = item.GetSetting("ExecutionTime", TimeSpan.FromHours(9));
                HotKey = item.GetSetting("HotKey", System.Windows.Input.Key.F1);
                WindowTitle = item.GetSetting("WindowTitle", string.Empty);
                MouseButton = item.GetSetting("MouseButton", System.Windows.Input.MouseButton.Left);
            }
        }

        protected override async Task<bool> DoExecuteAsync(CancellationToken cancellationToken)
        {
            try
            {
                _logger?.LogInformation("TestCommand���s�J�n: {Message}, �D��x: {Priority}, ���[�h: {Mode}", 
                    TestMessage, Priority, ExecutionMode);
                
                LogMessage($"�e�X�g�J�n: {TestMessage}");
                
                if (!IsTestEnabled)
                {
                    LogMessage("�e�X�g�@�\�������̂��߁A�X�L�b�v���܂�");
                    return true;
                }

                LogMessage($"�x������: {DelayMs}ms �őҋ@��...");
                await Task.Delay(DelayMs, cancellationToken);
                
                // �t�@�C���E�t�H���_�֘A�̃e�X�g
                if (!string.IsNullOrEmpty(TestFilePath))
                {
                    var resolvedPath = ResolvePath(TestFilePath);
                    LogMessage($"�e�X�g�t�@�C��: {resolvedPath}");
                }

                if (!string.IsNullOrEmpty(FolderPath))
                {
                    var resolvedFolder = ResolvePath(FolderPath);
                    LogMessage($"�t�H���_�p�X: {resolvedFolder}");
                }

                if (!string.IsNullOrEmpty(OnnxModelPath))
                {
                    var resolvedModel = ResolvePath(OnnxModelPath);
                    LogMessage($"ONNX���f��: {resolvedModel}");
                }

                // ���W�E����֘A�̃e�X�g
                if (MousePosition.X != 0 || MousePosition.Y != 0)
                {
                    LogMessage($"�}�E�X�ʒu: ({MousePosition.X}, {MousePosition.Y}) �ł̃N���b�N���V�~�����[�g�i{MouseButton}�{�^���j");
                }

                // �F�E���Ԋ֘A�̃e�X�g
                LogMessage($"�e�[�}�J���[: R={ThemeColor.R}, G={ThemeColor.G}, B={ThemeColor.B}");
                LogMessage($"�J�n��: {StartDate:yyyy-MM-dd}");
                LogMessage($"���s����: {ExecutionTime:hh\\:mm\\:ss}");

                // �L�[�E�E�B���h�E�֘A�̃e�X�g
                LogMessage($"�z�b�g�L�[: {HotKey}");
                if (!string.IsNullOrEmpty(WindowTitle))
                {
                    LogMessage($"�ΏۃE�B���h�E: {WindowTitle}");
                }

                // �Z�L�����e�B�֘A�̃e�X�g
                if (!string.IsNullOrEmpty(Password))
                {
                    LogMessage("�p�X���[�h���ݒ肳��Ă��܂� [****]");
                }

                LogMessage($"���s���[�h: {ExecutionMode}, �D��x: {Priority}");
                LogMessage("TestCommand���s����");
                _logger?.LogInformation("TestCommand���s����: {Message}", TestMessage);
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "TestCommand���s���ɃG���[: {Message}", ex.Message);
                LogMessage($"TestCommand�G���[: {ex.Message}");
                return false;
            }
        }

        protected override void ValidateSettings()
        {
            // �ݒ�l�̌���
            if (DelayMs < 0)
            {
                throw new ArgumentException("�x�����Ԃ�0�ȏ�ł���K�v������܂�");
            }

            if (!string.IsNullOrEmpty(TestFilePath))
            {
                ValidateFileExists(TestFilePath, "�e�X�g�t�@�C��");
            }

            if (Priority < 0 || Priority > 10)
            {
                throw new ArgumentException("�D��x��0-10�͈̔͂Őݒ肵�Ă�������");
            }

            if (StartDate < DateTime.Today.AddYears(-10) || StartDate > DateTime.Today.AddYears(10))
            {
                throw new ArgumentException("�J�n���͑Ó��Ȕ͈͂Őݒ肵�Ă�������");
            }

            if (ExecutionTime < TimeSpan.Zero || ExecutionTime >= TimeSpan.FromDays(1))
            {
                throw new ArgumentException("���s������0:00:00�`23:59:59�͈̔͂Őݒ肵�Ă�������");
            }
        }
    }
}