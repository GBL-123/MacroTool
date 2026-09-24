# Design: close-panel-on-exit

## Context

见 proposal.md - Why：慢退出与重连弹窗的根因是 Blazor Server 电路在主机关停时不会被主动断开，Kestrel 只能等 `/_blazor` WebSocket 关闭到 `HostOptions.ShutdownTimeout`（默认 30s）。

关键约束（已实测，Edge/Chromium）：

- 顶层页面只有在"会话历史只有一条记录"时才允许脚本 `window.close()`；由命令行/ShellExecute 直接打开的标签页满足该条件，应用内导航过（history > 1）则不满足。
- 只有让页面真正关闭或导航离开，才能立刻拆掉 WebSocket 与电路；仅隐藏 UI 或改文案不影响主机等待。
- `ControlBar.Shutdown()` 由电路同步上下文执行；`Engine.RequestShutdown` 之后该上下文可能被阻塞数秒，因此关闭页面的指令必须在触发退出之前下发。

## Goals / Non-Goals

**Goals:**

- 点击"关闭服务"并确认后，面板页面在数百毫秒内关闭（或退化为空白页），不再出现重连界面。
- 单标签页场景下主机退出不再等待 30 秒连接超时。
- 关闭/退出失败时不影响既有的退出流程（引擎仍会停止录制、保存、结束进程）。

**Non-Goals:**

- 不处理 F12/退出热键路径（无 UI 上下文，保持现状）。
- 不引入服务端电路管理（`CircuitHandler` / `RequestCircuitPauseAsync`）或调整 `HostOptions.ShutdownTimeout`。
- 不改变确认对话框交互、不处理多标签页与手工关闭页面的场景。

## Decisions

1. **客户端先关闭、服务端后退出（选定）**
   `ControlBar.Shutdown()` 在确认后先 `await JS.InvokeVoidAsync("macroTool.closePanel")`，再调用 `Engine.RequestShutdown(fromUi: true)`。
   备选：(a) 调低 `ShutdownTimeout`——只缩短等待，页面仍显示重连弹窗且改变全局关停语义；(b) 服务端主动断开电路——需要跟踪电路、代码量与风险更高，且只解决慢、不解决"页面关闭"。两者均不满足"直接关闭页面"。

2. **`window.close()` 为主，`about:blank` 兜底**
   脚本先在 `setTimeout` 中调用 `window.close()`；若浏览器拒绝（导航过、非 Chromium），再延时 `location.replace('about:blank')` 离开当前页面。兜底必须是"导航离开"而不是停留在原页：只有导航才会立即销毁电路并关闭 WebSocket。实测：Edge 命令行打开（history=1）`window.close()` 成功；`pushState` 后（history=2）被拒。

3. **关闭指令先下发，等页面断开后再触发退出**
   JS 把 `window.close()` 放进定时器（约 100ms 后执行、兜底导航再晚 150ms）并立刻返回，保证 JS 互操作回执先回到服务端；C# 侧收到回执后在**线程池**上等待约 350ms 再触发退出（`Task.Delay(...).ConfigureAwait(false)`）。这样页面先断开 WebSocket，主机关停时没有遗留连接可等——实测若在页面关闭前启动关停，Kestrel 会先中止连接、客户端重连，主机为被中止的请求再等约 5 秒。延时必须用 `ConfigureAwait(false)`：页面关闭会销毁电路上下文，若续体捕获该上下文可能永远不执行，退出就不会触发。

4. **JS 调用失败仍继续退出**
   `IJSRuntime` 调用包在 `try/catch` 中（例如电路已断连时抛 `JSDisconnectedException`），任何失败都不阻止 `Engine.RequestShutdown`。

5. **无服务端改动**
   页面断开后 `/_blazor` 连接自然消失，Kestrel 优雅关停随即完成；不新增端点、不新增配置项。

## Risks / Trade-offs

- [浏览器拒绝关闭（Firefox 或页面内导航过）] → 兜底导航到空白页：页面内容消失、连接断开、无重连弹窗；用户需手动关标签页。
- [多个面板标签页同时打开] → 只关闭发起操作的那一个，其余仍会等待连接超时；已在 proposal 中声明为非目标。
- [JS 互操作阻塞导致退出不触发] → 关闭定时器延迟执行（约 100–150ms）使回执先行；若仍失败，`catch` 后照常退出。
- [页面关闭晚于 350ms 延时] → 退化为"先关停、后断开"：仍会退出，只是主机为被中止的连接多等约 5 秒（而非 30 秒）。延时值 350ms 覆盖 JS 关闭（100ms）与兜底导航（250ms）。
- [bUnit 无法验证真实浏览器关闭行为] → bUnit 断言"确认后先调用 `macroTool.closePanel`、再触发引擎退出"；真实关闭/兜底行为用 headless Edge + CDP 手工核对。

## Migration Plan

无数据、配置或接口变更；纯前端行为调整，重启应用即生效。回滚 = 还原提交。

## Open Questions

无。
