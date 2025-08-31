using System.Windows.Input;
using System.Windows.Media;
using MacroPanels.List.Class;
using MacroPanels.Model.List.Interface;
using MacroPanels.ViewModel.Helpers;

namespace MacroPanels.ViewModel.Helpers
{
    /// <summary>
    /// EditPanelViewModel�p�̃v���p�e�B�Ǘ��N���X
    /// </summary>
    public class EditPanelPropertyManager
    {
        // �E�B���h�E�֘A
        public MultiInterfacePropertyAccessor<string> WindowTitle { get; }
        public MultiInterfacePropertyAccessor<string> WindowClassName { get; }

        // �摜�֘A
        public MultiInterfacePropertyAccessor<string> ImagePath { get; }
        public MultiInterfacePropertyAccessor<double> Threshold { get; }
        public MultiInterfacePropertyAccessor<Color?> SearchColor { get; }

        // �^�C�~���O�֘A
        public MultiInterfacePropertyAccessor<int> Timeout { get; }
        public MultiInterfacePropertyAccessor<int> Interval { get; }

        // �}�E�X�E�L�[�{�[�h�֘A
        public MultiInterfacePropertyAccessor<System.Windows.Input.MouseButton> MouseButton { get; }
        public PropertyAccessor<IHotkeyItem, bool> Ctrl { get; }
        public PropertyAccessor<IHotkeyItem, bool> Alt { get; }
        public PropertyAccessor<IHotkeyItem, bool> Shift { get; }
        public PropertyAccessor<IHotkeyItem, Key> Key { get; }

        // ���W�֘A
        public PropertyAccessor<IClickItem, int> X { get; }
        public PropertyAccessor<IClickItem, int> Y { get; }

        // �ҋ@�E���[�v�֘A
        public PropertyAccessor<IWaitItem, int> Wait { get; }
        public PropertyAccessor<ILoopItem, int> LoopCount { get; }

        // AI�֘A
        public MultiInterfacePropertyAccessor<string> ModelPath { get; }
        public MultiInterfacePropertyAccessor<int> ClassID { get; }
<<<<<<< HEAD
        public PropertyAccessor<SetVariableAIItem, string> Mode { get; }
=======
<<<<<<< HEAD
        public PropertyAccessor<SetVariableAIItem, string> Mode { get; }
=======
>>>>>>> cc003b3bf020157c70eac2bd186a987bda44d224
>>>>>>> 1b9342eba0081bf1f34c651d247e029b7e8c640a

        // �v���O�������s�֘A
        public PropertyAccessor<ExecuteItem, string> ProgramPath { get; }
        public PropertyAccessor<ExecuteItem, string> Arguments { get; }
        public PropertyAccessor<ExecuteItem, string> WorkingDirectory { get; }
        public PropertyAccessor<ExecuteItem, bool> WaitForExit { get; }

        // �ϐ��֘A
        public MultiInterfacePropertyAccessor<string> VariableName { get; }
        public PropertyAccessor<SetVariableItem, string> VariableValue { get; }
        public PropertyAccessor<IfVariableItem, string> CompareOperator { get; }
        public PropertyAccessor<IfVariableItem, string> CompareValue { get; }

        // �X�N���[���V���b�g�֘A
        public PropertyAccessor<ScreenshotItem, string> SaveDirectory { get; }
<<<<<<< HEAD
        
        // AI thresholds
        public MultiInterfacePropertyAccessor<double> ConfThreshold { get; }
        public MultiInterfacePropertyAccessor<double> IoUThreshold { get; }
=======
<<<<<<< HEAD
        // New AI thresholds
        public MultiInterfacePropertyAccessor<double> ConfThreshold { get; }
        public MultiInterfacePropertyAccessor<double> IoUThreshold { get; }
=======
>>>>>>> cc003b3bf020157c70eac2bd186a987bda44d224
>>>>>>> 1b9342eba0081bf1f34c651d247e029b7e8c640a

        public EditPanelPropertyManager()
        {
            // �E�B���h�E�֘A�̏�����
            WindowTitle = new MultiInterfacePropertyAccessor<string>(string.Empty)
                .AddInterface<IWaitImageItem>(x => x.WindowTitle)
                .AddInterface<IClickImageItem>(x => x.WindowTitle)
                .AddInterface<IClickItem>(x => x.WindowTitle)
                .AddInterface<IHotkeyItem>(x => x.WindowTitle)
                .AddInterface<IIfImageExistItem>(x => x.WindowTitle)
                .AddInterface<IIfImageNotExistItem>(x => x.WindowTitle)
                .AddInterface<IIfImageExistAIItem>(x => x.WindowTitle)
                .AddInterface<IIfImageNotExistAIItem>(x => x.WindowTitle)
                .AddInterface<ISetVariableAIItem>(x => x.WindowTitle)
<<<<<<< HEAD
                .AddInterface<IClickImageAIItem>(x => x.WindowTitle)
=======
<<<<<<< HEAD
                .AddInterface<IClickImageAIItem>(x => x.WindowTitle)
=======
>>>>>>> cc003b3bf020157c70eac2bd186a987bda44d224
>>>>>>> 1b9342eba0081bf1f34c651d247e029b7e8c640a
                .AddInterface<IScreenshotItem>(x => x.WindowTitle);

            WindowClassName = new MultiInterfacePropertyAccessor<string>(string.Empty)
                .AddInterface<IWaitImageItem>(x => x.WindowClassName)
                .AddInterface<IClickImageItem>(x => x.WindowClassName)
                .AddInterface<IClickItem>(x => x.WindowClassName)
                .AddInterface<IHotkeyItem>(x => x.WindowClassName)
                .AddInterface<IIfImageExistItem>(x => x.WindowClassName)
                .AddInterface<IIfImageNotExistItem>(x => x.WindowClassName)
                .AddInterface<IIfImageExistAIItem>(x => x.WindowClassName)
                .AddInterface<IIfImageNotExistAIItem>(x => x.WindowClassName)
                .AddInterface<ISetVariableAIItem>(x => x.WindowClassName)
<<<<<<< HEAD
                .AddInterface<IClickImageAIItem>(x => x.WindowClassName)
=======
<<<<<<< HEAD
                .AddInterface<IClickImageAIItem>(x => x.WindowClassName)
=======
>>>>>>> cc003b3bf020157c70eac2bd186a987bda44d224
>>>>>>> 1b9342eba0081bf1f34c651d247e029b7e8c640a
                .AddInterface<IScreenshotItem>(x => x.WindowClassName);

            // �摜�֘A�̏�����
            ImagePath = new MultiInterfacePropertyAccessor<string>(string.Empty)
                .AddInterface<IWaitImageItem>(x => x.ImagePath)
                .AddInterface<IClickImageItem>(x => x.ImagePath)
                .AddInterface<IIfImageExistItem>(x => x.ImagePath)
                .AddInterface<IIfImageNotExistItem>(x => x.ImagePath);

            Threshold = new MultiInterfacePropertyAccessor<double>(0.8)
                .AddInterface<IWaitImageItem>(x => x.Threshold)
                .AddInterface<IClickImageItem>(x => x.Threshold)
                .AddInterface<IIfImageExistItem>(x => x.Threshold)
                .AddInterface<IIfImageNotExistItem>(x => x.Threshold);

            SearchColor = new MultiInterfacePropertyAccessor<Color?>(null)
                .AddInterface<IWaitImageItem>(x => x.SearchColor)
                .AddInterface<IClickImageItem>(x => x.SearchColor)
                .AddInterface<IIfImageExistItem>(x => x.SearchColor)
                .AddInterface<IIfImageNotExistItem>(x => x.SearchColor);

            // �^�C�~���O�֘A�̏������iWait_Image��Click_Image�̂݁j
            Timeout = new MultiInterfacePropertyAccessor<int>(5000)
                .AddInterface<IWaitImageItem>(x => x.Timeout)
<<<<<<< HEAD
=======
<<<<<<< HEAD
>>>>>>> 1b9342eba0081bf1f34c651d247e029b7e8c640a
                .AddInterface<IClickImageItem>(x => x.Timeout);

            Interval = new MultiInterfacePropertyAccessor<int>(500)
                .AddInterface<IWaitImageItem>(x => x.Interval)
                .AddInterface<IClickImageItem>(x => x.Interval);
<<<<<<< HEAD
=======
=======
                .AddInterface<IClickImageItem>(x => x.Timeout)
                .AddInterface<IIfImageExistItem>(x => x.Timeout)
                .AddInterface<IIfImageNotExistItem>(x => x.Timeout)
                .AddInterface<IIfImageExistAIItem>(x => x.Timeout)
                .AddInterface<IIfImageNotExistAIItem>(x => x.Timeout);

            Interval = new MultiInterfacePropertyAccessor<int>(500)
                .AddInterface<IWaitImageItem>(x => x.Interval)
                .AddInterface<IClickImageItem>(x => x.Interval)
                .AddInterface<IIfImageExistItem>(x => x.Interval)
                .AddInterface<IIfImageNotExistItem>(x => x.Interval)
                .AddInterface<IIfImageExistAIItem>(x => x.Interval)
                .AddInterface<IIfImageNotExistAIItem>(x => x.Interval);
>>>>>>> cc003b3bf020157c70eac2bd186a987bda44d224
>>>>>>> 1b9342eba0081bf1f34c651d247e029b7e8c640a

            // �}�E�X�E�L�[�{�[�h�֘A�̏�����
            MouseButton = new MultiInterfacePropertyAccessor<System.Windows.Input.MouseButton>(System.Windows.Input.MouseButton.Left)
                .AddInterface<IClickImageItem>(x => x.Button)
<<<<<<< HEAD
                .AddInterface<IClickItem>(x => x.Button)
                .AddInterface<IClickImageAIItem>(x => x.Button);
=======
<<<<<<< HEAD
                .AddInterface<IClickItem>(x => x.Button)
                .AddInterface<IClickImageAIItem>(x => x.Button);
=======
                .AddInterface<IClickItem>(x => x.Button);
>>>>>>> cc003b3bf020157c70eac2bd186a987bda44d224
>>>>>>> 1b9342eba0081bf1f34c651d247e029b7e8c640a

            Ctrl = new PropertyAccessor<IHotkeyItem, bool>(x => x.Ctrl, false);
            Alt = new PropertyAccessor<IHotkeyItem, bool>(x => x.Alt, false);
            Shift = new PropertyAccessor<IHotkeyItem, bool>(x => x.Shift, false);
            Key = new PropertyAccessor<IHotkeyItem, Key>(x => x.Key, System.Windows.Input.Key.Escape);

            // ���W�֘A�̏�����
            X = new PropertyAccessor<IClickItem, int>(x => x.X, 0);
            Y = new PropertyAccessor<IClickItem, int>(x => x.Y, 0);

            // �ҋ@�E���[�v�֘A�̏�����
            Wait = new PropertyAccessor<IWaitItem, int>(x => x.Wait, 0);
            LoopCount = new PropertyAccessor<ILoopItem, int>(x => x.LoopCount, 1);

            // AI�֘A�̏�����
            ModelPath = new MultiInterfacePropertyAccessor<string>(string.Empty)
                .AddInterface<IfImageExistAIItem>(x => x.ModelPath)
                .AddInterface<IfImageNotExistAIItem>(x => x.ModelPath)
<<<<<<< HEAD
=======
<<<<<<< HEAD
>>>>>>> 1b9342eba0081bf1f34c651d247e029b7e8c640a
                .AddInterface<SetVariableAIItem>(x => x.ModelPath)
                .AddInterface<ClickImageAIItem>(x => x.ModelPath);

            ClassID = new MultiInterfacePropertyAccessor<int>(0)
                .AddInterface<IfImageExistAIItem>(x => x.ClassID)
                .AddInterface<IfImageNotExistAIItem>(x => x.ClassID)
                .AddInterface<ClickImageAIItem>(x => x.ClassID);

            Mode = new PropertyAccessor<SetVariableAIItem, string>(x => x.AIDetectMode, "Class");
<<<<<<< HEAD
=======
=======
                .AddInterface<SetVariableAIItem>(x => x.ModelPath);

            ClassID = new MultiInterfacePropertyAccessor<int>(0)
                .AddInterface<IfImageExistAIItem>(x => x.ClassID)
                .AddInterface<IfImageNotExistAIItem>(x => x.ClassID);
>>>>>>> cc003b3bf020157c70eac2bd186a987bda44d224
>>>>>>> 1b9342eba0081bf1f34c651d247e029b7e8c640a

            // �v���O�������s�֘A�̏�����
            ProgramPath = new PropertyAccessor<ExecuteItem, string>(x => x.ProgramPath, string.Empty);
            Arguments = new PropertyAccessor<ExecuteItem, string>(x => x.Arguments, string.Empty);
            WorkingDirectory = new PropertyAccessor<ExecuteItem, string>(x => x.WorkingDirectory, string.Empty);
            WaitForExit = new PropertyAccessor<ExecuteItem, bool>(x => x.WaitForExit, false);

            // �ϐ��֘A�̏�����
            VariableName = new MultiInterfacePropertyAccessor<string>(string.Empty)
                .AddInterface<SetVariableItem>(x => x.Name)
                .AddInterface<IfVariableItem>(x => x.Name)
                .AddInterface<SetVariableAIItem>(x => x.Name);

            VariableValue = new PropertyAccessor<SetVariableItem, string>(x => x.Value, string.Empty);
            CompareOperator = new PropertyAccessor<IfVariableItem, string>(x => x.Operator, "==");
            CompareValue = new PropertyAccessor<IfVariableItem, string>(x => x.Value, string.Empty);

            // �X�N���[���V���b�g�֘A�̏�����
            SaveDirectory = new PropertyAccessor<ScreenshotItem, string>(x => x.SaveDirectory, string.Empty);
<<<<<<< HEAD
=======
<<<<<<< HEAD
>>>>>>> 1b9342eba0081bf1f34c651d247e029b7e8c640a

            ConfThreshold = new MultiInterfacePropertyAccessor<double>(0.5)
                .AddInterface<IfImageExistAIItem>(x => x.ConfThreshold)
                .AddInterface<IfImageNotExistAIItem>(x => x.ConfThreshold)
                .AddInterface<SetVariableAIItem>(x => x.ConfThreshold)
                .AddInterface<ClickImageAIItem>(x => x.ConfThreshold);

            IoUThreshold = new MultiInterfacePropertyAccessor<double>(0.25)
                .AddInterface<IfImageExistAIItem>(x => x.IoUThreshold)
                .AddInterface<IfImageNotExistAIItem>(x => x.IoUThreshold)
                .AddInterface<SetVariableAIItem>(x => x.IoUThreshold)
                .AddInterface<ClickImageAIItem>(x => x.IoUThreshold);
<<<<<<< HEAD
=======
=======
>>>>>>> cc003b3bf020157c70eac2bd186a987bda44d224
>>>>>>> 1b9342eba0081bf1f34c651d247e029b7e8c640a
        }
    }
}