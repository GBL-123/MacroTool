# Proposal: close-panel-on-exit

## Why

点击"关闭服务"并确认后，浏览器面板不会自行关闭：ASP.NET Core 关停时不会主动断开 Blazor Server 电路，主机只能等待 `/_blazor` WebSocket 关闭，直到 `HostOptions.ShutdownTimeout`（默认 30 秒）才强制断开。实测（发布版）：面板开着时退出固定耗时约 30 秒（按钮 30.05s / F12 30.07s），期间页面还会出现"连接已中断、正在重新连接"的弹窗；没有页面连接时只需 0.11 秒。

## What Changes

- 面板"关闭服务"确认后，先请求浏览器页面自行关闭（`window.close()`），再触发引擎退出流程，让 WebSocket 在主机开始关停前就断开。
- 浏览器拒绝脚本关闭页面时（例如用户点过"时间线/日志"导致 history > 1，或使用非 Chromium 浏览器），退化为把该标签页导航到空白页（`about:blank`）：页面内容消失、连接立即断开，不再出现重连弹窗。
- 页面关闭/离开后主机不再等待 30 秒，退出耗时降到亚秒级。

## Capabilities

### New Capabilities

（无）

### Modified Capabilities

- `control-panel`: "关闭服务"要求新增一条行为——确认后 SHALL 让承载面板的浏览器页面自行关闭；浏览器拒绝关闭时 SHALL 退化为离开当前页面（空白页），保证面板不再显示重连状态。

## Impact

- `MacroTool.Web/Components/Shared/ControlBar.razor`：`Shutdown()` 流程注入 `IJSRuntime` 并先调用关闭脚本。
- `MacroTool.Web/wwwroot/js/macro.js`：新增 `macroTool.closePanel`。
- `MacroTool.Tests/ControlBarTests.cs`：关闭服务相关测试补充 JS 调用断言。
- `README.md`：补充"关闭服务会关闭面板页面"的行为说明。
- 不涉及 Domain/Application/Infrastructure、配置格式与时间线 schema；F12 退出热键路径的行为不在本变更范围内（保持现状，可作为后续变更）。
- 多个面板标签页同时打开时，本变更只关闭发起操作的那个页面，其余标签页仍按现状在连接超时后断开（约 30 秒），本变更不处理。
