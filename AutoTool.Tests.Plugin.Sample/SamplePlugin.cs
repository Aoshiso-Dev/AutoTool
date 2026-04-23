using System.Text.Json;
using AutoTool.Plugin.Abstractions.Interfaces;
using AutoTool.Plugin.Abstractions.PluginModel;

namespace AutoTool.Tests.Plugin.Sample;

public sealed class SamplePlugin : IAutoToolPlugin, IPluginCommandDefinitionProvider, IPluginServiceRegistrar, IPluginCommandExecutor, IPluginHealthCheck
{
    public PluginDescriptor Descriptor { get; } = new()
    {
        PluginId = "Sample.Plugin",
        DisplayName = "Sample Plugin",
        Version = "1.0.0",
        EntryAssembly = "AutoTool.Tests.Plugin.Sample.dll",
        EntryType = "AutoTool.Tests.Plugin.Sample.SamplePlugin",
        Permissions = [],
    };

    public ValueTask<PluginInitializationResult> InitializeAsync(
        IPluginInitializationContext context,
        CancellationToken cancellationToken)
    {
        context.Log("initialized");
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
                CommandType = "Sample.Plugin.ProviderCommand",
                DisplayName = "Provider Command",
                Category = "System",
                Order = 1,
                Description = "指定した変数へ文字列を設定します。",
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

    public void RegisterServices(IPluginServiceRegistry registry)
    {
        registry.Register(typeof(ISamplePluginService), typeof(SamplePluginService), PluginServiceLifetime.Singleton);
    }

    public ValueTask<bool> ExecuteCommandAsync(
        PluginCommandExecutionRequest request,
        IPluginExecutionContext context,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(request.CommandType, "Sample.Plugin.ProviderCommand", StringComparison.Ordinal))
        {
            return ValueTask.FromResult(false);
        }

        using var document = JsonDocument.Parse(request.ParameterJson);
        if (document.RootElement.TryGetProperty("targetVariable", out var variableElement)
            && document.RootElement.TryGetProperty("value", out var valueElement))
        {
            var variableName = variableElement.GetString();
            var value = valueElement.GetString();
            if (!string.IsNullOrWhiteSpace(variableName))
            {
                context.SetVariable(variableName, value ?? string.Empty);
            }
        }

        context.Log("provider command executed");
        return ValueTask.FromResult(true);
    }

    public ValueTask<PluginHealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(new PluginHealthCheckResult
        {
            IsHealthy = true,
            Summary = "Sample plugin ready",
            Messages = ["health check passed"],
        });
    }
}
