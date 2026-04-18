namespace AutoTool.Commands.Interface;

public interface IConditionSettings
{
}

public interface IImageConditionSettings : IConditionSettings
{
    string ImagePath { get; set; }
    double Threshold { get; set; }
    int Timeout { get; set; }
    int Interval { get; set; }
    string WindowTitle { get; set; }
    string WindowClassName { get; set; }
}
