using MacroTool.Application.Engine;

namespace MacroTool.Tests;

public sealed class EngineStateMachineTests
{
    [Fact]
    public void InitialState_IsIdle()
    {
        Assert.Equal(ToolState.Idle, new EngineStateMachine().State);
    }

    [Fact]
    public void BeginRecording_FromIdle_Succeeds()
    {
        var sm = new EngineStateMachine();

        Assert.True(sm.TryBeginRecording(out var reason));
        Assert.Null(reason);
        Assert.Equal(ToolState.Recording, sm.State);
    }

    [Fact]
    public void BeginRecording_WhilePlaying_IsRejected()
    {
        var sm = new EngineStateMachine();
        Assert.True(sm.TryBeginPlayback(hasPlayableTimeline: true, out _));

        Assert.False(sm.TryBeginRecording(out var reason));
        Assert.NotNull(reason);
        Assert.Equal(ToolState.Playing, sm.State);
    }

    [Fact]
    public void BeginPlayback_WhileRecording_IsRejected()
    {
        var sm = new EngineStateMachine();
        Assert.True(sm.TryBeginRecording(out _));

        Assert.False(sm.TryBeginPlayback(hasPlayableTimeline: true, out var reason));
        Assert.NotNull(reason);
        Assert.Equal(ToolState.Recording, sm.State);
    }

    [Fact]
    public void BeginPlayback_WithoutTimeline_IsRejected()
    {
        var sm = new EngineStateMachine();

        Assert.False(sm.TryBeginPlayback(hasPlayableTimeline: false, out var reason));
        Assert.NotNull(reason);
        Assert.Equal(ToolState.Idle, sm.State);
    }

    [Fact]
    public void BeginPlayback_WithTimeline_Succeeds()
    {
        var sm = new EngineStateMachine();

        Assert.True(sm.TryBeginPlayback(hasPlayableTimeline: true, out var reason));
        Assert.Null(reason);
        Assert.Equal(ToolState.Playing, sm.State);
    }

    [Fact]
    public void EndRecording_ReturnsToIdle()
    {
        var sm = new EngineStateMachine();
        Assert.True(sm.TryBeginRecording(out _));

        Assert.True(sm.TryEndRecording(out var reason));
        Assert.Null(reason);
        Assert.Equal(ToolState.Idle, sm.State);
    }

    [Fact]
    public void EndPlayback_ReturnsToIdle()
    {
        var sm = new EngineStateMachine();
        Assert.True(sm.TryBeginPlayback(hasPlayableTimeline: true, out _));

        Assert.True(sm.TryEndPlayback(out var reason));
        Assert.Null(reason);
        Assert.Equal(ToolState.Idle, sm.State);
    }

    [Fact]
    public void EndRecording_WhenNotRecording_IsRejected()
    {
        var sm = new EngineStateMachine();

        Assert.False(sm.TryEndRecording(out var reason));
        Assert.NotNull(reason);
        Assert.Equal(ToolState.Idle, sm.State);
    }

    [Fact]
    public void DoubleStartRecording_IsRejected()
    {
        var sm = new EngineStateMachine();
        Assert.True(sm.TryBeginRecording(out _));

        Assert.False(sm.TryBeginRecording(out _));
        Assert.Equal(ToolState.Recording, sm.State);
    }
}
