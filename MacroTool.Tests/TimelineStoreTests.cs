using MacroTool.Application.Engine;
using MacroTool.Application.Storage;

namespace MacroTool.Tests;

public sealed class TimelineStoreTests : IDisposable
{
    private readonly string _dir;
    private readonly string _file;

    public TimelineStoreTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "MacroToolTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
        _file = Path.Combine(_dir, "timeline.json");
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_dir, recursive: true);
        }
        catch
        {
        }
    }

    private static MacroTimeline SampleTimeline() => new()
    {
        DurationMs = 1000,
        Events =
        [
            new MacroEvent { T = 10, IsKey = true, Vk = 65, Scan = 30, Up = false },
            new MacroEvent { T = 50, IsKey = true, Vk = 65, Scan = 30, Up = true }
        ]
    };

    [Fact]
    public void Load_MissingFile_ReturnsNoTimelineAndNoError()
    {
        var store = new TimelineStore(_file);

        var result = store.Load();

        Assert.True(result.Success);
        Assert.Null(result.Timeline);
        Assert.False(store.Exists);
    }

    [Fact]
    public void SaveThenLoad_RoundTripsEvents()
    {
        var store = new TimelineStore(_file);

        var save = store.Save(SampleTimeline(), speedHint: 2.0);
        var load = store.Load();

        Assert.True(save.Success, save.Error);
        Assert.True(load.Success, load.Error);
        Assert.NotNull(load.Timeline);
        Assert.Equal(2, load.Timeline!.Events.Count);
        Assert.Equal(10, load.Timeline.Events[0].T);
        Assert.True(load.Timeline.Events[0].IsKey);
        Assert.Equal(65, load.Timeline.Events[0].Vk);
        Assert.True(load.Timeline.Events[1].Up);
        Assert.Equal(1000, load.Timeline.DurationMs);
    }

    [Fact]
    public void Save_WhenFileExists_WritesBackupOfPreviousContent()
    {
        var store = new TimelineStore(_file);
        Assert.True(store.Save(SampleTimeline(), 1.0).Success);

        var replacement = new MacroTimeline
        {
            DurationMs = 5,
            Events = [new MacroEvent { T = 5, IsKey = false, Up = false, X = 9, Y = 9 }]
        };
        Assert.True(store.Save(replacement, 1.0).Success);

        Assert.True(File.Exists(store.BackupPath));
        var backup = new TimelineStore(store.BackupPath).Load();
        Assert.NotNull(backup.Timeline);
        Assert.Equal(2, backup.Timeline!.Events.Count);

        var current = store.Load();
        Assert.Single(current.Timeline!.Events);
    }

    [Fact]
    public void Save_NewFile_DoesNotCreateBackup()
    {
        var store = new TimelineStore(_file);

        Assert.True(store.Save(SampleTimeline(), 1.0).Success);

        Assert.False(File.Exists(store.BackupPath));
    }

    [Fact]
    public void Save_DoesNotLeaveTempFile()
    {
        var store = new TimelineStore(_file);

        Assert.True(store.Save(SampleTimeline(), 1.0).Success);

        Assert.False(File.Exists(_file + ".tmp"));
    }

    [Fact]
    public void Load_CorruptedFile_ReturnsError()
    {
        File.WriteAllText(_file, "{ this is not valid json");

        var result = new TimelineStore(_file).Load();

        Assert.False(result.Success);
        Assert.NotNull(result.Error);
        Assert.Null(result.Timeline);
    }

    [Fact]
    public void ResolvePath_Whitespace_ReturnsDefault()
    {
        Assert.Equal(TimelineStore.DefaultPath, TimelineStore.ResolvePath("  "));
        Assert.Equal(TimelineStore.DefaultPath, TimelineStore.ResolvePath(null));
    }

    [Fact]
    public void ResolvePath_Relative_IsResolvedAgainstBaseDirectory()
    {
        var resolved = TimelineStore.ResolvePath("data\\my.json");

        Assert.True(Path.IsPathRooted(resolved));
        Assert.EndsWith(Path.Combine("data", "my.json"), resolved, StringComparison.OrdinalIgnoreCase);
    }
}
