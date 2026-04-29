using AutoTool.Automation.Runtime.Attributes;

namespace AutoTool.Tests.Desktop;

public sealed class DurationPropertyMetadataTests
{
    [Fact]
    public void DurationParts_ReadMillisecondsAsHoursMinutesSeconds()
    {
        var target = new DurationTarget { Wait = 3_723_500 };
        var metadata = CreateMetadata(target);

        Assert.Equal(1, metadata.DurationHours);
        Assert.Equal(2, metadata.DurationMinutes);
        Assert.Equal(3.5, metadata.DurationSeconds);
    }

    [Fact]
    public void DurationParts_WriteBackAsMilliseconds()
    {
        var target = new DurationTarget();
        var metadata = CreateMetadata(target);

        metadata.DurationHours = 1;
        metadata.DurationMinutes = 2;
        metadata.DurationSeconds = 3.5;

        Assert.Equal(3_723_500, target.Wait);
    }

    private static PropertyMetadata CreateMetadata(DurationTarget target)
    {
        var property = typeof(DurationTarget).GetProperty(nameof(DurationTarget.Wait))!;
        var attribute = property.GetCustomAttributes(typeof(CommandPropertyAttribute), inherit: false)
            .OfType<CommandPropertyAttribute>()
            .Single();

        return new PropertyMetadata
        {
            PropertyInfo = property,
            Attribute = attribute,
            Target = target
        };
    }

    private sealed class DurationTarget
    {
        [CommandProperty("待機時間", EditorType.Duration, Min = 0)]
        public int Wait { get; set; }
    }
}
