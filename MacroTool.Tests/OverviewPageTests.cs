using Bunit;
using MacroTool.Components.Pages;

namespace MacroTool.Tests;

public sealed class OverviewPageTests : IDisposable
{
    private readonly UiTestScope _scope;

    public OverviewPageTests()
    {
        _scope = new UiTestScope();
        _scope.Engine.ReplaceTimeline(UiTestScope.SampleTimeline());
    }

    public void Dispose()
    {
        _scope.Dispose();
    }

    [Fact]
    public void Overview_RendersSession_Timeline_AndEnv()
    {
        var cut = _scope.Context.Render<Home>();

        Assert.Contains("当前会话", cut.Find(".dash-grid").TextContent);
        Assert.Contains("时间线", cut.Find(".dash-grid").TextContent);
        Assert.Contains("环境与热键", cut.Find(".dash-grid").TextContent);

        var kv = cut.FindAll(".kv");
        Assert.True(kv.Count >= 1);

        // 空闲态大指标
        Assert.Contains("事件", cut.Find(".metrics").TextContent);
        Assert.Contains("总时长", cut.Find(".metrics").TextContent);
    }

    [Fact]
    public void Overview_EmptyTimeline_ShowsGuidance()
    {
        var scope = new UiTestScope();
        try
        {
            var cut = scope.Context.Render<Home>();
            Assert.Contains("尚无时间线", cut.Find(".dash-grid").TextContent);
        }
        finally
        {
            scope.Dispose();
        }
    }
}
