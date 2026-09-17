using MacroTool.Application.Engine;
using MacroTool.Application.Ports;
using MacroTool.Infrastructure.Hotkeys;
using MacroTool.Infrastructure.Interop;
using MacroTool.Infrastructure.Storage;
using MacroTool.Web.Components;
using MacroTool.Web.Hosting;
using Microsoft.Extensions.Options;
using MudBlazor;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory
});

WindowsEnvironment.EnableDpiAwareness();

builder.Services.AddMudServices(config =>
{
    config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.TopRight;
});

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.Configure<MacroOptions>(builder.Configuration.GetSection(MacroOptions.SectionName));
builder.Services.AddSingleton<LogBuffer>();
builder.Services.AddSingleton(sp =>
{
    var normalized = sp.GetRequiredService<IOptions<MacroOptions>>().Value.Normalize();
    return new EngineSettings(normalized.Speed, normalized.Jitter, normalized.RepeatCount);
});
builder.Services.AddSingleton<IGlobalHotkeys, HotkeyHost>();
builder.Services.AddSingleton<IInputSink, Win32InputSink>();
builder.Services.AddSingleton<ITimelineRepository>(sp =>
{
    var normalized = sp.GetRequiredService<IOptions<MacroOptions>>().Value.Normalize();
    return new TimelineStore(TimelineStore.ResolvePath(normalized.TimelinePath));
});
builder.Services.AddSingleton(sp => new MacroEngine(
    sp.GetRequiredService<IGlobalHotkeys>(),
    sp.GetRequiredService<ITimelineRepository>(),
    sp.GetRequiredService<EngineSettings>(),
    sp.GetRequiredService<LogBuffer>(),
    sp.GetRequiredService<ILogger<MacroEngine>>(),
    sp.GetRequiredService<IInputSink>(),
    () => new LowLevelHookCapture(),
    isElevated: WindowsEnvironment.IsElevated));
builder.Services.AddHostedService<MacroHostedService>();
builder.Services.AddHostedService<BrowserLauncher>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
