using System.Diagnostics;
using Bunit;
using MacroTool.Web.Components.Layout;
using MacroTool.Web.Components.Pages;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace MacroTool.Tests;

public sealed class FallbackPagesTests : IDisposable
{
    private readonly UiTestScope _scope;

    public FallbackPagesTests()
    {
        _scope = new UiTestScope();
    }

    public void Dispose()
    {
        _scope.Dispose();
    }

    [Theory]
    [InlineData("/timeline", "", "/timeline")]
    [InlineData("/Timeline", "logs", "/Timeline")]
    [InlineData(null, "logs", "/logs")]
    [InlineData("", "logs?warn=1", "/logs?warn=1")]
    [InlineData("not-found", "", null)]
    [InlineData("/NOT-FOUND", "", null)]
    [InlineData(null, "", null)]
    [InlineData(null, "not-found", null)]
    public void NotFound_ResolveDisplayPath_CoversStatusReExecuteAndNavigation(
        string? originalPath, string relativePath, string? expected)
        => Assert.Equal(expected, NotFound.ResolveDisplayPath(originalPath, relativePath));

    [Fact]
    public void NotFoundPage_RendersEmptyState_WithHomeLink_AndHidesBasePath()
    {
        var cut = _scope.Context.Render<NotFound>();

        Assert.Equal("页面不存在", cut.Find("h1.empty-title").TextContent.Trim());
        Assert.Equal("/", cut.Find(".empty-actions a").GetAttribute("href"));
        Assert.DoesNotContain("地址", cut.Find(".empty-sub").TextContent);
    }

    [Fact]
    public void NotFoundPage_ShowsRequestedPath_WhenNavigationPointsElsewhere()
    {
        _scope.Context.Services.GetRequiredService<NavigationManager>().NavigateTo("http://localhost/foo/bar");

        var cut = _scope.Context.Render<NotFound>();

        Assert.Contains("/foo/bar", cut.Find(".empty-sub").TextContent);
    }

    [Fact]
    public void ErrorPage_RendersRequestId_AndHasNoTemplateLeftovers()
    {
        Activity.Current = null;
        var httpContext = new DefaultHttpContext { TraceIdentifier = "trace-12345" };

        var cut = _scope.Context.Render<Error>(parameters =>
            parameters.AddCascadingValue<HttpContext>(httpContext));

        Assert.Equal("服务出错了", cut.Find("h1.empty-title").TextContent.Trim());
        Assert.Equal("/", cut.Find(".empty-actions a").GetAttribute("href"));
        Assert.Contains("trace-12345", cut.Find(".page-sub").TextContent);
        Assert.DoesNotContain("Development", cut.Markup);
        Assert.DoesNotContain("ASPNETCORE_ENVIRONMENT", cut.Markup);
        Assert.DoesNotContain("text-danger", cut.Markup);
    }

    [Fact]
    public void ReconnectModal_KeepsContractIdsAndClasses_WithChineseCopy()
    {
        _scope.Context.AddAsset("Components/Layout/ReconnectModal.razor.js",
            "Components/Layout/ReconnectModal.razor.js");

        var cut = _scope.Context.Render<ReconnectModal>();

        Assert.NotNull(cut.Find("#components-reconnect-modal"));
        Assert.Equal("连接已中断", cut.Find(".components-reconnect-header h2").TextContent.Trim());

        var retry = cut.Find("#components-reconnect-button");
        Assert.Contains("重试", retry.TextContent);
        Assert.True(retry.ClassList.Contains("components-reconnect-failed-visible"));

        var resume = cut.Find("#components-resume-button");
        Assert.Contains("恢复", resume.TextContent);
        Assert.True(resume.ClassList.Contains("components-pause-visible"));
        Assert.True(resume.ClassList.Contains("components-resume-failed-visible"));

        Assert.NotNull(cut.Find("#components-seconds-to-next-attempt"));
        Assert.NotNull(cut.Find(".components-rejoin-loader"));
        Assert.NotNull(cut.Find(".components-reconnect-backdrop"));
        Assert.NotNull(cut.Find(".components-reconnect-status-dot"));
        Assert.Contains("正在重新连接", cut.Markup);
        Assert.Contains("重连失败", cut.Markup);
        Assert.Contains("无法重新连接", cut.Markup);
        Assert.Contains("服务端已暂停会话", cut.Markup);
        Assert.Contains("恢复会话失败", cut.Markup);
        Assert.Contains("服务可能已退出", cut.Find(".components-reconnect-hint").TextContent);
    }

    [Fact]
    public void ReconnectModalStyles_TrackFrameworkRetryingState()
    {
        var css = File.ReadAllText(Path.Combine(
            FindRepoRoot(), "MacroTool.Web", "Components", "Layout", "ReconnectModal.razor.css"));

        Assert.Contains(
            "dialog[open].components-reconnect-retrying .components-reconnect-repeated-attempt-visible",
            css);
        Assert.Contains(
            "dialog[open].components-reconnect-show:not(.components-reconnect-retrying) .components-reconnect-first-attempt-visible",
            css);
        Assert.DoesNotContain("dialog[open].components-reconnect-repeated-attempt", css);
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);

        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "MacroTool.slnx")))
            dir = dir.Parent;

        return dir?.FullName ?? throw new InvalidOperationException("Repository root not found.");
    }
}
