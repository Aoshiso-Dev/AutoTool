namespace AutoTool.Core.Descriptors;


public interface ICommandRegistry
{
    IEnumerable<ICommandDescriptor> All { get; }
    ICommandDescriptor Get(string type);
    bool TryGet(string type, out ICommandDescriptor? descriptor);
}