namespace AutoTool.Tests.Plugin.Sample;

public sealed class SamplePluginService : ISamplePluginService
{
    public string GetValue()
    {
        return "sample";
    }
}

