using System.Reflection;
using AutoTool.Plugin.Abstractions.Interfaces;
using AutoTool.Plugin.Abstractions.Video;
using AutoTool.Plugin.Host.Abstractions;
using AutoTool.Plugin.Host.Models;

namespace AutoTool.Plugin.Host.Services;

public sealed class PluginLoader(
    IPluginCatalogLoader catalogLoader,
    IVideoStreamRegistry? videoStreamRegistry = null) : IPluginLoader
{
    private readonly IPluginCatalogLoader _catalogLoader = catalogLoader ?? throw new ArgumentNullException(nameof(catalogLoader));
    private readonly IVideoStreamRegistry _videoStreamRegistry = videoStreamRegistry ?? new VideoStreamRegistry();

    public PluginLoadResult Load(PluginManifestLoadResult manifestLoadResult)
    {
        ArgumentNullException.ThrowIfNull(manifestLoadResult);

        if (!manifestLoadResult.IsValid || manifestLoadResult.Manifest is null)
        {
            return new PluginLoadResult
            {
                ManifestLoadResult = manifestLoadResult,
                IsLoaded = false,
                Errors = manifestLoadResult.Errors,
            };
        }

        List<string> errors = [];
        var manifest = manifestLoadResult.Manifest;
        var assemblyPath = Path.Combine(manifestLoadResult.PluginDirectoryPath, manifest.EntryAssembly);

        try
        {
            var loadContext = new PluginAssemblyLoadContext(assemblyPath);
            var assembly = loadContext.LoadFromAssemblyPath(assemblyPath);
            var entryType = assembly.GetType(manifest.EntryType, throwOnError: false, ignoreCase: false);
            if (entryType is null)
            {
                errors.Add($"entryType が見つかりません: {manifest.EntryType}");
                loadContext.Unload();
                return new PluginLoadResult
                {
                    ManifestLoadResult = manifestLoadResult,
                    IsLoaded = false,
                    Errors = errors,
                };
            }

            if (!typeof(IAutoToolPlugin).IsAssignableFrom(entryType))
            {
                errors.Add($"entryType は IAutoToolPlugin を実装していません: {manifest.EntryType}");
                loadContext.Unload();
                return new PluginLoadResult
                {
                    ManifestLoadResult = manifestLoadResult,
                    IsLoaded = false,
                    Errors = errors,
                };
            }

            if (entryType.GetConstructor(Type.EmptyTypes) is null)
            {
                errors.Add($"entryType に引数なしコンストラクターがありません: {manifest.EntryType}");
                loadContext.Unload();
                return new PluginLoadResult
                {
                    ManifestLoadResult = manifestLoadResult,
                    IsLoaded = false,
                    Errors = errors,
                };
            }

            var instance = (IAutoToolPlugin)Activator.CreateInstance(entryType)!;
            var initializationContext = new PluginInitializationContext(
                hostVersion: typeof(PluginLoader).Assembly.GetName().Version?.ToString() ?? "1.0.0",
                pluginDirectoryPath: manifestLoadResult.PluginDirectoryPath,
                videoStreams: _videoStreamRegistry);
            var initializationResult = instance.InitializeAsync(initializationContext, CancellationToken.None)
                .AsTask()
                .GetAwaiter()
                .GetResult();
            if (!initializationResult.IsSuccess)
            {
                errors.Add(initializationResult.Message ?? "プラグイン初期化に失敗しました。");
                loadContext.Unload();
                return new PluginLoadResult
                {
                    ManifestLoadResult = manifestLoadResult,
                    IsLoaded = false,
                    Errors = errors,
                };
            }

            var commandDefinitionProvider = instance as IPluginCommandDefinitionProvider;
            var commandExecutor = instance as IPluginCommandExecutor;
            var healthCheck = instance as IPluginHealthCheck;
            var serviceRegistrar = instance as IPluginServiceRegistrar;
            var serviceRegistry = new PluginServiceRegistry();
            serviceRegistrar?.RegisterServices(serviceRegistry);

            return new PluginLoadResult
            {
                ManifestLoadResult = manifestLoadResult,
                IsLoaded = true,
                Plugin = new LoadedPlugin
                {
                    Manifest = manifest,
                    AssemblyPath = assemblyPath,
                    Assembly = assembly,
                    LoadContext = loadContext,
                    Instance = instance,
                    CommandDefinitionProvider = commandDefinitionProvider,
                    CommandExecutor = commandExecutor,
                    HealthCheck = healthCheck,
                    ServiceRegistrar = serviceRegistrar,
                    ServiceRegistrations = serviceRegistry.Registrations,
                },
                Errors = [],
            };
        }
        catch (Exception ex)
        {
            errors.Add($"プラグイン読み込みに失敗しました: {ex.Message}");
            return new PluginLoadResult
            {
                ManifestLoadResult = manifestLoadResult,
                IsLoaded = false,
                Errors = errors,
            };
        }
    }

    public IReadOnlyList<PluginLoadResult> LoadAll()
    {
        return _catalogLoader.LoadCatalog()
            .Select(Load)
            .ToList();
    }
}





