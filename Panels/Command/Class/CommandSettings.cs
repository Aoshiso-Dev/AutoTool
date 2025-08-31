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

    public class WaitImageCommandSettings : ICommandSettings, IWaitImageCommandSettings
    {
        public string ImagePath { get; set; } = string.Empty;
        public double Threshold { get; set; } = 0.8; // デフォルト値を設定
        public Color? SearchColor { get; set; }
        public int Timeout { get; set; } = 5000; // デフォルト値を設定
        public int Interval { get; set; } = 500; // デフォルト値を設定
        public string WindowTitle { get; set; } = string.Empty;
        public string WindowClassName { get; set; } = string.Empty;
    }

    public class ClickImageCommandSettings : ICommandSettings, IClickImageCommandSettings
    {
        public string ImagePath { get; set; } = string.Empty;
        public double Threshold { get; set; } = 0.8;
        public Color? SearchColor { get; set; }
        public int Timeout { get; set; } = 5000;
        public int Interval { get; set; } = 500;
        public System.Windows.Input.MouseButton Button { get; set; } = System.Windows.Input.MouseButton.Left;
        public string WindowTitle { get; set; } = string.Empty;
        public string WindowClassName { get; set; } = string.Empty;
    }

    public class HotkeyCommandSettings : ICommandSettings, IHotkeyCommandSettings
    {
        public bool Ctrl { get; set; }
        public bool Alt { get; set; }
        public bool Shift { get; set; }
        public System.Windows.Input.Key Key { get; set; } = System.Windows.Input.Key.Escape;
        public string WindowTitle { get; set; } = string.Empty;
        public string WindowClassName { get; set; } = string.Empty;
    }

    public class ClickCommandSettings : ICommandSettings, IClickCommandSettings
    {
        public System.Windows.Input.MouseButton Button { get; set; } = System.Windows.Input.MouseButton.Left;
        public int X { get; set; }
        public int Y { get; set; }
        public string WindowTitle { get; set; } = string.Empty;
        public string WindowClassName { get; set; } = string.Empty;
    }

    public class WaitCommandSettings : ICommandSettings, IWaitCommandSettings
    {
        public int Wait { get; set; } = 1000; // デフォルト1秒
    }

    public class LoopCommandSettings : ICommandSettings, ILoopCommandSettings
    {
        public int LoopCount { get; set; } = 1; // デフォルト1回
        public ICommand? Pair { get; set; }
        
        // バリデーション追加
        public void Validate()
        {
            if (LoopCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(LoopCount), "ループ回数は1以上である必要があります");
        }
    }

    public class LoopEndCommandSettings : ICommandSettings, ILoopEndCommandSettings
    {
        public int LoopCount { get; set; } = 1;
        public ICommand? Pair { get; set; }
    }

    public class AIImageDetectCommandSettings : ICommandSettings, IIfImageExistAISettings
    {
        public string ModelPath { get; set; } = string.Empty;
        public int ClassID { get; set; } = 0;
        public string WindowTitle { get; set; } = string.Empty;
        public string WindowClassName { get; set; } = string.Empty;
        public double ConfThreshold { get; set; } = 0.5; // デフォルト値を設定
        public double IoUThreshold { get; set; } = 0.25;
        public int Timeout { get; set; } = 5000;
        public int Interval { get; set; } = 500;
        
        // バリデーション追加
        public void Validate()
        {
            if (string.IsNullOrEmpty(ModelPath))
                throw new ArgumentException("モデルパスは必須です", nameof(ModelPath));
            if (ConfThreshold < 0 || ConfThreshold > 1)
                throw new ArgumentOutOfRangeException(nameof(ConfThreshold), "信頼度閾値は0-1の範囲である必要があります");
            if (IoUThreshold < 0 || IoUThreshold > 1)
                throw new ArgumentOutOfRangeException(nameof(IoUThreshold), "IoU閾値は0-1の範囲である必要があります");
        }
    }

    public class AIImageNotDetectCommandSettings : ICommandSettings, IIfImageNotExistAISettings
    {
        public string ModelPath { get; set; } = string.Empty;
        public int ClassID { get; set; } = 0;
        public string WindowTitle { get; set; } = string.Empty;
        public string WindowClassName { get; set; } = string.Empty;
        public double ConfThreshold { get; set; } = 0.5;
        public double IoUThreshold { get; set; } = 0.25;
        
        public void Validate()
        {
            if (string.IsNullOrEmpty(ModelPath))
                throw new ArgumentException("モデルパスは必須です", nameof(ModelPath));
            if (ConfThreshold < 0 || ConfThreshold > 1)
                throw new ArgumentOutOfRangeException(nameof(ConfThreshold), "信頼度閾値は0-1の範囲である必要があります");
            if (IoUThreshold < 0 || IoUThreshold > 1)
                throw new ArgumentOutOfRangeException(nameof(IoUThreshold), "IoU閾値は0-1の範囲である必要があります");
        }
    }

    public class ExecuteCommandSettings : ICommandSettings, IExecuteCommandSettings
    {
        public string ProgramPath { get; set; } = string.Empty;
        public string Arguments { get; set; } = string.Empty;
        public string WorkingDirectory { get; set; } = string.Empty;
        public bool WaitForExit { get; set; } = false;
        
        // バリデーション追加
        public void Validate()
        {
            if (string.IsNullOrEmpty(ProgramPath))
                throw new ArgumentException("プログラムパスは必須です", nameof(ProgramPath));
        }
    }

    public class SetVariableCommandSettings : ICommandSettings, ISetVariableCommandSettings
    {
        public string Name { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        
        // バリデーション追加
        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(Name))
                throw new ArgumentException("変数名は必須です", nameof(Name));
        }
    }

    public class SetVariableAISettings : ICommandSettings, ISetVariableAICommandSettings
    {
        public string Name { get; set; } = string.Empty;
        public string ModelPath { get; set; } = string.Empty;
        public string AIDetectMode { get; set; } = string.Empty;
        public string WindowTitle { get; set; } = string.Empty;
        public string WindowClassName { get; set; } = string.Empty;
        public double ConfThreshold { get; set; } = 0.5;
        public double IoUThreshold { get; set; } = 0.25;
        
        // バリデーション追加
        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(Name))
                throw new ArgumentException("変数名は必須です", nameof(Name));
            if (string.IsNullOrEmpty(ModelPath))
                throw new ArgumentException("モデルパスは必須です", nameof(ModelPath));
        }
    }

    public class IfVariableCommandSettings : ICommandSettings, IIfVariableCommandSettings
    {
        public string Name { get; set; } = string.Empty;
        public string Operator { get; set; } = "==";
        public string Value { get; set; } = string.Empty;
    }

    public class ClickImageAICommandSettings : ICommandSettings, IClickImageAICommandSettings
    {
        public string ModelPath { get; set; } = string.Empty;
        public int ClassID { get; set; } = 0;
        public string WindowTitle { get; set; } = string.Empty;
        public string WindowClassName { get; set; } = string.Empty;
        public double ConfThreshold { get; set; } = 0.5;
        public double IoUThreshold { get; set; } = 0.25;
        public System.Windows.Input.MouseButton Button { get; set; } = System.Windows.Input.MouseButton.Left;
        
        // バリデーション追加
        public void Validate()
        {
            if (string.IsNullOrEmpty(ModelPath))
                throw new ArgumentException("モデルパスは必須です", nameof(ModelPath));
            if (ConfThreshold < 0 || ConfThreshold > 1)
                throw new ArgumentOutOfRangeException(nameof(ConfThreshold), "信頼度閾値は0-1の範囲である必要があります");
            if (IoUThreshold < 0 || IoUThreshold > 1)
                throw new ArgumentOutOfRangeException(nameof(IoUThreshold), "IoU閾値は0-1の範囲である必要があります");
        }
    }
}
