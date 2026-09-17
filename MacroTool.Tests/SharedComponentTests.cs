using Bunit;
using MacroTool.Application.Engine;
using MacroTool.Domain;
using MacroTool.Infrastructure.Hotkeys;
using MacroTool.Web.Components.Shared;

namespace MacroTool.Tests;

public sealed class SharedComponentTests : IDisposable
{
    private readonly UiTestScope _scope;

    public SharedComponentTests()
    {
        _scope = new UiTestScope();
    }

    public void Dispose()
    {
        _scope.Dispose();
    }

    [Fact]
    public void EngineStatusChip_ShowsIdle_AndUpdatesToPlaying()
    {
        var scope = new UiTestScope(new FakeInputSink());
        try
        {
            scope.Engine.ReplaceTimeline(new MacroTimeline
            {
                DurationMs = 40,
                Events = [UiTestScope.Key(10, up: false), UiTestScope.Key(30, up: true)]
            });

            var cut = scope.Context.Render<EngineStatusChip>();
            Assert.Contains("空闲", cut.Find(".state-pill").TextContent);

            scope.Engine.StartPlayback();
            cut.WaitForState(() => cut.Find(".state-pill").TextContent.Contains("回放中"));
            Assert.Contains("回放中", cut.Find(".state-pill").TextContent);

            scope.Engine.StopPlayback();
        }
        finally
        {
            scope.Dispose();
        }
    }

    [Fact]
    public void Metric_RendersLabelAndValue()
    {
        var cut = _scope.Context.Render<Metric>(parameters => parameters
            .Add(p => p.Label, "点击")
            .Add(p => p.Value, "12"));

        Assert.Contains("点击", cut.Find(".metric-label").TextContent);
        Assert.Equal("12", cut.Find(".metric-value").TextContent.Trim());
    }

    [Fact]
    public void EnvHints_ShowsHotkeyRows_AndDegradedState()
    {
        var cut = _scope.Context.Render<EnvHints>();

        var text = cut.Find(".kv").TextContent;
        Assert.Contains("录制 / 回放", text);
        // HotkeyHost 未 Start 时为默认未注册状态 -> 降级文案
        Assert.Contains("注册失败", text);
        Assert.Contains("退出热键", text);
        Assert.Contains("时间线文件", text);
    }
}
