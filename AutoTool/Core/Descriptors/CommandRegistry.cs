using Microsoft.Extensions.DependencyInjection;


namespace AutoTool.Core.Descriptors;


public sealed class CommandRegistry : ICommandRegistry
{
    private readonly Dictionary<string, ICommandDescriptor> _map;


    public CommandRegistry(IEnumerable<ICommandDescriptor> descriptors)
    {
        _map = descriptors.ToDictionary(d => d.Type, StringComparer.Ordinal);
    }


    public IEnumerable<ICommandDescriptor> All => _map.Values;


    public ICommandDescriptor Get(string type)
    => _map.TryGetValue(type, out var d) ? d : throw new KeyNotFoundException($"Descriptor not found: {type}");


    public bool TryGet(string type, out ICommandDescriptor? descriptor)
    => _map.TryGetValue(type, out descriptor);
}