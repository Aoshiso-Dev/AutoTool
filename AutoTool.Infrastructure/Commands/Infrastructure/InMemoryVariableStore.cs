using System.Collections.Concurrent;
using AutoTool.Commands.Services;

namespace AutoTool.Commands.Infrastructure;

/// <summary>
/// メモリ内変数ストアの実装
/// </summary>
public class InMemoryVariableStore : IObservableVariableStore
{
    private readonly ConcurrentDictionary<string, string> _variables = new(StringComparer.OrdinalIgnoreCase);

    public event EventHandler<VariableStoreChangedEventArgs>? Changed;

    public void Set(string name, string value)
    {
        if (string.IsNullOrWhiteSpace(name)) return;
        var normalizedValue = value ?? string.Empty;
        _variables[name] = normalizedValue;
        Changed?.Invoke(this, new VariableStoreChangedEventArgs(name, normalizedValue, isClear: false));
    }

    public string? Get(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;
        return _variables.TryGetValue(name, out var v) ? v : null;
    }

    public void Clear()
    {
        _variables.Clear();
        Changed?.Invoke(this, new VariableStoreChangedEventArgs(name: null, value: null, isClear: true));
    }

    public IReadOnlyDictionary<string, string> GetSnapshot()
    {
        return _variables
            .OrderBy(static x => x.Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(static x => x.Key, static x => x.Value, StringComparer.OrdinalIgnoreCase);
    }
}

