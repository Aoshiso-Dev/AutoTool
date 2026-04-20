using AutoTool.Application.Files;
using AutoTool.Application.Ports;

namespace AutoTool.Automation.Runtime.Tests;

/// <summary>
/// ポート層モデルの基本挙動テストです。
/// </summary>
public class PortsModelTests
{
    [Fact]
    public void RecentFileEntry_DefaultConstructor_InitializesSafeDefaults()
    {
        var entry = new RecentFileEntry();

        Assert.Equal(string.Empty, entry.FileName);
        Assert.Equal(string.Empty, entry.FilePath);
    }

    [Fact]
    public void RecentFileEntry_AssignedProperties_AreStored()
    {
        var entry = new RecentFileEntry
        {
            FileName = "sample.macro",
            FilePath = @"C:\macro\sample.macro"
        };

        Assert.Equal("sample.macro", entry.FileName);
        Assert.Equal(@"C:\macro\sample.macro", entry.FilePath);
    }

    [Fact]
    public void FileTypeInfo_DefaultConstructor_InitializesSafeDefaults()
    {
        var info = new FileManager.FileTypeInfo();

        Assert.Equal(string.Empty, info.Filter);
        Assert.Equal(string.Empty, info.DefaultExt);
        Assert.Equal(string.Empty, info.Title);
    }

    [Fact]
    public void FileTypeInfo_AssignedProperties_AreStored()
    {
        var info = new FileManager.FileTypeInfo
        {
            Filter = "Macro|*.macro",
            FilterIndex = 2,
            RestoreDirectory = true,
            DefaultExt = "macro",
            Title = "Open Macro"
        };

        Assert.Equal("Macro|*.macro", info.Filter);
        Assert.Equal(2, info.FilterIndex);
        Assert.True(info.RestoreDirectory);
        Assert.Equal("macro", info.DefaultExt);
        Assert.Equal("Open Macro", info.Title);
    }
}
