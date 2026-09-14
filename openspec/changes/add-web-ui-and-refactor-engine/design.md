## Context

动机见 proposal.md；行为契约见 `specs/macro-engine`、`specs/control-panel`、`specs/timeline-editor`。

现状与约束：

- `MacroTool/Application/` 是从控制台项目粘贴来的完整实现，但当前宿主是 `.NET 10 Blazor Server + MudBlazor 9`，`Program.cs` 从未引用它；`Components/` 还是纯模板（`MainLayout.razor` 只有 `@Body`，Mud 的 Provider 都没装）
- 底层依赖 Windows 桌面会话：低级键鼠钩子（`WH_KEYBOARD_LL` / `WH_MOUSE_LL`）与 `SendInput`，仅能在交互式桌面进程中使用
- `RegisterHotKey(IntPtr.Zero, ...)` 把 `WM_HOTKEY` 投递到**调用线程**的消息队列，必须由同一线程 `GetMessage`/`PeekMessage` 泵送；ASP.NET Core 宿主没有 UI 消息泵
- Blazor Server 的有效载荷是 N 个 circuit（多个标签页/连接），而引擎是进程内唯一资源；浏览器全部关闭时引擎必须继续工作
- 录制/回放对时序敏感，现有"专用线程 + `Stopwatch` 轮询"的模型要保持
- `timeline.json` 是既有对外数据格式，不应改变

```
[热键线程]--+                                  +--> circuit 1 (标签页)
            |                                  |
[Recorder]-+--> MacroEngine(单例) --事件广播--+--> circuit 2 (标签页)
            |    ^  状态机 / CurrentTimeline     |
[Player]---+    |                              +--> circuit N
                 +-- TimelineStore(load/save/bak)
```

## Goals / Non-Goals

**Goals:**

- 把控制台引擎重构为 Web 宿主中的常驻单例服务，浏览器仅为观察者/控制器
- 行为（录制内容、回放时序、热键语义、文件格式）与现实现保持等价，除已声明的变更外不做行为漂移
- 编辑器操作做成与 Blazor 无关的纯函数，可用 xUnit 覆盖
- 引擎的线程与原生资源（钩子、热键）有明确的所有者与释放路径

**Non-Goals:**

- 不做多用户、鉴权、远程访问（仅 localhost）
- 不做坐标编辑、按键重映射、撤销/重做（本轮明确出局）
- 不把 Recorder/Player 改写为 async/TAP（时序精度优先，保留线程模型）
- 不改 `timeline.json` 格式，不做数据迁移
- 不追求跨平台

## Decisions

### 1. 引擎为 DI 单例 + HostedService，UI 只观察

`MacroEngine`（单例）持有状态机、`Recorder`、`Player`、当前时间线；`MacroHostedService : IHostedService` 负责启动热键、退出清理。UI 通过 DI 取引擎并订阅事件。

备选：作用域服务（错：状态是全局的）；静态服务定位器（可测性差）。

### 2. 热键宿主自持线程与消息泵

`HotkeyHost` 自己创建线程，在该线程内完成 `RegisterHotKey` → `GetMessage` 循环 → 投递回调 → `WM_QUIT` 退出。原实现依赖 `Start.Main` 的主循环泵消息，在 Web 宿主中不存在。

备选：隐藏窗口 + `WndProc`（原生代码更多）；ASP.NET 线程上注册（无消息泵，且会被请求处理阻塞）。

### 3. 引擎 → UI：状态/日志用事件推送，高频计数用轮询

- 引擎事件：`StateChanged`、`LogEmitted`、`TimelineChanged`、`HotkeyStatusChanged`
- 事件可能从热键线程/录制线程/回放线程触发，组件必须 `InvokeAsync(StateHasChanged)` 回主同步上下文；订阅在 `OnAfterRender(firstRender: true)` 建立、`IAsyncDisposable` 释放，避免预渲染重复订阅
- 录制计数、回放轮次等高频数据不做逐事件推送（会淹没 circuit），由页面在录制/回放期间用 500ms 定时器拉取引擎快照

备选：全部轮询（简单但状态/日志有延迟）；全部事件推送（高频计数造成无谓流量）。

### 4. 日志缓冲

引擎日志经 `ILogger` 输出，同时写入单例 `LogBuffer`（环形，默认 500 条）；新 circuit 连接时先回放缓冲历史。日志覆盖需求见 `macro-engine` 的"状态、进度与日志广播"。

### 5. 停止录制的尾部过滤

`Recorder.Stop(bool dropTrailingMouse)`：为 true 时丢弃 `T >= stopRequestMs - 300ms` 的鼠标事件。控制面板停止传 true，F10 传 false。停止请求到达服务器时物理点击已完成，因此该窗口内尾部鼠标事件可安全视为"点按钮"。

备选：前端上报浏览器窗口矩形做坐标过滤（多显示器/DPI 换算复杂，放弃）；只警告（弱）。

### 6. 编辑模型：工作副本 + 纯函数 + 保存后引擎重载

- 编辑器把引擎当前时间线复制为工作副本，所有编辑作用于副本；未保存的更改不触碰引擎（满足"未保存不影响回放"）
- 配对、删除、修剪、平移、缩放、重算总时长实现为无 Blazor 依赖的纯函数（放 `Application/`），xUnit 直接覆盖不变量
- 保存流程：`TimelineStore.Save(workingCopy)`（写主文件前先备份）→ `MacroEngine.ReloadTimeline()`（仅 Idle 允许）→ 引擎广播 `TimelineChanged`
- 修剪按"动作起点"判定，保证不拆散 down/up；时间编辑后统一升序重排并重算 `DurationMs = max(T)`

备选：直接改引擎中的活动对象（未保存状态会污染回放，放弃）。

### 7. TimelineStore 为 DI 服务，路径可配 + 自动备份

- 路径来自 `Macro:TimelinePath`，留空时默认 `AppContext.BaseDirectory/timeline.json`（保持 README 的"exe 同目录"心智）
- 任何保存（录制停止、编辑器保存、退出时保存）在目标文件已存在时先写 `<name>.bak`（单份覆盖）
- 错误以结果对象返回并记日志，不再打印控制台

### 8. 配置与运行时参数

`MacroOptions`（`appsettings.json` 的 `Macro` 节）：`Speed`（默认 1.0）、`Jitter`（默认 2）、`TimelinePath`、`OpenBrowser`（默认 true）。无效值回退默认并警告。运行期参数由 `EngineSettings` 持有，`Player` 在每轮开始时读取，实现"下一轮生效、不打断当前轮"。

原 `--speed/--jitter` 命令行参数移除（proposal 已声明 **BREAKING**）。

### 9. 重构后的目录与命名空间

```
MacroTool/Application/
+-- Interop/    Native.cs, InputSender.cs
+-- Engine/     MacroEngine.cs, Recorder.cs, Player.cs,
|               MacroEvent.cs, MacroTimeline.cs, EngineEvents.cs, EngineSettings.cs
+-- Hotkeys/    HotkeyHost.cs
+-- Storage/    TimelineStore.cs
+-- Editing/    TimelineEditing.cs（纯函数：配对/删除/修剪/平移/缩放/重算）
+-- Hosting/    MacroHostedService.cs, BrowserLauncher.cs
命名空间：MacroTool.Application.<目录>
```

`Components/`：

```
Components/
+-- Layout/   MainLayout.razor（Mud Provider + AppBar 全局状态条 + Drawer 导航）
+-- Pages/    Home.razor（仪表盘）, Timeline.razor（编辑器）
+-- Shared/   StatusCard, ControlPanel, ParameterPanel, LogViewer, ActionPairTable, EnvHints
```

### 10. Blazor/MudBlazor 接线

- `MainLayout` 必须包含 `MudThemeProvider`、`MudPopoverProvider`、`MudDialogProvider`、`MudSnackbarProvider`，否则 Mud 交互组件不工作
- 确认类交互用 `IDialogService`（修剪/缩放/重载/关闭服务），结果反馈用 `ISnackbar`
- 动作对表格用 `MudDataGrid`（开启虚拟化），配对的上下文行展开查看原始 down/up

### 11. 托管与启动

- `Program.cs` 顶部先 `Native.SetProcessDPIAware()`（保持原语义：系统 DPI 感知，录制坐标为物理屏幕像素）
- 移除 `UseHttpsRedirection`/HSTS（本机工具，避免证书摩擦）；`AllowedHosts` 收紧为 `localhost`；默认地址固定 `http://localhost:5047`（`Urls` 配置），使自动打开与 README 一致
- `appsettings.Development.json` 设 `Macro:OpenBrowser=false`（开发时 launchSettings 已自动打开），`BrowserLauncher` 在 `ApplicationStarted` 时按配置打开默认浏览器

### 12. 关闭语义

F12 与面板"关闭服务"走同一路径：停止录制（若有事件，先保存）→ 停止回放（释放悬挂按键）→ 注销热键与钩子 → `IHostApplicationLifetime.StopApplication()`。控制台 Q 退出移除。

### 13. 测试策略

新增 `MacroTool.Tests`（xUnit，引用 Web 工程）覆盖 `Editing` 纯函数与 `TimelineStore` 的文件/备份行为（临时目录）。原生钩子与注入无法单测，留人工验证清单（任务中列出）。`MacroTool.slnx` 加入测试工程。

## Risks / Trade-offs

- [宿主关闭时热键线程未退出会挂住进程] → `WM_QUIT` + 有界 `Join`，`IHostedService.StopAsync` 保证顺序
- [300ms 尾部过滤可能吞掉用户在停止前瞬间的真实点击] → 仅 UI 停止生效、F10 不受影响；文档注明
- [`Recorder.Stop()`/`Player.Stop()` 最多 3s 阻塞] → 引擎对外方法包 `Task.Run`/异步封装，避免卡住 Blazor circuit
- [多标签页并发下发命令] → 引擎是唯一权威，命令按到达顺序处理，UI 只反映引擎状态
- [大时间线表格渲染压力] → 动作对聚合使行数减半 + `MudDataGrid` 虚拟化，必要时分页
- [录制期间用户点控制面板会把点击录进去] → 面板在录制中显示醒目警示；本轮不做窗口矩形过滤
- [退出时自动保存可能把不想要的录制覆盖到好时间线] → 保存前自动 `.bak`，可恢复
- [编辑器未保存状态与 F11 的认知偏差] → 未保存横幅明确"回放仍使用磁盘旧时间线"
- [DPI 感知用系统级而非 per-monitor] → 与原工具行为一致；混合 DPI 场景留待后续独立变更

## Migration Plan

- 无数据迁移：`timeline.json` 格式不变
- 部署即本地运行；回滚 = 回退提交（无外部状态变更，`timeline.json.bak` 为附加文件，可忽略）
- 实施顺序：引擎重构（暂不接 UI）→ 手工验证热键/录制/回放 → 接 UI 面板 → 编辑器 → 测试与 README

## Open Questions

- VK → 键名显示映射先覆盖字母、数字、常用功能键与修饰键，冷门键显示 `VK 0x..`；完整键表可后续补
- 日志是否同时落盘（如 `logs/`）留待后续；本轮仅内存环形缓冲 + `ILogger` 控制台输出
