using AutoTool.Domain.Automation.Conditions;
using AutoTool.Domain.Macros;

namespace AutoTool.Tests.Domain;

public class ConditionTypeTests
{
    [Fact]
    public void GetTypes_ReturnsExpectedConditionTypesInOrder()
    {
        var types = ConditionType.GetTypes().ToArray();

        Assert.Equal(
            [
                ConditionType.True,
                ConditionType.False,
                ConditionType.ImageExists,
                ConditionType.ImageNotExists
            ],
            types);
    }

    [Fact]
    public void GetTypes_DoesNotContainDuplicateOrEmptyValues()
    {
        var types = ConditionType.GetTypes().ToArray();

        Assert.Equal(types.Length, types.Distinct(StringComparer.Ordinal).Count());
        Assert.DoesNotContain(types, string.IsNullOrWhiteSpace);
    }

    [Theory]
    [InlineData("True", true)]
    [InlineData("ImageExists", true)]
    [InlineData(" true ", false)]
    [InlineData("Unknown", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsSupported_ReturnsExpectedResult(string? value, bool expected)
    {
        var actual = ConditionType.IsSupported(value);
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(" True ", true, "True")]
    [InlineData("ImageNotExists", true, "ImageNotExists")]
    [InlineData("unknown", false, "")]
    [InlineData("", false, "")]
    [InlineData(null, false, "")]
    public void TryParse_ReturnsExpectedResult(string? value, bool expected, string parsed)
    {
        var result = ConditionType.TryParse(value, out var conditionType);

        Assert.Equal(expected, result);
        Assert.Equal(parsed, conditionType);
    }
}

public class FavoriteMacroEntryTests
{
    [Fact]
    public void Constructor_InitializesPropertiesWithSafeDefaults()
    {
        var entry = new FavoriteMacroEntry();

        Assert.Equal(string.Empty, entry.Name);
        Assert.Equal(string.Empty, entry.SnapshotPath);
        Assert.Equal(default, entry.CreatedAt);
    }

    [Fact]
    public void Properties_CanStoreAssignedValues()
    {
        var createdAt = new DateTimeOffset(2026, 4, 18, 12, 0, 0, TimeSpan.FromHours(9));
        var entry = new FavoriteMacroEntry
        {
            Name = "サンプルマクロ",
            SnapshotPath = @"C:\snapshots\macro.png",
            CreatedAt = createdAt
        };

        Assert.Equal("サンプルマクロ", entry.Name);
        Assert.Equal(@"C:\snapshots\macro.png", entry.SnapshotPath);
        Assert.Equal(createdAt, entry.CreatedAt);
    }

    [Fact]
    public void Create_NormalizesValues()
    {
        var createdAt = new DateTimeOffset(2026, 4, 18, 12, 0, 0, TimeSpan.FromHours(9));

        var entry = FavoriteMacroEntry.Create("  サンプル  ", "  C:\\macro\\a.macro  ", createdAt);

        Assert.Equal("サンプル", entry.Name);
        Assert.Equal("C:\\macro\\a.macro", entry.SnapshotPath);
        Assert.Equal(createdAt, entry.CreatedAt);
    }

    [Theory]
    [InlineData("", "a.macro", "name")]
    [InlineData("a", "", "snapshotPath")]
    [InlineData("   ", "a.macro", "name")]
    [InlineData("a", "   ", "snapshotPath")]
    public void Create_InvalidArgument_ThrowsArgumentException(string name, string path, string paramName)
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            FavoriteMacroEntry.Create(name, path, DateTimeOffset.Now));

        Assert.Equal(paramName, ex.ParamName);
    }

    [Fact]
    public void Normalize_And_IsValid_WorkAsExpected()
    {
        var entry = new FavoriteMacroEntry
        {
            Name = "  test  ",
            SnapshotPath = "  C:\\a.macro  "
        };

        Assert.True(entry.IsValid());

        entry.Normalize();
        Assert.Equal("test", entry.Name);
        Assert.Equal("C:\\a.macro", entry.SnapshotPath);

        entry.Name = " ";
        Assert.False(entry.IsValid());
    }

    [Fact]
    public void RecordEquality_WithSameValues_IsEqual()
    {
        var createdAt = new DateTimeOffset(2026, 4, 18, 12, 0, 0, TimeSpan.FromHours(9));
        var left = FavoriteMacroEntry.Create("name", @"C:\a.macro", createdAt);
        var right = FavoriteMacroEntry.Create("name", @"C:\a.macro", createdAt);

        Assert.Equal(left, right);
    }
}
