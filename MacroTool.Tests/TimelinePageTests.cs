using Bunit;
using MacroTool.Application.Engine;
using MacroTool.Web.Components.Pages;
using MacroTool.Domain;
using Microsoft.Extensions.DependencyInjection;

namespace MacroTool.Tests;

public sealed class TimelinePageTests : IDisposable
{
    private readonly UiTestScope _scope;

    public TimelinePageTests()
    {
        _scope = new UiTestScope();
        _scope.Engine.ReplaceTimeline(UiTestScope.SampleTimeline());
    }

    public void Dispose()
    {
        _scope.Dispose();
    }

    private IRenderedComponent<Timeline> Render()
        => _scope.Context.Render<Timeline>();

    [Fact]
    public void Page_ShowsSummary_AndGridRows()
    {
        var cut = Render();

        var head = cut.Find(".page-head").TextContent;
        Assert.Contains("时间线编辑", head);
        Assert.Contains("事件 5", head);
        Assert.Contains("动作 3", head);
        Assert.Contains("500.000 ms", head);

        Assert.Equal(3, cut.FindAll(".mud-table-body .mud-table-row").Count);
    }

    [Fact]
    public void DeleteAction_MarksDirty_AndUpdatesCounts()
    {
        var cut = Render();

        cut.FindAll("button[title='删除该动作']")[0].Click();

        Assert.Contains("事件 3", cut.Find(".page-head").TextContent);
        Assert.Contains("动作 2", cut.Find(".page-head").TextContent);
        Assert.NotNull(cut.Find(".badge.is-warn"));
    }

    [Fact]
    public void ShiftAll_UpdatesDuration()
    {
        var cut = Render();

        cut.FindAll("button").Single(b => b.TextContent.Trim() == "平移").Click();

        Assert.Contains("600.000 ms", cut.Find(".page-head").TextContent);
        Assert.NotNull(cut.Find(".badge.is-warn"));
    }

    [Fact]
    public void Save_WritesFile_ClearsDirty_AndAppliesToEngine()
    {
        var cut = Render();
        cut.FindAll("button[title='删除该动作']")[0].Click();
        Assert.NotNull(cut.Find(".badge.is-warn"));

        cut.FindAll("button").Single(b => b.TextContent.Trim() == "保存").Click();

        Assert.True(File.Exists(_scope.Store.FilePath));
        Assert.False(cut.FindAll(".badge.is-warn").Any(), "保存后不应存在未保存标记");
        Assert.Equal(3, _scope.Engine.CurrentTimeline!.Events.Count);
    }

    [Fact]
    public void Save_DoesNotWarnAboutUnsavedChanges()
    {
        var cut = Render();
        cut.FindAll("button[title='删除该动作']")[0].Click();

        var snackbar = _scope.Context.Services.GetRequiredService<MudBlazor.ISnackbar>();
        cut.FindAll("button").Single(b => b.TextContent.Trim() == "保存").Click();

        Assert.Contains(snackbar.ShownSnackbars, s => s.Message?.Contains("已保存并应用到引擎") == true);
        Assert.DoesNotContain(snackbar.ShownSnackbars, s => s.Message?.Contains("未保存") == true);
    }

    /// <summary>工具栏里有 4 个数值输入；MudSwitch 的 checkbox 需要排除。</summary>
    private static System.Collections.Generic.List<AngleSharp.Dom.IElement> NumberInputs(IEnumerable<AngleSharp.Dom.IElement> all)
        => all.Where(i => !string.Equals(i.GetAttribute("type"), "checkbox", StringComparison.OrdinalIgnoreCase))
              .ToList();

    [Fact]
    public void Trim_WithConfirmDialog_RemovesOutOfRangeActions()
    {
        var cut = _scope.Context.Render<DialogHost<Timeline>>();

        var inputs = NumberInputs(cut.FindAll(".tool-groups input"));
        Assert.Equal(4, inputs.Count);
        inputs[0].Change("0");
        inputs[1].Change("200");

        cut.FindAll("button").Single(b => b.TextContent.Trim() == "修剪").Click();

        // 确认对话框
        var dialogYes = cut.FindAll(".mud-dialog button").Single(b => b.TextContent.Trim() == "修剪");
        dialogYes.Click();

        Assert.Contains("事件 2", cut.Find(".page-head").TextContent);
        Assert.Contains("动作 1", cut.Find(".page-head").TextContent);
        Assert.NotNull(cut.Find(".badge.is-warn"));
    }

    [Fact]
    public void Trim_Cancelled_DoesNotChangeTimeline()
    {
        var cut = _scope.Context.Render<DialogHost<Timeline>>();

        var inputs = NumberInputs(cut.FindAll(".tool-groups input"));
        Assert.Equal(4, inputs.Count);
        inputs[0].Change("0");
        inputs[1].Change("200");

        cut.FindAll("button").Single(b => b.TextContent.Trim() == "修剪").Click();
        var dialogCancel = cut.FindAll(".mud-dialog button").Single(b => b.TextContent.Trim() == "取消");
        dialogCancel.Click();

        Assert.Contains("事件 5", cut.Find(".page-head").TextContent);
        Assert.False(cut.FindAll(".badge.is-warn").Any(), "不应出现未保存标记");
    }

    [Fact]
    public void Scale_WithConfirmDialog_HalvesTimes()
    {
        var cut = _scope.Context.Render<DialogHost<Timeline>>();

        var inputs = NumberInputs(cut.FindAll(".tool-groups input"));
        inputs[3].Change("0.5");

        cut.FindAll("button").Single(b => b.TextContent.Trim() == "缩放").Click();
        Assert.Equal("时间缩放", cut.Find(".mud-dialog-title").TextContent.Trim());
        Assert.Contains("把全部事件时间缩放为", cut.Find(".mud-dialog .mud-dialog-content").TextContent);
        var dialogYes = cut.FindAll(".mud-dialog button").Single(b => b.TextContent.Trim() == "缩放");
        dialogYes.Click();

        Assert.Contains("250.000 ms", cut.Find(".page-head").TextContent);
        Assert.NotNull(cut.Find(".badge.is-warn"));
    }

    [Fact]
    public void DeleteSelected_RemovesCheckedActions()
    {
        var cut = Render();

        var rowCheckbox = cut.Find(".mud-table-body input[type='checkbox']");
        rowCheckbox.Change(true);

        var deleteSelected = cut.FindAll("button").Single(b => b.TextContent.Trim() == "删除选中");
        Assert.False(deleteSelected.HasAttribute("disabled"));
        deleteSelected.Click();

        Assert.Contains("事件 3", cut.Find(".page-head").TextContent);
        Assert.NotNull(cut.Find(".badge.is-warn"));
    }

    [Fact]
    public void EngineTimelineReplaced_WhenNotDirty_ReloadsIntoEditor()
    {
        var cut = Render();

        var replacement = new MacroTimeline
        {
            DurationMs = 40,
            Events = [UiTestScope.Key(10, up: false), UiTestScope.Key(30, up: true)]
        };
        _scope.Engine.ReplaceTimeline(replacement);

        cut.WaitForState(() => cut.Find(".page-head").TextContent.Contains("事件 2"));
        Assert.Contains("动作 1", cut.Find(".page-head").TextContent);
        Assert.False(cut.FindAll(".badge.is-warn").Any(), "同步载入不应标记未保存");
    }

    [Fact]
    public void InvertedTimeline_ShowsWarningNotice()
    {
        var inverted = new MacroTimeline
        {
            DurationMs = 1000,
            Events =
            [
                new MacroEvent { T = 600, IsKey = false, Up = true },
                new MacroEvent { T = 1000, IsKey = false, Up = false }
            ]
        };
        _scope.Engine.ReplaceTimeline(inverted);

        var cut = Render();

        Assert.Contains("顺序颠倒", cut.Find(".notice.is-warn").TextContent);
    }
}
