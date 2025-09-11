using AutoTool.Core.Abstractions;

namespace AutoTool.Services.Implementations;

/// <summary>
/// シンプルな値リゾルバーの実装
/// </summary>
public class SimpleValueResolver : IValueResolver
{
    public async Task<string?> ResolveStringAsync(object? valueSource, CancellationToken ct)
    {
        await Task.Yield(); // 非同期メソッドの形式を保つ
        
        if (valueSource == null)
            return null;

        return valueSource.ToString();
    }

    public async Task<bool> EvaluateBoolAsync(string expr, CancellationToken ct)
    {
        await Task.Yield(); // 非同期メソッドの形式を保つ
        
        if (string.IsNullOrWhiteSpace(expr))
            return false;

        // 簡単な実装 - より高度な式評価が必要な場合は拡張
        if (bool.TryParse(expr, out var result))
            return result;

        // "true", "false" 以外の場合は文字列が空でないかをチェック
        return !string.IsNullOrWhiteSpace(expr);
    }
}

/// <summary>
/// シンプルな変数スコープの実装
/// </summary>
public class SimpleVariableScope : IVariableScope
{
    private readonly Dictionary<string, object?> _variables = new();

    public bool TryGet(string name, out object? value)
    {
        return _variables.TryGetValue(name, out value);
    }

    public void Set(string name, object? value)
    {
        _variables[name] = value;
    }
}