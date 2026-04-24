using System.Reflection;
using System.Runtime.Loader;
using AutoTool.Plugin.Abstractions.Interfaces;
using AutoTool.Plugin.Abstractions.PluginModel;
using AutoTool.Plugin.Host.Abstractions;
using AutoTool.Plugin.Host.Models;
using AutoTool.Plugin.Host.Services;

namespace AutoTool.Plugin.Host.Tests;

public sealed class PluginQuickActionCatalogTests
{
    [Fact]
    public void GetQuickActions_MatchesCommandTypesAndSortsByOrder()
    {
        ILoadedPluginCatalog loadedPluginCatalog = new FakeLoadedPluginCatalog(
        [
            CreateLoadedPlugin(
                pluginId: "Quick.Plugin",
                quickActions:
                [
                    new PluginQuickActionDefinition
                    {
                        ActionId = "second",
                        DisplayName = "Second",
                        CommandType = "Quick.Plugin.Open",
                        Order = 20,
                    },
                    new PluginQuickActionDefinition
                    {
                        ActionId = "missing",
                        DisplayName = "Missing",
                        CommandType = "Quick.Plugin.Missing",
                        Order = 30,
                    },
                    new PluginQuickActionDefinition
                    {
                        ActionId = "first",
                        DisplayName = "First",
                        CommandType = "Quick.Plugin.Open",
                        Order = 10,
                        ParameterJson = """{"target":"stage"}""",
                    },
                ],
                commands:
                [
                    new PluginCommandDefinition
                    {
                        CommandType = "Quick.Plugin.Open",
                        DisplayName = "Open",
                        Category = "System",
                    },
                ]),
        ]);
        IPluginCommandCatalog commandCatalog = new PluginCommandCatalog(loadedPluginCatalog);
        IPluginQuickActionCatalog quickActionCatalog = new PluginQuickActionCatalog(loadedPluginCatalog, commandCatalog);

        var quickActions = quickActionCatalog.GetQuickActions();

        Assert.Collection(
            quickActions,
            first =>
            {
                Assert.Equal("first", first.ActionId);
                Assert.True(first.IsAvailable);
                Assert.Equal("""{"target":"stage"}""", first.ParameterJson);
            },
            second =>
            {
                Assert.Equal("second", second.ActionId);
                Assert.True(second.IsAvailable);
                Assert.Equal("{}", second.ParameterJson);
            },
            missing =>
            {
                Assert.Equal("missing", missing.ActionId);
                Assert.False(missing.IsAvailable);
                Assert.Contains("Quick.Plugin.Missing", missing.UnavailableReason, StringComparison.Ordinal);
            });
    }

    private static LoadedPlugin CreateLoadedPlugin(
        string pluginId,
        IReadOnlyList<PluginQuickActionDefinition> quickActions,
        IReadOnlyList<PluginCommandDefinition> commands)
    {
        return new LoadedPlugin
        {
            Manifest = new PluginManifest
            {
                PluginId = pluginId,
                DisplayName = pluginId,
                Version = "1.0.0",
                EntryAssembly = $"{pluginId}.dll",
                EntryType = $"{pluginId}.PluginEntry",
                Commands = commands,
                QuickActions = quickActions,
            },
            AssemblyPath = Assembly.GetExecutingAssembly().Location,
            Assembly = Assembly.GetExecutingAssembly(),
            LoadContext = AssemblyLoadContext.Default,
            Instance = new FakePlugin(pluginId),
        };
    }

    private sealed class FakeLoadedPluginCatalog(IReadOnlyList<LoadedPlugin> plugins) : ILoadedPluginCatalog
    {
        public IReadOnlyList<LoadedPlugin> GetLoadedPlugins() => plugins;
    }

    private sealed class FakePlugin(string pluginId) : IAutoToolPlugin
    {
        public PluginDescriptor Descriptor { get; } = new()
        {
            PluginId = pluginId,
            DisplayName = pluginId,
            Version = "1.0.0",
            EntryAssembly = $"{pluginId}.dll",
            EntryType = $"{pluginId}.PluginEntry",
        };

        public ValueTask<PluginInitializationResult> InitializeAsync(
            IPluginInitializationContext context,
            CancellationToken cancellationToken)
        {
            return ValueTask.FromResult(new PluginInitializationResult { IsSuccess = true });
        }

        public ValueTask DisposeAsync(CancellationToken cancellationToken) => ValueTask.CompletedTask;
    }
}
