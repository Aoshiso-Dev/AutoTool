namespace AutoTool.Core.Abstractions;

// Settings の共通基底。with/不変を活かすため record に。
public abstract record AutoToolCommandSettings
{
    public int Version { get; init; }
}