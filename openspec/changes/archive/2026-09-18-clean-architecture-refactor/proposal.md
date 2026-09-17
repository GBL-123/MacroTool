## Why

当前所有逻辑集中在单个 Web 项目中，分层只靠文件夹约定，没有任何编译期约束：`MacroEngine` 直接 `new Recorder()`、注入具体 `TimelineStore`/`HotkeyHost`，UI 页面直接注入基础设施。结果是引擎无法脱离 Windows 真机与 Blazor 测试，依赖方向随时可能被侵蚀。本次重构用 Clean Architecture 的依赖倒置把边界固化为编译期规则，为后续演进（换 UI、复用引擎、跨平台核心）打底。

## What Changes

- 解决方案拆分为 `MacroTool.Domain` / `MacroTool.Application` / `MacroTool.Infrastructure` / `MacroTool.Web` / `MacroTool.Tests` 五个项目；原 `MacroTool` 项目重命名为 `MacroTool.Web`（文件夹、csproj、命名空间；`AssemblyName` 保持 `MacroTool`，exe 名与运行时行为不变）。
- Domain 提取纯模型与纯算法（`MacroEvent`/`MacroTimeline`、`EngineStateMachine`、`TimelineEditing`、`KeyNames`），零外部依赖。
- Application 定义四个端口并承载用例：`IInputSink`、`IInputCaptureSource`、`ITimelineRepository`、`IGlobalHotkeys`；`MacroEngine`、`Player`、`Recorder` 策略（去重、F 键过滤、尾部鼠标丢弃、配对排序）移入 Application。
- Infrastructure 实现端口：`Win32` 输入注入、低级钩子捕获、全局热键、JSON 时间线仓储（保留 `.bak`/原子写）、系统上下文；`Native`/`InputSender` 收敛为 internal。
- UI 只依赖 `MacroEngine` 门面：`EngineSnapshot` 补 `Speed`/`Jitter`/`RepeatCount`，引擎新增 `TimelinePath`、`HotkeyStatusChanged`、`SaveTimeline()`、`LoadTimelineFromDisk()`；`Timeline`/`Home`/`EnvHints`/`ControlBar` 不再注入 `TimelineStore`/`HotkeyHost`/`EngineSettings`（`LogBuffer` 仍可注入，属 Application）。
- 新增架构测试守护"Components 不引用 Infrastructure"；`coverage.ps1` 改为按产品程序集汇总行覆盖率，阈值仍 75%。
- **BREAKING**（仅开发/构建层面）：项目路径与命名空间变更，`dotnet run`/`dotnet publish` 命令、AGENTS.md 文档同步更新；用户可见行为、配置键、时间线 JSON 格式、热键、exe 名称均不变。
- 非目标：不新增功能、不改变任何产品行为、不引入插件化/事件总线等额外抽象，不做本轮之外的测试重写。

## Capabilities

### New Capabilities

（无）

### Modified Capabilities

（无——纯结构重构，无 spec 级行为变化。按 OpenSpec 约定在 `.openspec.yaml` 设置 `skip_specs: true` 显式跳过 delta spec，不伪造需求变更。）

## Impact

- 代码：`MacroTool/` 下全部 45 个源文件与 17 个测试文件的归属/命名空间迁移；`MacroTool.slnx`、`MacroTool.Tests.csproj` 引用更新。
- 工具链：`coverage.ps1` 门禁从单个 `MacroTool` 程序集改为产品程序集汇总；无 CI 变更。
- 文档：`README.md`（开发/发布命令）、`AGENTS.md`（架构与目录说明）需同步。
- 风险：CP0 门面化是唯一有语义改动的阶段，依赖 ~140 个现有测试守护；CP1/CP2 为编译器可验证的机械搬迁。
