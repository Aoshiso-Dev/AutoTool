namespace AutoTool.Model;

public interface IUndoRedoCommand
{
    string Description { get; }
    void Execute();
    void Undo();
}
