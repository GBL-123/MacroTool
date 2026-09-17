using MacroTool.Domain;
using MacroTool.Domain.Editing;

namespace MacroTool.Tests;

public sealed class TimelineEditingTests
{
    private static MacroEvent KeyDown(double t, int vk = 65, int scan = 30)
        => new() { T = t, IsKey = true, Vk = vk, Scan = scan, Up = false };

    private static MacroEvent KeyUp(double t, int vk = 65, int scan = 30)
        => new() { T = t, IsKey = true, Vk = vk, Scan = scan, Up = true };

    private static MacroEvent ClickDown(double t, int x = 100, int y = 200)
        => new() { T = t, IsKey = false, Up = false, X = x, Y = y };

    private static MacroEvent ClickUp(double t)
        => new() { T = t, IsKey = false, Up = true };

    [Fact]
    public void BuildActions_PairsClickDownAndUp()
    {
        var actions = TimelineEditing.BuildActions([ClickDown(100), ClickUp(150)]);

        var action = Assert.Single(actions);
        Assert.True(action.IsPaired);
        Assert.False(action.IsKey);
        Assert.Equal(100, action.StartT);
        Assert.Equal(50, action.DurationMs);
        Assert.Equal(100, action.X);
        Assert.Equal(200, action.Y);
    }

    [Fact]
    public void BuildActions_PairsKeyDownAndUp()
    {
        var actions = TimelineEditing.BuildActions([KeyDown(10), KeyUp(60)]);

        var action = Assert.Single(actions);
        Assert.True(action.IsPaired);
        Assert.True(action.IsKey);
        Assert.Equal(65, action.Vk);
        Assert.Equal(60, action.EndT);
    }

    [Fact]
    public void BuildActions_InterleavedInput_PairsCorrectlyAndSortsByStart()
    {
        var events = new[]
        {
            KeyDown(10),
            ClickDown(20),
            KeyUp(30),
            ClickUp(40)
        };

        var actions = TimelineEditing.BuildActions(events);

        Assert.Equal(2, actions.Count);
        Assert.True(actions[0].IsKey);
        Assert.Equal(10, actions[0].StartT);
        Assert.False(actions[1].IsKey);
        Assert.Equal(20, actions[1].StartT);
        Assert.All(actions, a => Assert.True(a.IsPaired));
    }

    [Fact]
    public void BuildActions_UnpairedDown_IsMarked()
    {
        var actions = TimelineEditing.BuildActions([KeyDown(10)]);

        var action = Assert.Single(actions);
        Assert.True(action.IsUnpaired);
        Assert.Equal(UnpairedKind.Down, action.Unpaired);
        Assert.Null(action.UpIndex);
    }

    [Fact]
    public void BuildActions_LoneUp_IsMarkedAsUnpairedUp()
    {
        var actions = TimelineEditing.BuildActions([KeyUp(10)]);

        var action = Assert.Single(actions);
        Assert.Equal(UnpairedKind.Up, action.Unpaired);
        Assert.Null(action.DownIndex);
    }

    [Fact]
    public void DeleteAction_PairedAction_RemovesBothEvents()
    {
        var events = new[] { ClickDown(100), ClickUp(150) }.ToList();
        var action = TimelineEditing.BuildActions(events)[0];

        var result = TimelineEditing.DeleteAction(events, action);

        Assert.Empty(result);
    }

    [Fact]
    public void DeleteAction_UnpairedAction_RemovesSingleEvent()
    {
        var events = new[] { KeyDown(10), KeyDown(20, vk: 66) }.ToList();
        var actions = TimelineEditing.BuildActions(events);

        var result = TimelineEditing.DeleteAction(events, actions[0]);

        var remaining = Assert.Single(result);
        Assert.Equal(66, remaining.Vk);
    }

    [Fact]
    public void Trim_KeepsWholeActionsWhoseStartIsInRange()
    {
        var events = new[]
        {
            ClickDown(100),
            ClickUp(1500),
            ClickDown(3000),
            ClickUp(3100)
        }.ToList();

        var result = TimelineEditing.Trim(events, keepFromMs: 50, keepToMs: 200);

        Assert.Equal(2, result.Count);
        Assert.Equal(100, result[0].T);
        Assert.Equal(1500, result[1].T);
    }

    [Fact]
    public void CountTrimmedActions_CountsOutOfRangeActions()
    {
        var events = new[]
        {
            ClickDown(100),
            ClickUp(150),
            ClickDown(3000),
            ClickUp(3100)
        };

        Assert.Equal(1, TimelineEditing.CountTrimmedActions(events, 50, 500));
        Assert.Equal(2, TimelineEditing.CountTrimmedActions(events, 200, 500));
        Assert.Equal(0, TimelineEditing.CountTrimmedActions(events, 0, 5000));
    }

    [Fact]
    public void ShiftAll_ClampsNegativeTimesToZero()
    {
        var events = new[] { KeyDown(100), KeyUp(250) };

        var result = TimelineEditing.ShiftAll(events, -500);

        Assert.Equal(0, result[0].T);
        Assert.Equal(0, result[1].T);
    }

    [Fact]
    public void ShiftAll_PreservesRelativeDeltas()
    {
        var events = new[] { KeyDown(100), KeyUp(250) };

        var result = TimelineEditing.ShiftAll(events, 25);

        Assert.Equal(125, result[0].T);
        Assert.Equal(275, result[1].T);
    }

    [Fact]
    public void ShiftActions_OnlyShiftsSelectedAction()
    {
        var events = new[]
        {
            ClickDown(100),
            ClickUp(150),
            ClickDown(500),
            ClickUp(550)
        }.ToList();
        var actions = TimelineEditing.BuildActions(events);

        var result = TimelineEditing.ShiftActions(events, [actions[1]], -50);

        Assert.Equal(100, result[0].T);
        Assert.Equal(150, result[1].T);
        Assert.Equal(450, result[2].T);
        Assert.Equal(500, result[3].T);
    }

    [Fact]
    public void Scale_MultipliesAllTimes()
    {
        var events = new[] { KeyDown(100), KeyUp(300) };

        var result = TimelineEditing.Scale(events, 0.5);

        Assert.Equal(50, result[0].T);
        Assert.Equal(150, result[1].T);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Scale_InvalidFactor_Throws(double factor)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            TimelineEditing.Scale([KeyDown(100)], factor));
    }

    [Fact]
    public void Normalize_SortsAscendingAndRecomputesDuration()
    {
        var timeline = TimelineEditing.Normalize([KeyUp(300), KeyDown(100)]);

        Assert.Equal(2, timeline.Events.Count);
        Assert.Equal(100, timeline.Events[0].T);
        Assert.Equal(300, timeline.Events[1].T);
        Assert.Equal(300, timeline.DurationMs);
    }

    [Fact]
    public void Normalize_Empty_ProducesZeroDuration()
    {
        Assert.Equal(0, TimelineEditing.Normalize([]).DurationMs);
    }

    [Fact]
    public void HasInvertedPairs_DetectsUpBeforeDown()
    {
        var invertedTimeline = TimelineEditing.Normalize([ClickUp(600), ClickDown(1000)]);

        Assert.True(TimelineEditing.HasInvertedPairs(invertedTimeline.Events));
        Assert.Equal(UnpairedKind.Up, TimelineEditing.BuildActions(invertedTimeline.Events)[0].Unpaired);
    }

    [Fact]
    public void HasInvertedPairs_NormalTimeline_IsFalse()
    {
        var events = new[] { ClickDown(100), ClickUp(150) };

        Assert.False(TimelineEditing.HasInvertedPairs(events));
    }

    [Fact]
    public void KeyNames_FormatCommonVirtualKeys()
    {
        Assert.Equal("A", KeyNames.Format(0x41));
        Assert.Equal("0", KeyNames.Format(0x30));
        Assert.Equal("F1", KeyNames.Format(0x70));
        Assert.Equal("F10", KeyNames.Format(0x79));
        Assert.Equal("Space", KeyNames.Format(0x20));
        Assert.Equal("VK 0x99", KeyNames.Format(0x99));
    }

    [Theory]
    [InlineData(0x41, "A")]
    [InlineData(0x5A, "Z")]
    [InlineData(0x30, "0")]
    [InlineData(0x39, "9")]
    [InlineData(0x70, "F1")]
    [InlineData(0x7B, "F12")]
    [InlineData(0x20, "Space")]
    [InlineData(0x0D, "Enter")]
    [InlineData(0x1B, "Esc")]
    [InlineData(0x09, "Tab")]
    [InlineData(0x08, "Backspace")]
    [InlineData(0x10, "Shift")]
    [InlineData(0x11, "Ctrl")]
    [InlineData(0x12, "Alt")]
    [InlineData(0x25, "Left")]
    [InlineData(0x26, "Up")]
    [InlineData(0x27, "Right")]
    [InlineData(0x28, "Down")]
    [InlineData(0x2E, "Delete")]
    [InlineData(0x24, "Home")]
    [InlineData(0x23, "End")]
    [InlineData(0x21, "PageUp")]
    [InlineData(0x22, "PageDown")]
    [InlineData(0x14, "CapsLock")]
    [InlineData(0x5B, "Win")]
    [InlineData(0x0C, "Clear")]
    [InlineData(0x0A, "LineFeed")]
    public void KeyNames_Format_CoversMappedKeys(int vk, string expected)
    {
        Assert.Equal(expected, KeyNames.Format(vk));
    }

    [Fact]
    public void MacroTimeline_Clone_RoundTrips()
    {
        var timeline = new MacroTimeline
        {
            DurationMs = 100,
            Events = [new MacroEvent { T = 10, IsKey = true, Vk = 65, Scan = 30, Up = false }]
        };

        var clone = timeline.Clone();
        clone.Events[0].T = 999;

        Assert.Equal(10, timeline.Events[0].T);
        Assert.Equal(999, clone.Events[0].T);
        Assert.Single(timeline.Events);
    }

    [Fact]
    public void MacroTimeline_FromEvents_ComputesDuration()
    {
        var timeline = MacroTimeline.FromEvents([new MacroEvent { T = 1 }, new MacroEvent { T = 42 }]);

        Assert.Equal(42, timeline.DurationMs);
        Assert.Equal(2, timeline.Events.Count);
    }
}
