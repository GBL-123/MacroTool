using Bunit;
using MacroTool.Application.Engine;
using MacroTool.Components.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace MacroTool.Tests;

public sealed class ControlBarTests : IDisposable
{
    private readonly UiTestScope _scope;

    public ControlBarTests()
    {
        _scope = new UiTestScope();
    }

    private IRenderedComponent<DialogHost<ControlBar>> RenderIdle()
    {
        _scope.Engine.ReplaceTimeline(UiTestScope.SampleTimeline());
        return _scope.Context.Render<DialogHost<ControlBar>>();
    }

    [Fact]
    public void IdleState_TransportAvailability_AndNav_AndMetrics()
    {
        var cut = RenderIdle();

        var record = cut.FindAll("button").Single(b => b.TextContent.Trim() == "录制");
        var play = cut.FindAll("button").Single(b => b.TextContent.Trim() == "回放");
        var stop = cut.FindAll("button").Single(b => b.TextContent.Trim() == "停止");

        Assert.False(record.HasAttribute("disabled"));
        Assert.False(play.HasAttribute("disabled"));
        Assert.True(stop.HasAttribute("disabled"));

        var navText = cut.Find(".nav-tabs").TextContent;
        Assert.Contains("概览", navText);
        Assert.Contains("时间线", navText);
        Assert.Contains("日志", navText);

        Assert.Contains("5", cut.Find(".bar-metrics").TextContent);
        Assert.Contains("空闲", cut.Find(".state-pill").TextContent);
    }

    [Fact]
    public void PlayingState_TransportAvailability_Switches()
    {
        var scope = new UiTestScope(new PlayerDelegates(
            (_, _, _) => true, (_, _) => true, () => true));
        try
        {
            scope.Engine.ReplaceTimeline(new MacroTimeline
            {
                DurationMs = 40,
                Events = [UiTestScope.Key(10, up: false), UiTestScope.Key(30, up: true)]
            });
            scope.Engine.StartPlayback();

            var cut = scope.Context.Render<DialogHost<ControlBar>>();
            cut.WaitForState(() => cut.Find(".state-pill").TextContent.Contains("回放中"));

            var play = cut.FindAll("button").Single(b => b.TextContent.Trim() == "停止回放");
            Assert.False(play.HasAttribute("disabled"));
            var record = cut.FindAll("button").Single(b => b.TextContent.Trim() == "录制");
            Assert.True(record.HasAttribute("disabled"));

            scope.Engine.StopPlayback();
        }
        finally
        {
            scope.Dispose();
        }
    }

    [Fact]
    public void Apply_ValidValues_UpdatesEngineSettings()
    {
        var cut = RenderIdle();

        var inputs = cut.FindAll(".bar-field input");
        Assert.Equal(3, inputs.Count);
        inputs[0].Change("2.5");
        inputs[1].Change("5");
        inputs[2].Change("3");

        cut.FindAll("button").Single(b => b.TextContent.Trim() == "应用").Click();

        Assert.Equal(2.5, _scope.Settings.Speed);
        Assert.Equal(5, _scope.Settings.Jitter);
        Assert.Equal(3, _scope.Settings.RepeatCount);
    }

    [Fact]
    public void Apply_UiClampsBelowMin_BeforeEngineValidation()
    {
        var cut = RenderIdle();

        var inputs = cut.FindAll(".bar-field input");
        inputs[0].Change("0");

        cut.FindAll("button").Single(b => b.TextContent.Trim() == "应用").Click();

        // UI 层把低于 Min(0.1) 的输入钳制为 0.1；引擎侧的非法值拒绝由 MacroEngineTests 覆盖
        Assert.Equal(0.1, _scope.Settings.Speed);
    }

    [Fact]
    public void Shutdown_Confirmed_RequestsEngineShutdown()
    {
        var cut = RenderIdle();

        var raised = false;
        _scope.Engine.ShutdownRequested += () => raised = true;

        cut.Find("button[title='关闭服务']").Click();
        var dialogYes = cut.FindAll(".mud-dialog button").Single(b => b.TextContent.Trim() == "关闭");
        dialogYes.Click();

        Assert.True(raised);
    }

    [Fact]
    public void Shutdown_Cancelled_DoesNotRequestShutdown()
    {
        var cut = RenderIdle();

        var raised = false;
        _scope.Engine.ShutdownRequested += () => raised = true;

        cut.Find("button[title='关闭服务']").Click();
        var dialogCancel = cut.FindAll(".mud-dialog button").Single(b => b.TextContent.Trim() == "取消");
        dialogCancel.Click();

        Assert.False(raised);
    }

    public void Dispose()
    {
        _scope.Dispose();
    }
}

/// <summary>Renders MudDialogProvider plus the component under test so dialogs have a host.</summary>
public sealed class DialogHost<TComponent> : ComponentBase where TComponent : ComponentBase
{
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<MudBlazor.MudDialogProvider>(0);
        builder.CloseComponent();
        builder.OpenComponent<TComponent>(1);
        builder.CloseComponent();
    }
}
