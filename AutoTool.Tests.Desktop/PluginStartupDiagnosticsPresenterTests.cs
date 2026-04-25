using AutoTool.Desktop.Services;
using AutoTool.Plugin.Host.Abstractions;
using AutoTool.Plugin.Host.Models;

namespace AutoTool.Plugin.Host.Tests;

/// <summary>
/// プラグイン起動時診断の表示用整形を確認するテストです。
/// </summary>
public sealed class PluginStartupDiagnosticsPresenterTests
{
    [Fact]
    public void BuildPresentation_WithHealthyDiagnostics_ReturnsHealthyStatusMessage()
    {
        IPluginStartupDiagnosticsCatalog catalog = new FakePluginStartupDiagnosticsCatalog(
        [
            new PluginStartupDiagnostics
            {
                PluginId = "Sample.Plugin",
                DisplayName = "Sample Plugin",
                Version = "1.0.0",
                IsHealthy = true,
                Summary = "正常",
                RequestedPermissions = ["sample.read"],
                Messages = ["health check passed"],
            }
        ]);

        var presentation = new PluginStartupDiagnosticsPresenter(catalog).BuildPresentation();

        Assert.Equal("プラグイン起動時診断: 正常 1 件", presentation.StatusMessage);
        Assert.False(presentation.ShouldOpenLogPanel);
        Assert.Contains(presentation.Entries, static x => x.Message.Contains("読み込みました", StringComparison.Ordinal));
        Assert.Contains(presentation.Entries, static x => x.Message.Contains("要求権限", StringComparison.Ordinal));
    }

    [Fact]
    public void BuildPresentation_WithUnhealthyDiagnostics_RequestsLogPanelOpen()
    {
        IPluginStartupDiagnosticsCatalog catalog = new FakePluginStartupDiagnosticsCatalog(
        [
            new PluginStartupDiagnostics
            {
                PluginId = "Sample.Plugin",
                DisplayName = "Sample Plugin",
                Version = "1.0.0",
                IsHealthy = false,
                Summary = "権限定義不足",
                RequestedPermissions = ["sample.read"],
                MissingPermissions = ["sample.write"],
                Messages = ["commands が要求する権限が permissions に不足しています: sample.write"],
            }
        ]);

        var presentation = new PluginStartupDiagnosticsPresenter(catalog).BuildPresentation();

        Assert.Equal("プラグイン起動時診断: 異常 1 件 / 正常 0 件", presentation.StatusMessage);
        Assert.True(presentation.ShouldOpenLogPanel);
        Assert.Contains(presentation.Entries, static x => x.Message.Contains("問題を検出しました", StringComparison.Ordinal));
        Assert.Contains(presentation.Entries, static x => x.Message.Contains("不足権限", StringComparison.Ordinal));
    }

    private sealed class FakePluginStartupDiagnosticsCatalog(IReadOnlyList<PluginStartupDiagnostics> diagnostics) : IPluginStartupDiagnosticsCatalog
    {
        private readonly IReadOnlyList<PluginStartupDiagnostics> _diagnostics = diagnostics;

        public IReadOnlyList<PluginStartupDiagnostics> GetDiagnostics() => _diagnostics;
    }
}

