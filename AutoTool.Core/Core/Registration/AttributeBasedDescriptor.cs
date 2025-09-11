using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AutoTool.Core.Abstractions;
using AutoTool.Core.Attributes;
using AutoTool.Core.Commands;
using AutoTool.Core.Descriptors;
using Microsoft.Extensions.Logging;

namespace AutoTool.Core.Registration
{
    /// <summary>
    /// CommandAttributeが付与されたクラスから自動的にDescriptorを生成する
    /// </summary>
    public sealed class AttributeBasedDescriptor : ICommandDescriptor
    {
        public string Type { get; }
        public string DisplayName { get; }
        public string? IconKey { get; }
        public Type SettingsType { get; }
        public int LatestSettingsVersion { get; } = 1;
        public IReadOnlyList<BlockSlot> BlockSlots { get; }

        /// <summary>
        /// カテゴリ（AttributeBasedDescriptor固有）
        /// </summary>
        public string? Category { get; }

        /// <summary>
        /// 表示順序（AttributeBasedDescriptor固有）
        /// </summary>
        public int Order { get; }

        /// <summary>
        /// 説明（AttributeBasedDescriptor固有）
        /// </summary>
        public string? Description { get; }

        private readonly Type _commandType;
        private readonly ILogger? _logger;

        public AttributeBasedDescriptor(Type commandType, CommandAttribute attribute, ILogger? logger = null)
        {
            _commandType = commandType ?? throw new ArgumentNullException(nameof(commandType));
            _logger = logger;

            Type = attribute.Type;
            DisplayName = attribute.DisplayName;
            IconKey = attribute.IconKey;
            Category = attribute.Category;
            Order = attribute.Order;
            Description = attribute.Description;

            // IHasSettings<T>からSettingsTypeを取得
            var hasSettingsInterface = commandType.GetInterfaces()
                .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IHasSettings<>));

            if (hasSettingsInterface != null)
            {
                SettingsType = hasSettingsInterface.GetGenericArguments()[0];
            }
            else
            {
                // フォールバック: 設定なしの場合は基底Settings型を使用
                SettingsType = typeof(AutoToolCommandSettings);
            }

            // IHasBlocksからBlockSlotsを取得
            var hasBlocksInterface = commandType.GetInterfaces()
                .FirstOrDefault(i => i == typeof(IHasBlocks));

            if (hasBlocksInterface != null)
            {
                // TODO: AttributeまたはstaticプロパティからBlockSlot情報を取得
                // 現在は空配列をデフォルトとする
                BlockSlots = Array.Empty<BlockSlot>();
            }
            else
            {
                BlockSlots = Array.Empty<BlockSlot>();
            }

            _logger?.LogDebug("AttributeBasedDescriptor作成: Type={Type}, DisplayName={DisplayName}, CommandType={CommandType}", 
                Type, DisplayName, commandType.Name);
        }

        public AutoToolCommandSettings CreateDefaultSettings()
        {
            try
            {
                // デフォルトコンストラクタでSettingsインスタンスを作成
                return (AutoToolCommandSettings)Activator.CreateInstance(SettingsType)!;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "デフォルト設定の作成に失敗: SettingsType={SettingsType}", SettingsType.Name);
                throw new InvalidOperationException($"Failed to create default settings for {SettingsType.Name}", ex);
            }
        }

        public AutoToolCommandSettings MigrateToLatest(AutoToolCommandSettings settings)
        {
            // 基本的な実装：バージョンを最新に更新
            if (settings.GetType() == SettingsType)
            {
                // recordのwith構文を使用してバージョンを更新
                // これは各Settings型がrecordである前提
                try
                {
                    var withMethod = SettingsType.GetMethod("<Clone>$", BindingFlags.Public | BindingFlags.Instance);
                    if (withMethod != null)
                    {
                        var cloned = (AutoToolCommandSettings)withMethod.Invoke(settings, null)!;
                        // Versionプロパティを更新（recordのイミュータブル特性により新しいインスタンスを作成）
                        return cloned; // 現在はバージョン更新なしでそのまま返す
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "設定のマイグレーション中に警告: Type={Type}", Type);
                }

                return settings;
            }

            throw new InvalidCastException($"Unexpected settings type: {settings.GetType().Name}");
        }

        public IEnumerable<string> ValidateSettings(AutoToolCommandSettings settings)
        {
            if (settings.GetType() != SettingsType)
            {
                yield return "Settings type mismatch.";
                yield break;
            }

            // 基本的な検証のみ実装
            // 具体的な検証は各コマンドのIValidatableCommand実装で行う
            yield break;
        }

        public IAutoToolCommand CreateCommand(AutoToolCommandSettings settings, IServiceProvider services)
        {
            try
            {
                // コンストラクタパターンを試行
                var constructors = _commandType.GetConstructors();

                // (TSettings settings) パターンを探す
                var settingsConstructor = constructors.FirstOrDefault(c =>
                {
                    var parameters = c.GetParameters();
                    return parameters.Length == 1 && parameters[0].ParameterType == SettingsType;
                });

                if (settingsConstructor != null)
                {
                    var command = (IAutoToolCommand)Activator.CreateInstance(_commandType, settings)!;
                    _logger?.LogDebug("コマンド作成成功 (settings constructor): Type={Type}", Type);
                    return command;
                }

                // (TSettings settings, IServiceProvider services) パターンを探す
                var settingsServiceConstructor = constructors.FirstOrDefault(c =>
                {
                    var parameters = c.GetParameters();
                    return parameters.Length == 2 && 
                           parameters[0].ParameterType == SettingsType &&
                           parameters[1].ParameterType == typeof(IServiceProvider);
                });

                if (settingsServiceConstructor != null)
                {
                    var command = (IAutoToolCommand)Activator.CreateInstance(_commandType, settings, services)!;
                    _logger?.LogDebug("コマンド作成成功 (settings+services constructor): Type={Type}", Type);
                    return command;
                }

                // デフォルトコンストラクタを試行
                var defaultConstructor = constructors.FirstOrDefault(c => c.GetParameters().Length == 0);
                if (defaultConstructor != null)
                {
                    var command = (IAutoToolCommand)Activator.CreateInstance(_commandType)!;
                    
                    // 設定を後から注入できるかチェック
                    if (command is IHasSettings<AutoToolCommandSettings> hasSettings)
                    {
                        // TODO: 設定注入メソッドがあれば呼び出し
                        // 現在は設定注入をサポートしない
                    }

                    _logger?.LogDebug("コマンド作成成功 (default constructor): Type={Type}", Type);
                    return command;
                }

                throw new InvalidOperationException($"No suitable constructor found for command type {_commandType.Name}");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "コマンド作成に失敗: Type={Type}, CommandType={CommandType}", Type, _commandType.Name);
                throw new InvalidOperationException($"Failed to create command instance for {Type}", ex);
            }
        }
    }
}