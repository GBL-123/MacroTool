using MacroTool.Application.Engine;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MacroTool.Web.Hosting;

public sealed class MacroHostedService : IHostedService
{
    private readonly MacroEngine _engine;
    private readonly LogBuffer _logBuffer;
    private readonly IOptions<MacroOptions> _options;
    private readonly ILogger<MacroHostedService> _logger;

    public MacroHostedService(
        MacroEngine engine,
        IHostApplicationLifetime lifetime,
        IOptions<MacroOptions> options,
        LogBuffer logBuffer,
        ILogger<MacroHostedService> logger)
    {
        _engine = engine;
        _options = options;
        _logBuffer = logBuffer;
        _logger = logger;
        engine.ShutdownRequested += lifetime.StopApplication;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        foreach (var warning in _options.Value.Normalize().Warnings)
            Log(LogLevel.Warning, warning);

        if (!OperatingSystem.IsWindows())
        {
            Log(LogLevel.Error, "宏引擎仅支持 Windows 交互式桌面会话，未启动");
            return Task.CompletedTask;
        }

        _engine.Start();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _engine.Dispose();
        return Task.CompletedTask;
    }

    private void Log(LogLevel level, string message)
    {
        _logger.Log(level, "{Message}", message);
        _logBuffer.Add(new EngineLogEntry(DateTimeOffset.Now, level, message));
    }
}
