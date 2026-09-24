# Tasks

## 1. 浏览器关闭脚本

- [x] 1.1 在 `MacroTool.Web/wwwroot/js/macro.js` 的 `window.macroTool` 中新增 `closePanel`：延时调用 `window.close()`，被拒绝时再延时 `location.replace('about:blank')` 兜底；验证：`dotnet build MacroTool.slnx` 通过，页面控制台手工调用可关闭/离开当前页

## 2. 关闭服务流程

- [x] 2.1 `MacroTool.Web/Components/Shared/ControlBar.razor` 注入 `IJSRuntime`；`Shutdown()` 在确认后先 `await JS.InvokeVoidAsync("macroTool.closePanel")`（异常吞掉、不阻断），再调用 `Engine.RequestShutdown(fromUi: true)`；验证：`Shutdown_Confirmed_RequestsEngineShutdown` 断言先出现 `macroTool.closePanel` 调用、随后触发退出

## 3. 测试

- [x] 3.1 更新 `MacroTool.Tests/ControlBarTests.cs`：确认关闭断言 `JSInterop.Invocations` 含 `macroTool.closePanel`；取消关闭断言不含该调用；验证：`dotnet test MacroTool.slnx` 全绿

## 4. 文档

- [x] 4.1 `README.md` 在关闭服务/退出相关段落补充：确认关闭会先关闭面板页面（浏览器拒绝脚本关闭时退化为空白页），单标签页场景下退出不再等待约 30 秒；验证：README 文本与行为一致

## 5. 整体验证

- [x] 5.1 全量回归：`dotnet build MacroTool.slnx` 与 `dotnet test MacroTool.slnx` 通过
- [x] 5.2 端到端核对（发布版 + headless Edge/CDP）：点击"关闭服务"并确认后标签页关闭、进程在数秒内退出、无重连弹窗；对 history > 1 的页面核对退化为空白页
