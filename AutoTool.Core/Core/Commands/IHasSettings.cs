using AutoTool.Core.Abstractions;

public interface IHasSettings<out TSettings> where TSettings : AutoToolCommandSettings
{
    TSettings Settings { get; }
}