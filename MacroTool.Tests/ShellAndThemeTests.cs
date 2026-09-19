using Bunit;
using MacroTool.Application.Engine;
using MacroTool.Domain;
using MacroTool.Web.Components.Layout;
using MacroTool.Web.Components.Pages;
using MacroTool.Web.Components.Shared;
using MacroTool.Web.Components.Theme;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.Logging;

namespace MacroTool.Tests;

public sealed class ShellAndThemeTests : IDisposable
{
    private readonly UiTestScope _scope;

    public ShellAndThemeTests()
    {
        _scope = new UiTestScope();
        _scope.Engine.ReplaceTimeline(UiTestScope.SampleTimeline());
    }

    public void Dispose()
    {
        _scope.Dispose();
    }

    [Fact]
    public void MacroTheme_InstanceBuilt_WithLightPalette()
    {
        var theme = MacroTheme.Instance;

        Assert.NotNull(theme.PaletteLight);
        Assert.Equal("#F7F6F3", theme.PaletteLight!.Background);
        Assert.Equal("#111111", theme.PaletteLight.Primary);
        Assert.NotEmpty(theme.Typography.Default.FontFamily!);
    }

    [Fact]
    public void LogsPage_RendersLogPanel()
    {
        _scope.Logs.Add(new EngineLogEntry(DateTimeOffset.Now, LogLevel.Information, "hello log"));

        var cut = _scope.Context.Render<Logs>();

        Assert.Contains("hello log", cut.Find(".log-panel").TextContent);
    }

    [Fact]
    public void LogViewer_ShowsSnapshot_AndStreamsNewEntries()
    {
        var scope = new UiTestScope();
        try
        {
            var cut = scope.Context.Render<LogViewer>();

            Assert.Contains("暂无日志", cut.Find(".log-panel").TextContent);

            scope.Logs.Add(new EngineLogEntry(DateTimeOffset.Now, LogLevel.Warning, "streamed entry"));
            cut.WaitForState(() => cut.Find(".log-panel").TextContent.Contains("streamed entry"));

            var warnLine = cut.Find(".log-line.is-warn");
            Assert.NotNull(warnLine);
        }
        finally
        {
            scope.Dispose();
        }
    }

    [Fact]
    public void LogViewer_FollowDisabled_DoesNotThrowOnScroll()
    {
        var scope = new UiTestScope();
        try
        {
            var cut = scope.Context.Render<LogViewer>();

            var switches = cut.FindAll("input[type='checkbox']");
            Assert.NotEmpty(switches);
            switches[0].Change(false);

            scope.Logs.Add(new EngineLogEntry(DateTimeOffset.Now, LogLevel.Information, "no follow"));
            cut.WaitForState(() => cut.Find(".log-panel").TextContent.Contains("no follow"));
        }
        finally
        {
            scope.Dispose();
        }
    }

    [Fact]
    public void MainLayout_RendersShell_WithControlBarAndBody()
    {
        RenderFragment body = builder => builder.AddContent(0, "BODY-CONTENT");

        var cut = _scope.Context.Render<MainLayout>(parameters => parameters
            .Add(x => x.Body!, body));

        Assert.Contains("BODY-CONTENT", cut.Find(".page").TextContent);
        Assert.Contains("MacroTool", cut.Find(".brand-name").TextContent);
        Assert.NotNull(cut.Find(".mud-appbar"));
        Assert.NotNull(cut.Find(".mud-main-content"));
    }
}
