using AutoTool.Commands.Model.Input;
using AutoTool.Commands.Interface;

namespace AutoTool.Commands.Commands;

public class CommandSettings : ICommandSettings { }

public class WaitImageCommandSettings : ICommandSettings, IWaitImageCommandSettings
{
    public string ImagePath { get; set; } = string.Empty;
    public double Threshold { get; set; } = 0.8;
    public CommandColor? SearchColor { get; set; }
    public int Timeout { get; set; } = 5000;
    public int Interval { get; set; } = 500;
    public string WindowTitle { get; set; } = string.Empty;
    public string WindowClassName { get; set; } = string.Empty;
}

public class FindImageCommandSettings : ICommandSettings, IFindImageCommandSettings
{
    public string ImagePath { get; set; } = string.Empty;
    public double Threshold { get; set; } = 0.8;
    public CommandColor? SearchColor { get; set; }
    public int Timeout { get; set; } = 5000;
    public int Interval { get; set; } = 500;
    public bool Strict { get; set; } = false;
    public string FoundVariableName { get; set; } = string.Empty;
    public string XVariableName { get; set; } = string.Empty;
    public string YVariableName { get; set; } = string.Empty;
    public string WindowTitle { get; set; } = string.Empty;
    public string WindowClassName { get; set; } = string.Empty;
}

public class FindTextCommandSettings : ICommandSettings, IFindTextCommandSettings
{
    public string TargetText { get; set; } = string.Empty;
    public bool CaseSensitive { get; set; } = false;
    public string MatchMode { get; set; } = "Contains";
    public int X { get; set; } = 0;
    public int Y { get; set; } = 0;
    public int Width { get; set; } = 300;
    public int Height { get; set; } = 100;
    public int Timeout { get; set; } = 3000;
    public int Interval { get; set; } = 500;
    public bool Strict { get; set; } = false;
    public double MinConfidence { get; set; } = 50.0;
    public string FoundVariableName { get; set; } = string.Empty;
    public string TextVariableName { get; set; } = string.Empty;
    public string ConfidenceVariableName { get; set; } = string.Empty;
    public string WindowTitle { get; set; } = string.Empty;
    public string WindowClassName { get; set; } = string.Empty;
    public string Language { get; set; } = "jpn";
    public string PageSegmentationMode { get; set; } = "6";
    public string Whitelist { get; set; } = string.Empty;
    public string PreprocessMode { get; set; } = "Gray";
    public string TessdataPath { get; set; } = string.Empty;
}

public class IfImageCommandSettings : ICommandSettings, IIfImageCommandSettings
{
    public string ImagePath { get; set; } = string.Empty;
    public double Threshold { get; set; } = 0.8;
    public CommandColor? SearchColor { get; set; } = null;
    public string WindowTitle { get; set; } = string.Empty;
    public string WindowClassName { get; set; } = string.Empty;

    public void Validate()
    {
        if (string.IsNullOrEmpty(ImagePath))
            throw new ArgumentException("画像パスは必須です", nameof(ImagePath));
        if (Threshold < 0 || Threshold > 1)
            throw new ArgumentOutOfRangeException(nameof(Threshold), "閾値は0-1の範囲である必要があります");
    }
}

public class ClickImageCommandSettings : ICommandSettings, IClickImageCommandSettings
{
    public string ImagePath { get; set; } = string.Empty;
    public double Threshold { get; set; } = 0.8;
    public CommandColor? SearchColor { get; set; }
    public int Timeout { get; set; } = 5000;
    public int Interval { get; set; } = 500;
    public CommandMouseButton Button { get; set; } = CommandMouseButton.Left;
    public int HoldDurationMs { get; set; } = 20;
    public string ClickInjectionMode { get; set; } = "MouseEvent";
    public bool SimulateMouseMove { get; set; } = false;
    public string WindowTitle { get; set; } = string.Empty;
    public string WindowClassName { get; set; } = string.Empty;
}

public class HotkeyCommandSettings : ICommandSettings, IHotkeyCommandSettings
{
    public bool Ctrl { get; set; }
    public bool Alt { get; set; }
    public bool Shift { get; set; }
    public CommandKey Key { get; set; } = CommandKey.Escape;
    public string WindowTitle { get; set; } = string.Empty;
    public string WindowClassName { get; set; } = string.Empty;
}

public class ClickCommandSettings : ICommandSettings, IClickCommandSettings
{
    public CommandMouseButton Button { get; set; } = CommandMouseButton.Left;
    public int HoldDurationMs { get; set; } = 20;
    public string ClickInjectionMode { get; set; } = "MouseEvent";
    public bool SimulateMouseMove { get; set; } = false;
    public int X { get; set; }
    public int Y { get; set; }
    public string WindowTitle { get; set; } = string.Empty;
    public string WindowClassName { get; set; } = string.Empty;
}

public class WaitCommandSettings : ICommandSettings, IWaitCommandSettings
{
    public int Wait { get; set; } = 1000;
}

public class LoopCommandSettings : ICommandSettings, ILoopCommandSettings
{
    public int LoopCount { get; set; } = 1;
    public ICommand? Pair { get; set; }

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
    public double ConfThreshold { get; set; } = 0.5;
    public double IoUThreshold { get; set; } = 0.25;
    public int Timeout { get; set; } = 5000;
    public int Interval { get; set; } = 500;

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

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Name))
            throw new ArgumentException("変数名は必須です", nameof(Name));
        if (string.IsNullOrEmpty(ModelPath))
            throw new ArgumentException("モデルパスは必須です", nameof(ModelPath));
    }
}

public class IfTextCommandSettings : ICommandSettings, IIfTextCommandSettings
{
    public string TargetText { get; set; } = string.Empty;
    public bool CaseSensitive { get; set; } = false;
    public string MatchMode { get; set; } = "Contains";
    public int X { get; set; } = 0;
    public int Y { get; set; } = 0;
    public int Width { get; set; } = 300;
    public int Height { get; set; } = 100;
    public double MinConfidence { get; set; } = 50.0;
    public string WindowTitle { get; set; } = string.Empty;
    public string WindowClassName { get; set; } = string.Empty;
    public string Language { get; set; } = "jpn";
    public string PageSegmentationMode { get; set; } = "6";
    public string Whitelist { get; set; } = string.Empty;
    public string PreprocessMode { get; set; } = "Gray";
    public string TessdataPath { get; set; } = string.Empty;
}

public class SetVariableOCRCommandSettings : ICommandSettings, ISetVariableOCRCommandSettings
{
    public string Name { get; set; } = string.Empty;
    public int X { get; set; } = 0;
    public int Y { get; set; } = 0;
    public int Width { get; set; } = 300;
    public int Height { get; set; } = 100;
    public string WindowTitle { get; set; } = string.Empty;
    public string WindowClassName { get; set; } = string.Empty;
    public string Language { get; set; } = "jpn";
    public string PageSegmentationMode { get; set; } = "6";
    public string Whitelist { get; set; } = string.Empty;
    public double MinConfidence { get; set; } = 50.0;
    public string PreprocessMode { get; set; } = "Gray";
    public string TessdataPath { get; set; } = string.Empty;

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Name))
            throw new ArgumentException("変数名は必須です", nameof(Name));
        if (Width <= 0)
            throw new ArgumentOutOfRangeException(nameof(Width), "幅は1以上である必要があります");
        if (Height <= 0)
            throw new ArgumentOutOfRangeException(nameof(Height), "高さは1以上である必要があります");
        if (MinConfidence < 0 || MinConfidence > 100)
            throw new ArgumentOutOfRangeException(nameof(MinConfidence), "最小信頼度は0-100の範囲である必要があります");
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
    public CommandMouseButton Button { get; set; } = CommandMouseButton.Left;
    public int HoldDurationMs { get; set; } = 20;
    public string ClickInjectionMode { get; set; } = "MouseEvent";
    public bool SimulateMouseMove { get; set; } = false;

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

public class ScreenshotCommandSettings : ICommandSettings, IScreenshotCommandSettings
{
    public string SaveDirectory { get; set; } = string.Empty;
    public string WindowTitle { get; set; } = string.Empty;
    public string WindowClassName { get; set; } = string.Empty;
}
