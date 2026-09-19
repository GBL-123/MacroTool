## Why

三个"兜底界面"是全项目仅存的风格断点：404 页是英文文案且没有 `h1`（`FocusOnNavigate` 实际落空），Error 页是 Blazor 模板残留（`text-danger` 在 app.css 与 MudBlazor CSS 中都不存在，等于裸样式），断线重连弹窗是英文 + 冷蓝遮罩，与暖纸主题不符。MudBlazor 迁移已归档，正是把最后这三块收口的时机。

## What Changes

- **404（`NotFound.razor`）**：套用项目现成的 `.empty-state` 版式（`MudPaper.panel` 外壳），中文文案，补 `<h1>` 让路由焦点生效；显示请求路径，取值优先 `IStatusCodeReExecuteFeature.OriginalPath`，回退 `NavigationManager`（覆盖客户端软导航与直接 GET 两条渲染路径）。保留 `Router NotFoundPage` 与 `UseStatusCodePagesWithReExecute` 两条链路不变。
- **Error（`Error.razor`）**：维持现状的 MainLayout 壳（`RouteView.DefaultLayout` 本就生效，仅补显式 `@layout` 声明与 `NotFound` 一致），删除 Development/`ASPNETCORE_ENVIRONMENT` 开发者样板，改为中文说明 + mono 请求 ID + 「返回概览」（`Href` 链接，静态即可用）；图标改用 danger 语义色。
- **断线重连（`ReconnectModal.razor` + `.razor.css`）**：仅替换中文文案与视觉（暖色遮罩、项目字体/圆角/阴影、按钮造型对齐 Mud 按钮），保持 `components-reconnect-*` 类名、元素 id 与 `.razor.js` 完全不动；失败态增加"服务可能已退出，请重新运行 MacroTool.exe"提示。
- **CSS**：`app.css` 新增 `.empty-icon.is-danger` 修饰类（约 3 行）；不删除既有规则。
- **测试**：新增 `FallbackPagesTests`，锁定 404 的 h1/路径/回页链接、Error 的请求 ID 渲染、ReconnectModal 的 id/类契约与中文文案。
- 非目标：不改任何控制行为或重连协议；不给重连弹窗加「刷新页面」按钮（`.razor.js` 重试路径在 circuit 失效时已自动 `location.reload()`）；不新增依赖；不做暗色模式。
- **BREAKING**：无（纯界面与文案，用户可见行为不变）。

## Capabilities

### New Capabilities

（无）

### Modified Capabilities

（无——纯界面/文案调整，无 spec 级行为变更。按 OpenSpec 约定在 `.openspec.yaml` 设置 `skip_specs: true` 显式跳过 delta spec，不伪造需求变更。）

## Impact

- 代码：`MacroTool.Web/Components/Pages/NotFound.razor`、`MacroTool.Web/Components/Pages/Error.razor`、`MacroTool.Web/Components/Layout/ReconnectModal.razor` 与 `ReconnectModal.razor.css`、`MacroTool.Web/wwwroot/css/app.css`（仅新增一个修饰类）。
- 测试：新增 `MacroTool.Tests/FallbackPagesTests.cs`；既有测试选择器不受影响（不删除类名）。
- 文档：`README.md` 与 `AGENTS.md` 无需改动（行为、配置、协议均未变）。
- 风险：重连弹窗样式只能在真实断线场景目检；404 路径显示需手动覆盖两条渲染路径（软导航 + 直接 GET）。
