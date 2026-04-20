using AutoTool.Commands.DependencyInjection;
using AutoTool.Commands.Interface;
using AutoTool.Infrastructure.DependencyInjection;
using AutoTool.Automation.Runtime.Lists;
using AutoTool.Automation.Runtime.Definitions;
using AutoTool.Automation.Contracts.Lists;
using AutoTool.Automation.Runtime.MacroFactory;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Microsoft.Extensions.DependencyInjection;

namespace AutoTool.Automation.Runtime.Benchmarks.Benchmarks;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
/// <summary>
/// マクロ生成処理の性能を測定するベンチマークです。
/// </summary>
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
        services.AddMacroRuntimeCoreServices();

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
        var list = new CommandList(registry);

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
