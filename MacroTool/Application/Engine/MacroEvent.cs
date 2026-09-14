namespace MacroTool.Application.Engine;

public sealed class MacroEvent
{
    public double T { get; set; }
    public bool IsKey { get; set; }
    public int Vk { get; set; }
    public int Scan { get; set; }
    public bool Ext { get; set; }
    public bool Up { get; set; }
    public int X { get; set; }
    public int Y { get; set; }

    public MacroEvent Clone() => new()
    {
        T = T,
        IsKey = IsKey,
        Vk = Vk,
        Scan = Scan,
        Ext = Ext,
        Up = Up,
        X = X,
        Y = Y
    };
}

public sealed class MacroTimeline
{
    public List<MacroEvent> Events { get; set; } = [];
    public double DurationMs { get; set; }

    public static MacroTimeline FromEvents(IEnumerable<MacroEvent> events)
    {
        var list = events.Select(e => e.Clone()).ToList();
        return new MacroTimeline
        {
            Events = list,
            DurationMs = list.Count == 0 ? 0 : list.Max(e => e.T)
        };
    }

    public MacroTimeline Clone() => FromEvents(Events);
}
