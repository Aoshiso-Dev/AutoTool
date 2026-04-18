namespace AutoTool.Application.Ports;

public interface IPanelDialogService
{
    string? SelectImageFile();
    string? SelectModelFile();
    string? SelectExecutableFile();
    string? SelectFolder();
}
