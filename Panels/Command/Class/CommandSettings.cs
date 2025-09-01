using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MacroPanels.Command.Interface;
using System.Windows.Media;

namespace MacroPanels.Command.Class
{
    public class CommandSettings : ICommandSettings { }

    /// <summary>
    /// ウィンドウ対象を持つコマンド設定の基底クラス
    /// </summary>
    public abstract class WindowTargetCommandSettings : CommandSettings
    {
        public string WindowTitle { get; set; } = string.Empty;
        public string WindowClassName { get; set; } = string.Empty;
    }

    /// <summary>
    /// 画像検索機能を持つコマンド設定の基底クラス
    /// </summary>
    public abstract class ImageSearchCommandSettings : WindowTargetCommandSettings, IValidatable
    {
        public string ImagePath { get; set; } = string.Empty;
        public double Threshold { get; set; } = 0.8;
        public Color? SearchColor { get; set; } = null;

        public virtual void Validate()
        {
            ValidationHelper.ValidateRequired(ImagePath, nameof(ImagePath), "画像パス");
            ValidationHelper.ValidateThreshold(Threshold, nameof(Threshold));
        }
    }

    /// <summary>
    /// AI検出機能を持つコマンド設定の基底クラス
    /// </summary>
    public abstract class AIDetectionCommandSettings : WindowTargetCommandSettings, IValidatable
    {
        public string ModelPath { get; set; } = string.Empty;
        public int ClassID { get; set; } = 0;
        public double ConfThreshold { get; set; } = 0.5;
        public double IoUThreshold { get; set; } = 0.25;

        public virtual void Validate()
        {
            ValidationHelper.ValidateRequired(ModelPath, nameof(ModelPath), "モデルパス");
            ValidationHelper.ValidateThreshold(ConfThreshold, "信頼度閾値");
            ValidationHelper.ValidateThreshold(IoUThreshold, "IoU閾値");
        }
    }

    public class WaitImageCommandSettings : ImageSearchCommandSettings, IWaitImageCommandSettings
    {
        public int Timeout { get; set; } = 5000; // デフォルト値を設定
        public int Interval { get; set; } = 500; // デフォルト値を設定
    }

    // IF系画像コマンド用の設定クラス（TimeoutとIntervalを削除）
    public class IfImageCommandSettings : ImageSearchCommandSettings, IIfImageCommandSettings
    {
        // Validateはベースクラスで実装済み
    }

    public class ClickImageCommandSettings : ImageSearchCommandSettings, IClickImageCommandSettings
    {
        public int Timeout { get; set; } = 5000;
        public int Interval { get; set; } = 500;
        public System.Windows.Input.MouseButton Button { get; set; } = System.Windows.Input.MouseButton.Left;
    }

    public class HotkeyCommandSettings : WindowTargetCommandSettings, IHotkeyCommandSettings
    {
        public bool Ctrl { get; set; }
        public bool Alt { get; set; }
        public bool Shift { get; set; }
        public System.Windows.Input.Key Key { get; set; } = System.Windows.Input.Key.Escape;
    }

    public class ClickCommandSettings : WindowTargetCommandSettings, IClickCommandSettings
    {
        public System.Windows.Input.MouseButton Button { get; set; } = System.Windows.Input.MouseButton.Left;
        public int X { get; set; }
        public int Y { get; set; }
    }

    public class WaitCommandSettings : ICommandSettings, IWaitCommandSettings
    {
        public int Wait { get; set; } = 1000; // デフォルト1秒
    }

    public class LoopCommandSettings : ICommandSettings, ILoopCommandSettings, IValidatable
    {
        public int LoopCount { get; set; } = 1; // デフォルト1回
        public ICommand? Pair { get; set; }
        
        public void Validate()
        {
            ValidationHelper.ValidatePositiveInteger(LoopCount, nameof(LoopCount), "ループ回数");
        }
    }

    public class LoopEndCommandSettings : ICommandSettings, ILoopEndCommandSettings
    {
        public int LoopCount { get; set; } = 1;
        public ICommand? Pair { get; set; }
    }

    public class AIImageDetectCommandSettings : AIDetectionCommandSettings, IIfImageExistAISettings
    {
        public int Timeout { get; set; } = 5000;
        public int Interval { get; set; } = 500;
        
        // Validateはベースクラスで実装済み
    }

    public class AIImageNotDetectCommandSettings : AIDetectionCommandSettings, IIfImageNotExistAISettings
    {
        // Validateはベースクラスで実装済み
    }

    public class ExecuteCommandSettings : ICommandSettings, IExecuteCommandSettings, IValidatable
    {
        public string ProgramPath { get; set; } = string.Empty;
        public string Arguments { get; set; } = string.Empty;
        public string WorkingDirectory { get; set; } = string.Empty;
        public bool WaitForExit { get; set; } = false;
        
        public void Validate()
        {
            ValidationHelper.ValidateRequired(ProgramPath, nameof(ProgramPath), "プログラムパス");
        }
    }

    public class SetVariableCommandSettings : ICommandSettings, ISetVariableCommandSettings, IValidatable
    {
        public string Name { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        
        public void Validate()
        {
            ValidationHelper.ValidateVariableName(Name, nameof(Name));
        }
    }

    public class SetVariableAISettings : AIDetectionCommandSettings, ISetVariableAICommandSettings
    {
        public string Name { get; set; } = string.Empty;
        public string AIDetectMode { get; set; } = string.Empty;
        
        public override void Validate()
        {
            ValidationHelper.ValidateVariableName(Name, nameof(Name));
            base.Validate(); // AI検出のバリデーションも実行
        }
    }

    public class IfVariableCommandSettings : ICommandSettings, IIfVariableCommandSettings
    {
        public string Name { get; set; } = string.Empty;
        public string Operator { get; set; } = "==";
        public string Value { get; set; } = string.Empty;
    }

    public class ScreenshotCommandSettings : WindowTargetCommandSettings, IScreenshotCommandSettings
    {
        public string SaveDirectory { get; set; } = string.Empty;
    }

    public class ClickImageAICommandSettings : AIDetectionCommandSettings, IClickImageAICommandSettings
    {
        public System.Windows.Input.MouseButton Button { get; set; } = System.Windows.Input.MouseButton.Left;
        
        // Validateはベースクラスで実装済み
    }
}
