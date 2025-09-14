using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using AutoTool.Core.Abstractions;
using AutoTool.Core.Attributes;
using AutoTool.Core.Commands;
using AutoTool.Core.Descriptors;
using AutoTool.Core.Registration;
using Microsoft.Extensions.Logging;
using System.Runtime.Loader;

namespace AutoTool.Core.Services
{
    /// <summary>
    /// Attributeベースのコマンド自動登録サービス
    /// </summary>
    public sealed class AttributeCommandRegistrationService
    {
        private readonly ILogger<AttributeCommandRegistrationService> _logger;

        public AttributeCommandRegistrationService(ILogger<AttributeCommandRegistrationService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 指定されたアセンブリからCommandAttributeが付与されたクラスを検索し、
        /// ICommandRegistryに自動登録する
        /// </summary>
        /// <param name="registry">登録先のレジストリ</param>
        /// <param name="assemblies">検索対象のアセンブリ配列</param>
        /// <returns>登録されたコマンド数</returns>
        public int RegisterCommandsFromAssemblies(ICommandRegistry registry, params Assembly[] assemblies)
        {
            if (registry == null) throw new ArgumentNullException(nameof(registry));
            if (assemblies == null || assemblies.Length == 0)
            {
                _logger.LogWarning("検索対象のアセンブリが指定されていません");
                return 0;
            }

            var registeredCount = 0;
            var discoveredCommands = new List<(Type CommandType, CommandAttribute Attribute)>();

            // 1. すべてのアセンブリからコマンドクラスを発見
            foreach (var assembly in assemblies)
            {
                try
                {
                    _logger.LogDebug("アセンブリを検索中: {AssemblyName}", assembly.GetName().Name);
                    
                    var commandTypes = assembly.GetTypes()
                        .Where(type => type.IsClass && 
                                      !type.IsAbstract && 
                                      typeof(IAutoToolCommand).IsAssignableFrom(type) &&
                                      type.GetCustomAttribute<CommandAttribute>() != null)
                        .ToArray();

                    foreach (var commandType in commandTypes)
                    {
                        var attribute = commandType.GetCustomAttribute<CommandAttribute>()!;
                        discoveredCommands.Add((commandType, attribute));
                        
                        _logger.LogDebug("コマンド発見: Type={Type}, DisplayName={DisplayName}, Class={ClassName}", 
                            attribute.Type, attribute.DisplayName, commandType.Name);
                    }
                }
                catch (ReflectionTypeLoadException ex)
                {
                    _logger.LogWarning(ex, "アセンブリ {AssemblyName} の型読み込み中に一部エラーが発生しました", 
                        assembly.GetName().Name);
                    
                    // 読み込めた型だけを処理対象に含める
                    var loadedTypes = ex.Types.Where(t => t != null).ToArray();
                    foreach (var type in loadedTypes)
                    {
                        if (type!.IsClass && 
                            !type.IsAbstract && 
                            typeof(IAutoToolCommand).IsAssignableFrom(type))
                        {
                            var attribute = type.GetCustomAttribute<CommandAttribute>();
                            if (attribute != null)
                            {
                                discoveredCommands.Add((type, attribute));
                                _logger.LogDebug("コマンド発見（部分読み込み）: Type={Type}, DisplayName={DisplayName}", 
                                    attribute.Type, attribute.DisplayName);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "アセンブリ {AssemblyName} の処理中にエラーが発生しました", 
                        assembly.GetName().Name);
                }
            }

            // 2. 発見されたコマンドをOrderに従ってソート
            var sortedCommands = discoveredCommands
                .OrderBy(cmd => cmd.Attribute.Order)
                .ThenBy(cmd => cmd.Attribute.DisplayName)
                .ToArray();

            _logger.LogInformation("発見されたコマンド数: {Count}", sortedCommands.Length);

            // 3. レジストリに登録
            foreach (var (commandType, attribute) in sortedCommands)
            {
                try
                {
                    var descriptor = new AttributeBasedDescriptor(commandType, attribute, _logger);
                    
                    // 重複チェック
                    if (registry.TryGet(attribute.Type, out var existingDescriptor))
                    {
                        _logger.LogWarning("コマンドタイプが重複しています。スキップします: Type={Type}, 既存={ExistingType}", 
                            attribute.Type, existingDescriptor?.GetType().Name);
                        continue;
                    }

                    // レジストリに登録（具体的な登録メソッドはICommandRegistryの実装に依存）
                    RegisterDescriptor(registry, descriptor);
                    registeredCount++;

                    _logger.LogDebug("コマンド登録完了: Type={Type}, DisplayName={DisplayName}", 
                        attribute.Type, attribute.DisplayName);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "コマンド登録中にエラー: Type={Type}, Class={ClassName}", 
                        attribute.Type, commandType.Name);
                }
            }

            _logger.LogInformation("コマンド自動登録完了: 登録数={RegisteredCount}/{DiscoveredCount}", 
                registeredCount, sortedCommands.Length);

            return registeredCount;
        }

        /// <summary>
        /// ICommandRegistryにDescriptorを登録する
        /// </summary>
        private void RegisterDescriptor(ICommandRegistry registry, ICommandDescriptor descriptor)
        {
            // 1. IDynamicCommandRegistryをサポートするレジストリの場合
            if (registry is IDynamicCommandRegistry dynamicRegistry)
            {
                dynamicRegistry.Register(descriptor);
                return;
            }

            // 2. 静的レジストリの場合、リフレクションで内部コレクションにアクセス
            try
            {
                var registryType = registry.GetType();
                
                // Addメソッドを探す
                var addMethod = registryType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                    .FirstOrDefault(m => m.Name == "Add" && m.GetParameters().Length == 1);
                
                if (addMethod != null)
                {
                    addMethod.Invoke(registry, new object[] { descriptor });
                    return;
                }

                // 内部フィールドやプロパティを探す
                var descriptorsField = registryType.GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
                    .FirstOrDefault(f => typeof(System.Collections.IDictionary).IsAssignableFrom(f.FieldType));

                if (descriptorsField != null)
                {
                    var collection = descriptorsField.GetValue(registry);
                    if (collection is System.Collections.Generic.IDictionary<string, ICommandDescriptor> dict)
                    {
                        dict[descriptor.Type] = descriptor;
                        return;
                    }
                    else if (collection is System.Collections.IDictionary generalDict)
                    {
                        generalDict[descriptor.Type] = descriptor;
                        return;
                    }
                }

                // _mapフィールドを直接探す（CommandRegistryの場合）
                var mapField = registryType.GetField("_map", BindingFlags.NonPublic | BindingFlags.Instance);
                if (mapField != null)
                {
                    var map = mapField.GetValue(registry);
                    if (map is Dictionary<string, ICommandDescriptor> typedMap)
                    {
                        typedMap[descriptor.Type] = descriptor;
                        return;
                    }
                }

                _logger.LogWarning("レジストリに登録メソッドが見つかりません: {RegistryType}", registryType.Name);
                throw new InvalidOperationException($"Cannot register descriptor in registry type: {registryType.Name}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "リフレクションによるDescriptor登録に失敗: {DescriptorType}", descriptor.Type);
                throw new InvalidOperationException($"Failed to register descriptor {descriptor.Type} via reflection", ex);
            }
        }

        /// <summary>
        /// 現在実行中のアセンブリドメインから自動的にコマンドを登録
        /// ※ 出力フォルダの AutoTool*.dll を見て、未ロードのものは明示的にロードして探索します。
        /// </summary>
        public int RegisterCommandsFromCurrentDomain(ICommandRegistry registry)
        {
            // 1) まず現在ロードされているアセンブリを取得
            var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
                .ToList();

            _logger.LogDebug("現在のドメインにロード済みアセンブリ: {Count}個", loadedAssemblies.Count);
            foreach (var a in loadedAssemblies)
            {
                _logger.LogDebug("- {Name}", a.GetName().Name);
            }

            // 2) 出力フォルダ内の AutoTool*.dll を列挙し、まだロードされていないアセンブリをロードする
            try
            {
                var baseDir = AppContext.BaseDirectory;
                var dllFiles = Directory.EnumerateFiles(baseDir, "AutoTool*.dll", SearchOption.TopDirectoryOnly);
                foreach (var dll in dllFiles)
                {
                    try
                    {
                        var asmName = AssemblyName.GetAssemblyName(dll);
                        if (loadedAssemblies.Any(a => string.Equals(a.GetName().Name, asmName.Name, StringComparison.OrdinalIgnoreCase)))
                        {
                            continue; // 既にロード済み
                        }

                        // 安全にロード（既存のLoadコンテキストに追加）
                        var loaded = AssemblyLoadContext.Default.LoadFromAssemblyPath(dll);
                        loadedAssemblies.Add(loaded);
                        _logger.LogDebug("ディスクからアセンブリをロード: {Assembly}", asmName.Name);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, "出力フォルダのアセンブリ読み込みに失敗: {Path}", dll);
                        // 続行
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "出力フォルダスキャン中に例外が発生しました");
            }

            // 3) AutoTool で始まるアセンブリのみを対象にする（既存ロジックを再現）
            var assemblies = loadedAssemblies
                .Where(a => (a.GetName().Name?.StartsWith("AutoTool", StringComparison.OrdinalIgnoreCase) ?? false))
                .ToArray();

            _logger.LogDebug("現在のドメインから検索対象アセンブリを特定: {Count}個", assemblies.Length);
            foreach (var assembly in assemblies)
            {
                _logger.LogDebug("- {AssemblyName}", assembly.GetName().Name);
            }

            return RegisterCommandsFromAssemblies(registry, assemblies);
        }
    }

    /// <summary>
    /// 動的なコマンド登録をサポートするレジストリインターフェース
    /// </summary>
    public interface IDynamicCommandRegistry : ICommandRegistry
    {
        void Register(ICommandDescriptor descriptor);
        bool Unregister(string type);
    }
}