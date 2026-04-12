using System.Collections.Concurrent;
using AutoTool.Commands.Services;

namespace AutoTool.Commands.Infrastructure;

/// <summary>
/// メモリ内変数ストアの実装
/// </summary>
public class InMemoryVariableStore : IVariableStore
{
    private readonly ConcurrentDictionary<string, string> _variables = new(StringComparer.OrdinalIgnoreCase);

    public void Set(string name, string value)
    {
        if (string.IsNullOrWhiteSpace(name)) return;
        _variables[name] = value ?? string.Empty;
    }

    public string? Get(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;
        return _variables.TryGetValue(name, out var v) ? v : null;
    }

    public void Clear() => _variables.Clear();
}

