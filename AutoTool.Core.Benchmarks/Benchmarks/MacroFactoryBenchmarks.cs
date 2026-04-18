using AutoTool.Commands.DependencyInjection;
using AutoTool.Commands.Interface;
using AutoTool.Panels.List.Class;
using AutoTool.Panels.Model.CommandDefinition;
using AutoTool.Panels.Model.List.Interface;
using AutoTool.Panels.Model.MacroFactory;
using AutoTool.Panels.Serialization;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Microsoft.Extensions.DependencyInjection;

namespace AutoTool.Core.Benchmarks.Benchmarks;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class MacroFactoryBenchmarks
{
    private ServiceProvider _provider = null!;
    private IMacroFactory _macroFactory = null!;
    private IReadOnlyList<ICommandListItem> _items = null!;

    [Params(100, 1000, 10000)]
    public int ItemCount { get; set; }

    [GlobalSetup]
    public void GlobalSetup()
    {
        var services = new ServiceCollection();
        services.AddCommandServices();
        services.AddSingleton<ICommandFactory, CommandFactory>();
        services.AddSingleton<ReflectionCommandRegistry>();
        services.AddSingleton<ICommandRegistry>(sp => sp.GetRequiredService<ReflectionCommandRegistry>());
        services.AddSingleton<ICommandDefinitionProvider>(sp => sp.GetRequiredService<ReflectionCommandRegistry>());
        services.AddTransient<ICompositeCommandBuilder, IfCompositeCommandBuilder>();
        services.AddTransient<ICompositeCommandBuilder, LoopCompositeCommandBuilder>();
        services.AddSingleton<IMacroFactory, MacroFactory>();

        _provider = services.BuildServiceProvider();
        _provider.GetRequiredService<ICommandRegistry>().Initialize();

        _macroFactory = _provider.GetRequiredService<IMacroFactory>();
        _items = BuildItems(ItemCount);
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        _provider.Dispose();
    }

    [Benchmark]
    public ICommand CreateMacro()
    {
        return _macroFactory.CreateMacro(_items);
    }

    private static IReadOnlyList<ICommandListItem> BuildItems(int count)
    {
        if (count < 3)
        {
            count = 3;
        }

        var registry = new ReflectionCommandRegistry();
        registry.Initialize();
        var list = new CommandList(registry, new MacroFileSerializer());

        list.Add(new LoopItem
        {
            ItemType = CommandTypeNames.Loop,
            LoopCount = 1
        });

        list.Add(new IfVariableItem
        {
            ItemType = CommandTypeNames.IfVariable,
            Name = "state",
            Operator = "==",
            Value = "ok"
        });

        var bodyCount = Math.Max(1, count - 4);
        for (var i = 0; i < bodyCount; i++)
        {
            list.Add(new WaitItem
            {
                ItemType = CommandTypeNames.Wait,
                Wait = 1
            });
        }

        list.Add(new IfEndItem
        {
            ItemType = CommandTypeNames.IfEnd
        });

        list.Add(new LoopEndItem
        {
            ItemType = CommandTypeNames.LoopEnd
        });

        return list.Items.ToList();
    }
}
