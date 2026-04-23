using System.Reflection;
using System.Runtime.Loader;
using AutoTool.Plugin.Abstractions.Interfaces;
using AutoTool.Plugin.Abstractions.PluginModel;

namespace AutoTool.Plugin.Host.Models;

public sealed record LoadedPlugin
{
    public required PluginManifest Manifest { get; init; }

    public required string AssemblyPath { get; init; }

    public required Assembly Assembly { get; init; }

    public required AssemblyLoadContext LoadContext { get; init; }

    public required IAutoToolPlugin Instance { get; init; }

    public IPluginCommandDefinitionProvider? CommandDefinitionProvider { get; init; }

    public IPluginCommandExecutor? CommandExecutor { get; init; }

    public IPluginHealthCheck? HealthCheck { get; init; }

    public IPluginServiceRegistrar? ServiceRegistrar { get; init; }

    public IReadOnlyList<PluginServiceRegistration> ServiceRegistrations { get; init; } = [];
}



