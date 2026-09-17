using Microsoft.Extensions.Logging;

namespace MacroTool.Application.Engine;

public sealed record EngineLogEntry(DateTimeOffset Timestamp, LogLevel Level, string Message);

public sealed class LogBuffer
{
    public const int DefaultCapacity = 500;

    private readonly object _gate = new();
    private readonly Queue<EngineLogEntry> _entries = new();
    private readonly int _capacity;

    public LogBuffer() : this(DefaultCapacity)
    {
    }

    public LogBuffer(int capacity)
    {
        _capacity = Math.Max(1, capacity);
    }

    public event Action<EngineLogEntry>? Added;

    public void Add(EngineLogEntry entry)
    {
        lock (_gate)
        {
            _entries.Enqueue(entry);
            while (_entries.Count > _capacity)
                _entries.Dequeue();
        }
        Added?.Invoke(entry);
    }

    public IReadOnlyList<EngineLogEntry> Snapshot()
    {
        lock (_gate)
        {
            return _entries.ToArray();
        }
    }
}
