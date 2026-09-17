## 1. CP0 - 端口与引擎内核（单项目内，行为不变）

- [x] 1.1 在 Application 侧定义端口与契约：`IInputSink`、`IInputCaptureSource`、`ITimelineRepository`、`IGlobalHotkeys`，并把 `TimelineLoadResult`/`TimelineSaveResult`/`HotkeyStatus` 放到端口归属处；验证 `dotnet build MacroTool.slnx` 成功
- [x] 1.2 `Player` 改用 `IInputSink`，删除 `PlayerDelegates`，`PlayerDelegatesTests` 改为基于 fake `IInputSink`；验证该测试类通过
- [x] 1.3 新增 `Win32InputSink`（包装现有 `InputSender`）实现 `IInputSink`；验证 build 成功且无调用点回归
- [x] 1.4 拆分 `Recorder`：Application 保留策略（F 键过滤、去重、计时、计数、`DropTrailingMouse`、排序），新增 Infrastructure 侧 `LowLevelHookCapture` 实现 `IInputCaptureSource`（专用线程 + 钩子 + 消息泵 + WM_QUIT 停止，支持按录制会话创建）；`RecorderProcessTests` 改为通过 fake source 触发；验证 `RecorderProcessTests` 与 Windows-only `Recorder_StartStopWithoutInput_ProducesEmptyTimeline` 通过
- [x] 1.5 `MacroEngine` 构造函数改收端口（`IGlobalHotkeys`、`ITimelineRepository`、`Func<IInputCaptureSource>`、`IInputSink`、`bool isElevated`），移除 `Native`/`new Recorder()`/`new Player()` 直接依赖；验证 `MacroEngineTests` 通过
- [x] 1.6 门面扩展：`EngineSnapshot` 增加 `Speed`/`Jitter`/`RepeatCount`；新增 `TimelinePath`、`HotkeyStatusChanged`、`SaveTimeline()`（先存盘、成功后替换）、`LoadTimelineFromDisk()`（只读盘不替换），并更新引擎测试；验证 `MacroEngineTests` 新用例通过
- [x] 1.7 `UiTestScope` 按新构造签名装配（真实适配器 + 临时目录或 fake）；验证全量 `dotnet test MacroTool.slnx` 通过

## 2. CP0 - UI 门面化

- [x] 2.1 `Timeline.razor` 移除 `TimelineStore`/`EngineSettings` 注入，改用 `Engine.SaveTimeline`/`Engine.LoadTimelineFromDisk` 与快照中的速度；验证 `TimelinePageTests` 通过且保存/重载/脏标记/Snackbar 分支与现状一致
- [x] 2.2 `ControlBar.razor` 移除 `EngineSettings` 注入，用快照初始化输入、`Engine.UpdateSettings` 提交；验证 `ControlBarTests` 通过
- [x] 2.3 `EnvHints.razor`/`Home.razor` 移除 `HotkeyHost`/`TimelineStore` 注入，改用 `Engine.HotkeyStatus`/`HotkeyStatusChanged`/`TimelinePath`；验证 `SharedComponentTests`、`OverviewPageTests`、`ShellAndThemeTests` 通过
- [x] 2.4 全量 `dotnet test MacroTool.slnx` 通过，确认 Components 中不再出现 `@inject` 基础设施类型（`grep @inject MacroTool\Components` 人工核对）

## 3. CP1 - Domain 项目提取

- [x] 3.1 新建 `MacroTool.Domain`（net10.0、零包引用），迁移 `MacroEvent`/`MacroTimeline`、`EngineStateMachine`/`ToolState`、`TimelineEditing`/`TimelineAction`/`UnpairedKind`、`KeyNames`，命名空间改为 `MacroTool.Domain`（编辑归属 `MacroTool.Domain.Editing`）；验证 build 成功
- [x] 3.2 当前项目添加对 Domain 的引用并更新 `MacroTool.slnx`、所有受影响 using（含 `_Imports.razor`、测试）；验证全量测试通过

## 4. CP2 - Application / Infrastructure / Web 拆分

- [x] 4.1 新建 `MacroTool.Application`（net10.0，引用 Domain + `Microsoft.Extensions.Logging.Abstractions`），迁移 `MacroEngine`、`EngineSettings`、`LogBuffer`、`Player`、`Recorder`、端口与结果类型；验证 build 成功
- [x] 4.2 新建 `MacroTool.Infrastructure`（net10.0，引用 Application + Domain，无额外包），迁移 `Native`、`Win32InputSink`、`LowLevelHookCapture`、`HotkeyHost`、`TimelineStore`、DPI/提权辅助，保持 `Native`/`InputSender` internal；验证 build 成功
- [x] 4.3 原项目原地改名为 `MacroTool.Web`（文件夹、csproj、命名空间 `MacroTool.Web.Components`/`MacroTool.Web.Hosting`，显式 `<AssemblyName>MacroTool</AssemblyName>`），迁移 `Hosting`/`MacroOptions`，更新 `MacroTool.slnx` 与测试项目引用；验证 build 与全量测试通过
- [x] 4.4 清理 `InternalsVisibleTo`：Web 保留（`ControlBar` internals），Application/Infrastructure 按需添加或改为 public；验证测试编译无误且无多余 IVT
- [x] 4.5 发布冒烟：`dotnet publish MacroTool.Web/MacroTool.Web.csproj -c Release -r win-x64 --self-contained false`，确认产物为 `MacroTool.exe`、`appsettings.json` 与默认 `timeline.json` 解析路径（exe 旁）不变

## 5. 边界守护与工具链

- [x] 5.1 新增架构测试（手写反射，不引包）：断言 `MacroTool.Web.Components.*` 类型签名不引用 `MacroTool.Infrastructure`；验证测试通过，并临时引入一个 Infrastructure 引用确认能失败后还原
- [x] 5.2 `coverage.ps1` 改为解析 cobertura 中四个产品程序集 `line-rate` 加权汇总（阈值仍 75%，排除测试程序集）；运行 `.\coverage.ps1` 验证门禁通过并输出各程序集占比
- [x] 5.3 清理旧的单程序集门禁逻辑与残留路径引用；验证 `coverage.ps1` 不再依赖 `MacroTool` 单行匹配

## 6. 文档与最终验证

- [x] 6.1 README 更新开发/发布命令：`dotnet run --project MacroTool.Web`、`dotnet publish MacroTool.Web/MacroTool.Web.csproj ...`；确认 `MacroTool.exe` 等产品描述未变
- [x] 6.2 AGENTS.md 更新：项目布局、`MacroTool.Web/Program.cs` 组合根、构建锁说明、测试命令不变性
- [x] 6.3 最终验证：`dotnet build MacroTool.slnx`、`dotnet test MacroTool.slnx`、`.\coverage.ps1` 全部通过；在交互桌面上手动冒烟录制/回放/保存/重载，观察行为与此前一致
