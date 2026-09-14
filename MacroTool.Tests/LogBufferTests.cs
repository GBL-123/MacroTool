using MacroTool.Application.Engine;
using Microsoft.Extensions.Logging;

namespace MacroTool.Tests;

public sealed class LogBufferTests
{
    [Fact]
    public void Add_TrimsToCapacity()
    {
        var buffer = new LogBuffer(3);

        for (var i = 0; i < 10; i++)
            buffer.Add(new EngineLogEntry(DateTimeOffset.Now, LogLevel.Information, $"m{i}"));

        var entries = buffer.Snapshot();
        Assert.Equal(3, entries.Count);
        Assert.Equal("m7", entries[0].Message);
        Assert.Equal("m9", entries[2].Message);
    }

    [Fact]
    public void Add_RaisesAddedEvent()
    {
        var buffer = new LogBuffer(10);
        EngineLogEntry? seen = null;
        buffer.Added += e => seen = e;

        var entry = new EngineLogEntry(DateTimeOffset.Now, LogLevel.Warning, "hello");
        buffer.Add(entry);

        Assert.Same(entry, seen);
    }

    [Fact]
    public void Snapshot_Empty_ReturnsEmpty()
    {
        Assert.Empty(new LogBuffer().Snapshot());
    }
}
