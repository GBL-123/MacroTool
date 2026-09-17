using MacroTool.Application;
using MacroTool.Application.Engine;
using MacroTool.Application.Hosting;
using MacroTool.Application.Hotkeys;
using MacroTool.Application.Interop;
using MacroTool.Application.Storage;
using MacroTool.Components;
using Microsoft.Extensions.Options;
using MudBlazor;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory
});

Native.SetProcessDPIAware();

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
builder.Services.AddSingleton(sp =>
{
    var normalized = sp.GetRequiredService<IOptions<MacroOptions>>().Value.Normalize();
    return new TimelineStore(TimelineStore.ResolvePath(normalized.TimelinePath));
});
builder.Services.AddSingleton<HotkeyHost>();
builder.Services.AddSingleton<MacroEngine>();
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
