using MacroTool.Application.Engine;

namespace MacroTool.Application.Editing;

public enum UnpairedKind
{
    None,
    Down,
    Up
}

public sealed class TimelineAction
{
    public int Index { get; set; }
    public bool IsKey { get; init; }
    public int? DownIndex { get; init; }
    public int? UpIndex { get; init; }
    public double StartT { get; init; }
    public double EndT { get; init; }
    public UnpairedKind Unpaired { get; init; }
    public int Vk { get; init; }
    public int Scan { get; init; }
    public bool Ext { get; init; }
    public int X { get; init; }
    public int Y { get; init; }

    public bool IsPaired => DownIndex is not null && UpIndex is not null;

    public bool IsUnpaired => Unpaired != UnpairedKind.None;

    public double DurationMs => IsPaired ? Math.Max(0, EndT - StartT) : 0;
}

public static class TimelineEditing
{
    public static List<TimelineAction> BuildActions(IReadOnlyList<MacroEvent> events)
    {
        var actions = new List<TimelineAction>();
        var pendingKeys = new Dictionary<int, int>();
        int pendingMouseDown = -1;

        for (int i = 0; i < events.Count; i++)
        {
            var e = events[i];
            if (!e.Up)
            {
                if (e.IsKey)
                    pendingKeys[e.Vk] = i;
                else
                    pendingMouseDown = i;
                continue;
            }

            if (!e.IsKey)
            {
                if (pendingMouseDown >= 0)
                {
                    actions.Add(Paired(events[pendingMouseDown], pendingMouseDown, e, i));
                    pendingMouseDown = -1;
                }
                else
                {
                    actions.Add(UnpairedUp(e, i));
                }
            }
            else if (pendingKeys.Remove(e.Vk, out int downIndex))
            {
                actions.Add(Paired(events[downIndex], downIndex, e, i));
            }
            else
            {
                actions.Add(UnpairedUp(e, i));
            }
        }

        foreach (var (_, downIndex) in pendingKeys)
            actions.Add(UnpairedDown(events[downIndex], downIndex));
        if (pendingMouseDown >= 0)
            actions.Add(UnpairedDown(events[pendingMouseDown], pendingMouseDown));

        actions.Sort((a, b) => a.StartT.CompareTo(b.StartT));
        for (int i = 0; i < actions.Count; i++)
            actions[i].Index = i;
        return actions;
    }

    public static bool HasInvertedPairs(IReadOnlyList<MacroEvent> events)
        => BuildActions(events).Any(a => a.Unpaired == UnpairedKind.Up);

    public static List<MacroEvent> DeleteAction(IReadOnlyList<MacroEvent> events, TimelineAction action)
    {
        var remove = new HashSet<int>();
        if (action.DownIndex is int down) remove.Add(down);
        if (action.UpIndex is int up) remove.Add(up);

        var result = new List<MacroEvent>(events.Count - remove.Count);
        for (int i = 0; i < events.Count; i++)
        {
            if (!remove.Contains(i))
                result.Add(events[i].Clone());
        }
        return result;
    }

    public static int CountTrimmedActions(IReadOnlyList<MacroEvent> events, double keepFromMs, double keepToMs)
        => BuildActions(events).Count(a => a.StartT < keepFromMs || a.StartT > keepToMs);

    public static List<MacroEvent> Trim(IReadOnlyList<MacroEvent> events, double keepFromMs, double keepToMs)
    {
        var keep = new HashSet<int>();
        foreach (var action in BuildActions(events))
        {
            if (action.StartT >= keepFromMs && action.StartT <= keepToMs)
            {
                if (action.DownIndex is int down) keep.Add(down);
                if (action.UpIndex is int up) keep.Add(up);
            }
        }

        var result = new List<MacroEvent>(keep.Count);
        for (int i = 0; i < events.Count; i++)
        {
            if (keep.Contains(i))
                result.Add(events[i].Clone());
        }
        return result;
    }

    public static List<MacroEvent> ShiftAll(IReadOnlyList<MacroEvent> events, double offsetMs)
    {
        var result = new List<MacroEvent>(events.Count);
        foreach (var e in events)
        {
            var clone = e.Clone();
            clone.T = Math.Max(0, clone.T + offsetMs);
            result.Add(clone);
        }
        return result;
    }

    public static List<MacroEvent> ShiftActions(
        IReadOnlyList<MacroEvent> events,
        IEnumerable<TimelineAction> actions,
        double offsetMs)
    {
        var indexes = new HashSet<int>();
        foreach (var action in actions)
        {
            if (action.DownIndex is int down) indexes.Add(down);
            if (action.UpIndex is int up) indexes.Add(up);
        }

        var result = new List<MacroEvent>(events.Count);
        for (int i = 0; i < events.Count; i++)
        {
            var clone = events[i].Clone();
            if (indexes.Contains(i))
                clone.T = Math.Max(0, clone.T + offsetMs);
            result.Add(clone);
        }
        return result;
    }

    public static List<MacroEvent> Scale(IReadOnlyList<MacroEvent> events, double factor)
    {
        if (!(factor > 0))
            throw new ArgumentOutOfRangeException(nameof(factor), "缩放倍率必须大于 0");

        var result = new List<MacroEvent>(events.Count);
        foreach (var e in events)
        {
            var clone = e.Clone();
            clone.T = Math.Max(0, clone.T * factor);
            result.Add(clone);
        }
        return result;
    }

    public static MacroTimeline Normalize(IEnumerable<MacroEvent> events)
    {
        var list = events
            .OrderBy(e => e.T)
            .Select(e => e.Clone())
            .ToList();
        return new MacroTimeline
        {
            Events = list,
            DurationMs = list.Count == 0 ? 0 : list[^1].T
        };
    }

    private static TimelineAction Paired(MacroEvent down, int downIndex, MacroEvent up, int upIndex) => new()
    {
        IsKey = down.IsKey,
        DownIndex = downIndex,
        UpIndex = upIndex,
        StartT = down.T,
        EndT = up.T,
        Unpaired = UnpairedKind.None,
        Vk = down.Vk,
        Scan = down.Scan,
        Ext = down.Ext,
        X = down.IsKey ? 0 : down.X,
        Y = down.IsKey ? 0 : down.Y
    };

    private static TimelineAction UnpairedDown(MacroEvent down, int downIndex) => new()
    {
        IsKey = down.IsKey,
        DownIndex = downIndex,
        UpIndex = null,
        StartT = down.T,
        EndT = down.T,
        Unpaired = UnpairedKind.Down,
        Vk = down.Vk,
        Scan = down.Scan,
        Ext = down.Ext,
        X = down.IsKey ? 0 : down.X,
        Y = down.IsKey ? 0 : down.Y
    };

    private static TimelineAction UnpairedUp(MacroEvent up, int upIndex) => new()
    {
        IsKey = up.IsKey,
        DownIndex = null,
        UpIndex = upIndex,
        StartT = up.T,
        EndT = up.T,
        Unpaired = UnpairedKind.Up,
        Vk = up.Vk,
        Scan = up.Scan,
        Ext = up.Ext,
        X = up.IsKey ? 0 : up.X,
        Y = up.IsKey ? 0 : up.Y
    };
}
