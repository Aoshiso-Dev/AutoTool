namespace AutoTool.Application.History;

public interface IUndoRedoCommand
{
    string Description { get; }
    void Execute();
    void Undo();
}
