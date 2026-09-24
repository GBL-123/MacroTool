using Bunit;
using MacroTool.Application.Engine;
using MacroTool.Web.Components.Shared;
using MacroTool.Domain;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;

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
        var scope = new UiTestScope(new FakeInputSink());
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
    public async Task Apply_ValidValues_UpdatesEngineSettings()
    {
        var cut = RenderIdle();

        var speedSelect = cut.FindComponent<MudBlazor.MudSelect<double>>();
        await cut.InvokeAsync(() => speedSelect.Instance.ValueChanged.InvokeAsync(2.5));

        var inputs = cut.FindAll(".bar-field input");
        Assert.Equal(3, inputs.Count);
        inputs[1].Change("5");
        inputs[2].Change("3");

        cut.FindAll("button").Single(b => b.TextContent.Trim() == "应用").Click();

        Assert.Equal(2.5, _scope.Settings.Speed);
        Assert.Equal(5, _scope.Settings.Jitter);
        Assert.Equal(3, _scope.Settings.RepeatCount);
    }

    [Fact]
    public void SpeedChoices_Cover_01_To_30_By_Tenths()
    {
        Assert.Equal(30, ControlBar.SpeedChoices.Length);
        Assert.Equal(0.1, ControlBar.SpeedChoices[0], 3);
        Assert.Equal(3.0, ControlBar.SpeedChoices[^1], 3);
        for (int i = 1; i < ControlBar.SpeedChoices.Length; i++)
            Assert.Equal(0.1, ControlBar.SpeedChoices[i] - ControlBar.SpeedChoices[i - 1], 3);
    }

    [Fact]
    public void Apply_InvalidNumericText_ShowsErrorToast_AndKeepsSettings()
    {
        var cut = RenderIdle();

        var inputs = cut.FindAll(".bar-field input");
        inputs[1].Change("abc");

        var snackbar = _scope.Context.Services.GetRequiredService<MudBlazor.ISnackbar>();
        cut.FindAll("button").Single(b => b.TextContent.Trim() == "应用").Click();

        Assert.Contains(snackbar.ShownSnackbars, s => s.Message?.Contains("整数") == true);
        Assert.DoesNotContain(snackbar.ShownSnackbars, s => s.Message?.Contains("参数已更新") == true);
        Assert.Equal(2, _scope.Settings.Jitter);
    }

    [Fact]
    public void Apply_FullWidthDigits_AreNormalized()
    {
        var cut = RenderIdle();

        var inputs = cut.FindAll(".bar-field input");
        inputs[1].Change("１５");
        inputs[2].Change("３");

        cut.FindAll("button").Single(b => b.TextContent.Trim() == "应用").Click();

        Assert.Equal(15, _scope.Settings.Jitter);
        Assert.Equal(3, _scope.Settings.RepeatCount);
    }

    [Fact]
    public void Apply_EmptyRepeatCount_MeansInfinite()
    {
        var cut = RenderIdle();

        var inputs = cut.FindAll(".bar-field input");
        inputs[2].Change("");

        cut.FindAll("button").Single(b => b.TextContent.Trim() == "应用").Click();

        Assert.Equal(0, _scope.Settings.RepeatCount);
    }

    [Theory]
    [InlineData("15", true, 15)]
    [InlineData(" 7 ", true, 7)]
    [InlineData("１５", true, 15)]
    [InlineData("0.5", false, 0)]
    [InlineData("abc", false, 0)]
    [InlineData("-1", false, 0)]
    [InlineData("101", false, 0)]
    [InlineData("", false, 0)]
    public void TryParseInt_NormalizesFullWidth_AndEnforcesRange(string text, bool expected, int expectedValue)
    {
        Assert.Equal(expected, ControlBar.TryParseInt(text, 0, 100, out int value));
        if (expected)
            Assert.Equal(expectedValue, value);
    }

    [Fact]
    public async Task Shutdown_Confirmed_ClosesPanel_ThenRequestsEngineShutdown()
    {
        var cut = RenderIdle();

        var panelClosedBeforeShutdown = false;
        var shutdownRequested = new TaskCompletionSource();
        _scope.Engine.ShutdownRequested += () =>
        {
            panelClosedBeforeShutdown = _scope.Context.JSInterop.Invocations.Any(i => i.Identifier == "macroTool.closePanel");
            shutdownRequested.TrySetResult();
        };

        cut.Find("button[title='关闭服务']").Click();
        var dialogYes = cut.FindAll(".mud-dialog button").Single(b => b.TextContent.Trim() == "关闭");
        dialogYes.Click();

        await shutdownRequested.Task.WaitAsync(TimeSpan.FromSeconds(3));

        Assert.True(panelClosedBeforeShutdown);
        Assert.Contains(_scope.Context.JSInterop.Invocations, i => i.Identifier == "macroTool.closePanel");
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
        Assert.DoesNotContain(_scope.Context.JSInterop.Invocations, i => i.Identifier == "macroTool.closePanel");
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
