## Why

`MacroTool/Application/` 下是一份从其他项目粘贴来的控制台宏录制器代码：入口 `Start.Main`、主线程消息泵、`Console.WriteLine` 当仪表盘，与当前 Blazor Server 宿主完全脱节（全项目无任何引用，属于"能编译的死代码"）。同时 README 仍宣称"无 UI"。本次变更把这台引擎重构为 Web 宿主的常驻服务，并补上浏览器控制面板与时间线编辑能力。

## What Changes

- **BREAKING**: 应用形态从"控制台工具"变为"本机 Web 服务 + 浏览器控制面板"。启动后自动打开 `http://localhost`；控制台 Q 键退出作废，退出改为 F12 热键或页面"关闭服务"按钮
- 重构 `Application/`：按 Interop / Engine / Hotkeys / Storage / Hosting 重组；引擎变为 DI 单例 + `IHostedService`；热键宿主改为自持专用线程（注册与消息泵同线程）；所有 `Console.WriteLine` 换成 `ILogger` + 引擎事件流
- 新增控制面板 UI（MudBlazor）：实时状态、录制/回放/停止控制、实时计数、参数面板（倍速/抖动）、实时日志流、热键与环境提示、"关闭服务"
- 新增时间线编辑器：动作对视图（自动配对 down/up）、删除/配对删除、头尾修剪、整体平移与时间缩放、保存（自动留 `.bak`）、重新载入、未保存标记；仅在引擎空闲时可用
- UI 停止录制时丢弃尾部鼠标事件（约 300ms 窗口），避免把"点按钮"这一下录进时间线；F10 热键停止不做任何过滤
- 时间线存储保持现有 `timeline.json` 格式；路径可配置，默认 exe 目录；编辑保存自动备份
- 新增 xUnit 测试项目，覆盖编辑器操作与时间线不变量（时间升序、配对、`DurationMs` 重算）
- README 从"无 UI"改写为浏览器控制面板用法；`--speed`/`--jitter` 不再是主要配置入口（初值来自 appsettings，运行期由页面调参）

## Capabilities

### New Capabilities
- `macro-engine`: 常驻宏引擎的行为——状态机（空闲/录制/回放）、全局热键、录制与回放、进度与日志事件、生命周期与关闭
- `control-panel`: 浏览器控制面板的行为——状态展示、控制操作、参数调整、日志流、运行环境提示与关闭服务
- `timeline-editor`: 时间线的载入/展示/编辑/保存行为——动作对视图、删除/修剪/平移/缩放、备份与未保存管理

### Modified Capabilities
<!-- 项目尚无已建立的 specs，无修改项 -->
（无）

## Impact

- 受影响代码：`MacroTool/Application/*`（全部重组与重写）、`MacroTool/Program.cs`、`MacroTool/Components/*`（布局与页面）、`MacroTool/MacroTool.csproj`、`appsettings.json`、`README.md`；新增测试项目
- 依赖：运行时无新增（MudBlazor 已在）；测试新增 xUnit
- 运行环境：仅 Windows 交互桌面会话；仅绑定 localhost
- 数据：`timeline.json` 格式不变，新增同目录 `.bak` 备份文件
