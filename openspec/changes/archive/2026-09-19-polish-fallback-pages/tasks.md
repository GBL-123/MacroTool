## 1. 404 NotFound

- [x] 1.1 抽出请求路径解析 internal 纯函数（`OriginalPath` 优先、`NavigationManager` 相对路径回退、空或 `not-found` 返回 null），并验证三种输入的单测通过
- [x] 1.2 `NotFound.razor` 换 `.empty-state` 版式（`MudPaper.panel` 外壳、`404` 图标块、`<h1 class="empty-title">`、副文含路径、`返回概览` 按钮），PageTitle 改 `页面不存在 - MacroTool`；验证 build 成功且路径纯函数测试与既有测试全通过

## 2. Error

- [x] 2.1 `app.css` 在 `.empty-icon` 附近新增 `.empty-icon.is-danger` 修饰类（`--danger-soft` 背景 / `--danger-text` 文字）；验证 build 成功且 CSS 中该规则可检索
- [x] 2.2 `Error.razor` 改造：补 `@layout Layout.MainLayout`、删 Development/`ASPNETCORE_ENVIRONMENT` 样板与 `text-danger`、换中文空态正文（`ErrorOutline` + danger 图标、mono 请求 ID、`Href="/"` 返回概览），保留 `HttpContext`/`Activity` 取值逻辑；验证 `FallbackPagesTests` 的 Error 断言通过

## 3. ReconnectModal

- [x] 3.1 `ReconnectModal.razor` 全部文案替换为定稿中文，保留全部 `components-reconnect-*` 类名与元素 id 逐字不变，失败态新增 `components-reconnect-hint` 提示行；验证 `FallbackPagesTests` 的契约断言通过
- [x] 3.2 `ReconnectModal.razor.css` 暖色化：遮罩 `rgba(30,27,22,.45)` + 极淡琥珀 radial、卡片改用 `--surface/--border/--radius/--shadow-2`、标题 `--font-display`、状态点琥珀 + pulse、按钮对齐 Mud 按钮造型与焦点环、hint 小字样式；验证 build 成功且 CSS 中既有 id/类选择器仍完整保留

## 4. 测试护栏

- [x] 4.1 新增 `FallbackPagesTests`：404 的 h1/回页链接/路径显示与隐藏、Error 的请求 ID 渲染（cascading `HttpContext`）与无模板残留、ReconnectModal 的 id/类契约与中文文案；验证该测试类全部通过

## 5. 最终验证

- [x] 5.1 运行 `dotnet build MacroTool.slnx` 与 `dotnet test MacroTool.slnx`，验证全绿且无既有测试回归
- [ ] 5.2 人工目检：非 Development 下访问 `/Error`（壳/文案/请求 ID/返回概览）、软导航与直接 GET 未知路径的 404、真实断线三态（重连中/失败/暂停）文案与配色；记录结论
