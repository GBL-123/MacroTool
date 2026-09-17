using MacroTool.Domain;

namespace MacroTool.Application.Ports;

public sealed record TimelineLoadResult(MacroTimeline? Timeline, string? Error)
{
    public bool Success => Error is null;
}

public sealed record TimelineSaveResult(bool Success, string? Error);

public interface ITimelineRepository
{
    string FilePath { get; }

    bool Exists { get; }

    TimelineLoadResult Load();

    TimelineSaveResult Save(MacroTimeline timeline, double speedHint);
}
