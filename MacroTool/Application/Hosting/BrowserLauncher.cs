using System.Diagnostics;
using MacroTool.Application;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MacroTool.Application.Hosting;

public sealed class BrowserLauncher : IHostedService
{
    private readonly IOptions<MacroOptions> _options;
    private readonly IServer _server;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly ILogger<BrowserLauncher> _logger;

    public BrowserLauncher(
        IOptions<MacroOptions> options,
        IServer server,
        IHostApplicationLifetime lifetime,
        ILogger<BrowserLauncher> logger)
    {
        _options = options;
        _server = server;
        _lifetime = lifetime;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (_options.Value.Normalize().OpenBrowser)
            _lifetime.ApplicationStarted.Register(OpenBrowser);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private void OpenBrowser()
    {
        try
        {
            string? address = _server.Features.Get<IServerAddressesFeature>()?.Addresses.FirstOrDefault();
            string url = string.IsNullOrWhiteSpace(address) ? "http://localhost:5047" : address;
            url = url.Replace("0.0.0.0", "localhost").Replace("[::]", "localhost");
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            _logger.LogInformation("已打开控制面板: {Url}", url);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "自动打开浏览器失败，请手动访问控制面板地址");
        }
    }
}
