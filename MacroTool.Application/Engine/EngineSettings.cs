namespace MacroTool.Application.Engine;

public sealed class EngineSettings
{
    private long _speedBits;
    private int _jitter;
    private int _repeatCount;

    public EngineSettings(double speed, int jitter)
        : this(speed, jitter, 0)
    {
    }

    public EngineSettings(double speed, int jitter, int repeatCount)
    {
        _speedBits = BitConverter.DoubleToInt64Bits(speed);
        _jitter = jitter;
        _repeatCount = repeatCount;
    }

    public double Speed
    {
        get => BitConverter.Int64BitsToDouble(Interlocked.Read(ref _speedBits));
        set => Interlocked.Exchange(ref _speedBits, BitConverter.DoubleToInt64Bits(value));
    }

    public int Jitter
    {
        get => Volatile.Read(ref _jitter);
        set => Volatile.Write(ref _jitter, value);
    }

    /// <summary>回放次数上限，0 = 无限循环。</summary>
    public int RepeatCount
    {
        get => Volatile.Read(ref _repeatCount);
        set => Volatile.Write(ref _repeatCount, value);
    }
}
