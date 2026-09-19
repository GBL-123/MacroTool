## Context

动机见 `proposal.md - Why`。探索阶段已核实的状态与约束：

- 404 有两条渲染路径：`Router NotFoundPage`（客户端软导航）与 `UseStatusCodePagesWithReExecute("/not-found")`（直接 GET）。重执行期间 `NavigationManager` 指向 `/not-found`，原始路径在 `IStatusCodeReExecuteFeature.OriginalPath`；水合后 circuit 的 `NavigationManager.Uri` 恢复为浏览器地址。
- `Error.razor` 没有 `@layout` 指令，但 `Routes.razor:3` 的 `RouteView DefaultLayout="typeof(Layout.MainLayout)"` 已让它套着 MainLayout（顶栏、主题、ControlBar 均在）。它仅在 `!IsDevelopment` 时可达（`Program.cs:56-59`），正文残留 `text-danger`（app.css 与 MudBlazor.min.css 均未定义该类）与 Development 样板。
- `ReconnectModal` 是 .NET 10 模板衍生的自制 `<dialog>`（含 paused/resume 态），硬契约 = `components-reconnect-*` 类名 + 元素 id + `ReconnectModal.razor.js`（`Blazor.reconnect()`/`resumeCircuit()`）；scoped CSS 经 `MacroTool.styles.css` 打包。迁移 change 的 D7 已定其保持自制。
- `MudThemeProvider` 生成的 `--mud-palette-*` 以 `:root` 作用域注入，全局可见；app.css 的 `:root` token 同样全局。`.empty-state` 系列在迁移 CSS 清理后保留（app.css:755-794），Timeline 提供现成结构：`MudPaper.panel > .empty-state > .empty-icon/.empty-title/.empty-sub/.empty-actions`。
- PageTitle 约定：`页面名 - MacroTool`。
- 迁移 change 已归档，但其实现改动尚未提交；本 change 的改动建议在迁移提交后单独成 commit。

## Goals / Non-Goals

**Goals:**

- 三个兜底界面在文案语言、字体、色彩、圆角、阴影、交互语汇上与项目暖纸主题一致。
- 零行为变更：404 两条链路、Error 的异常处理路径、重连协议与 JS 全部不动。
- 用少量测试把可见文案与重连 DOM 契约锁住。

**Non-Goals:**

- 不迁移 `ReconnectModal` 到 Mud 组件（静态 SSR + JS 直接绑 id；迁移 D7 已定）。
- 不给重连弹窗加「刷新页面」按钮（JS 在 circuit 失效时已自动 `location.reload()`）。
- 不显示 Error 的原始请求路径；不给 404 做重定向或自动跳首页。
- 不引入暗色模式、新依赖或新 spec 级行为。

## Decisions

### D1. 404 版式：复用 `.empty-state`

`MudPaper Elevation="0" Class="panel"` 包 `.empty-state`，`.empty-icon` 内放 `404`（display 字体、琥珀底）。备选：`.notice`（视觉过轻，不像终点页）、裸 `MudText`（就是现状）。空态是项目既有的"无内容"语汇，与 Timeline 一致。

### D2. 404 路径取值：OriginalPath 优先，NavigationManager 回退

`HttpContext?.Features.Get<IStatusCodeReExecuteFeature>()?.OriginalPath` 有值时用它；否则用 `NavigationManager.ToBaseRelativePath(Uri)`；结果为空或是 `not-found` 时整行不显示。备选：只用 NavigationManager（重执行场景显示 `/not-found`，误导）；不显示路径（丢失"我访问了什么"信息）。为可测性，取值逻辑抽成 internal 纯函数（输入 `string? originalPath` 与相对 URL，输出显示值），bUnit 不便模拟 feature 时直接测函数。

### D3. 404 标题用 `<h1>`

`<h1 class="empty-title">页面不存在</h1>`。`Routes.razor:4` 的 `<FocusOnNavigate Selector="h1">` 目前在该页落空，补上即修复。

### D4. Error 壳：维持 MainLayout，补显式 `@layout`

与 NotFound 一致（同时让 bUnit 直接渲染 `Error` 时也带壳与主题）。备选：独立极简壳——否决，现状本就套壳，改独立壳是从一致性回退且多一份维护。

### D5. Error 正文：空态 + danger 语义色 + 静态链接

`.empty-state` + `Icons.Material.Filled.ErrorOutline`；请求 ID 用 mono（无则整行不显示）；主动作 `MudButton Href="/"`（不依赖 circuit，刚渲染即可点）。删除 Development/`ASPNETCORE_ENVIRONMENT` 样板与 `text-danger`；保留 `HttpContext` 级联与 `Activity.Current` 取值逻辑。

### D6. `.empty-icon.is-danger` 修饰类

app.css 在 `.empty-icon` 附近新增约 3 行（背景 `--danger-soft`、颜色 `--danger-text`）。备选：内联 style（散落、难复用）、新组件（过度设计）。404 保持默认琥珀（找不到不是故障）。

### D7. ReconnectModal：只换文案与 CSS，契约零改动

保留全部 id/类/JS。视觉：遮罩改暖色 `rgba(30,27,22,.45)` + 极淡琥珀 radial（呼应 `body::before` 环境光）；卡片用 `--surface` / `--border` / `var(--radius)` / `var(--shadow-2)`；标题用 `--font-display`；状态点琥珀且复用项目 pulse 动效语言；按钮对齐 Mud 按钮（约 10px 圆角、primary 黑填充、次要动作用 `--surface-2`/`--border-strong` 的 neutral 造型），焦点环用 `--accent-line`。

### D8. 重连失败态提示行

在失败/恢复失败态显示：`服务可能已退出——请重新运行 MacroTool.exe，再点「重试」。` 用新类 `components-reconnect-hint`（`--text-faint` 小字），与 `components-reconnect-failed-visible` 等既有可见性类组合，不改 JS。

### D9. 文案定稿

**404**（PageTitle `页面不存在 - MacroTool`）：

| 位置 | 文案 |
|---|---|
| h1 | 页面不存在 |
| 副文 | 地址 `/foo/bar` 没有对应页面，可能已被移动或删除。（路径缺失时去掉"地址 …"部分） |
| 按钮 | 返回概览 |

**Error**（PageTitle `出错了 - MacroTool`）：

| 位置 | 文案 |
|---|---|
| h1 | 服务出错了 |
| 副文 | 处理请求时发生了未预期的错误。详细信息已输出到服务端控制台。 |
| 元信息 | 请求 ID `<id>`（mono，无则整行不显示） |
| 按钮 | 返回概览 |

**ReconnectModal**：

| 现英文 | 定稿 |
|---|---|
| Connection Interrupted | 连接已中断 |
| Your current session is still open… | 会话仍在保留，正在尝试恢复。 |
| Rejoining the server... | 正在重新连接… |
| Rejoin failed. Trying again in \<n\>s. | 重连失败，将在 \<n\> 秒后重试。 |
| Failed to rejoin… | 无法重新连接。请重试，或刷新页面。 |
| The session has been paused… | 服务端已暂停会话。 |
| Failed to resume the session… | 恢复会话失败。请重试，或刷新页面。 |
| Retry / Resume | 重试 / 恢复 |
| （新增失败态提示） | 服务可能已退出——请重新运行 MacroTool.exe，再点「重试」。 |

### D10. 测试护栏：新增 `FallbackPagesTests`

- 404：h1 文本、回页链接；路径解析纯函数用例（有原始路径 / 无 / `/not-found`）。
- Error：注入 cascading `HttpContext`（`DefaultHttpContext` + `TraceIdentifier`）断言请求 ID 渲染；断言不再出现 `Development`/`text-danger` 字样。
- ReconnectModal：断言 `components-reconnect-modal`、`components-reconnect-button`、`components-resume-button` id 与关键 `components-reconnect-*` 类仍在，且关键文案为中文（契约 + 语言双保险）。

## Risks / Trade-offs

- [重连弹窗只能在真实断线时目检] → 手动检查清单：杀掉/重启服务进程，覆盖重连中、失败、暂停三态的文案与配色。
- [404 路径在不同渲染路径取值不同] → D2 的优先级策略 + 纯函数测试；目检同时覆盖软导航与直接 GET。
- [bUnit 模拟 `IStatusCodeReExecuteFeature` 繁琐] → 解析逻辑抽 internal 纯函数直测，组件只做一次 feature 读取。
- [与未提交的迁移改动并行] → 本 change 只新增 CSS 规则、不删既有规则；建议迁移先提交，本 change 单独 commit。
- [Error 页依赖 MainLayout/ControlBar] → 维持现状；正文本身不依赖引擎（静态链接），异常场景下仍可导航。
- [改文案可能破坏重连 JS 依赖的 DOM 结构] → 只改文本节点与新增提示行/类，id 与既有类名保持逐字不变；测试锁定。

## Migration Plan

1. `NotFound.razor` 改造（D1-D3）+ 路径解析纯函数 + 测试。
2. `Error.razor` 改造（D4-D6）+ `.empty-icon.is-danger` + 测试。
3. `ReconnectModal.razor/.razor.css` 文案与视觉（D7-D9）+ 契约测试。
4. `dotnet build MacroTool.slnx` + `dotnet test MacroTool.slnx` 全绿；人工目检：非 Development 下访问 `/Error`、软导航与直接 GET 未知路径、真实断线三态。
5. 回滚：逐文件 revert；零数据/协议/配置变更。

## Open Questions

- 重连弹窗状态点用琥珀还是中性灰：留给实现时目检微调，不改变方案与任务拆分。
