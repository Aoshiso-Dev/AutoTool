using System.Text.Json;
using AutoTool.Plugin.Abstractions.Interfaces;
using AutoTool.Plugin.Abstractions.PluginModel;

namespace AutoTool.Plugin.Template;

public sealed class TemplatePlugin : IAutoToolPlugin, IPluginCommandDefinitionProvider, IPluginCommandExecutor, IPluginHealthCheck
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public PluginDescriptor Descriptor { get; } = new()
    {
        PluginId = TemplatePluginConstants.PluginId,
        DisplayName = TemplatePluginConstants.DisplayName,
        Version = TemplatePluginConstants.Version,
        EntryAssembly = TemplatePluginConstants.EntryAssembly,
        EntryType = TemplatePluginConstants.EntryType,
        Permissions = [],
    };

    public ValueTask<PluginInitializationResult> InitializeAsync(
        IPluginInitializationContext context,
        CancellationToken cancellationToken)
    {
        context.Log("template plugin initialized");
        return ValueTask.FromResult(PluginInitializationResult.Success());
    }

    public ValueTask DisposeAsync(CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }

    public IReadOnlyList<PluginCommandDefinition> GetCommandDefinitions()
    {
        return
        [
            new PluginCommandDefinition
            {
                CommandType = TemplatePluginConstants.CommandType,
                DisplayName = TemplatePluginConstants.CommandDisplayName,
                Category = "System",
                Order = 1,
                Description = "指定した変数へ値を設定します。実装差し替えの起点に使うテンプレートです。",
                Properties =
                [
                    new PluginCommandPropertyDefinition
                    {
                        Name = "targetVariable",
                        DisplayName = "対象変数",
                        EditorType = "TextBox",
                        Group = "設定",
                        Order = 1,
                        IsRequired = true,
                    },
                    new PluginCommandPropertyDefinition
                    {
                        Name = "value",
                        DisplayName = "設定値",
                        EditorType = "TextBox",
                        Group = "設定",
                        Order = 2,
                    }
                ]
            }
        ];
    }

    public ValueTask<bool> ExecuteCommandAsync(
        PluginCommandExecutionRequest request,
        IPluginExecutionContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);

        if (!string.Equals(request.CommandType, TemplatePluginConstants.CommandType, StringComparison.Ordinal))
        {
            return ValueTask.FromResult(false);
        }

        var parameters = JsonSerializer.Deserialize<WriteVariableParameters>(
            string.IsNullOrWhiteSpace(request.ParameterJson) ? "{}" : request.ParameterJson,
            JsonSerializerOptions) ?? new WriteVariableParameters();

        if (!string.IsNullOrWhiteSpace(parameters.TargetVariable))
        {
            context.SetVariable(parameters.TargetVariable, parameters.Value ?? string.Empty);
        }

        context.Log($"template command executed: target={parameters.TargetVariable}");
        return ValueTask.FromResult(true);
    }

    public ValueTask<PluginHealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(new PluginHealthCheckResult
        {
            IsHealthy = true,
            Summary = "Template plugin ready",
            Messages = ["health check passed"],
        });
    }
}
