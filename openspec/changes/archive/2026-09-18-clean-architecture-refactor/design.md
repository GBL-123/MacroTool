## Context

见 `proposal.md`。约束与现状要点（探索阶段已核实）：

- 单项目 `MacroTool`（Web SDK，`AssemblyName=MacroTool`），45 个源文件约 2k 行，17 个测试文件约 140 个测试（xUnit v3 + Moq + bUnit，MTP 运行）。
- `Application/` 已不引用 `Components/`，但内部混装：`Engine/MacroEvent.cs`、`EngineStateMachine.cs`、`Editing/TimelineEditing.cs` 是纯逻辑；`Recorder`/`Player`/`HotkeyHost`/`TimelineStore`/`Interop/*` 是基础设施。
- 唯一现成测试缝：`PlayerDelegates`（函数元组）与 `Recorder.ProcessKey/ProcessMouse`（internal）。`Player` 默认落到静态 `InputSender`；`Recorder` 直接调用 `Native`；`MacroEngine` 构造时调用 `Native.IsUserAnAdmin()` 并 `new Recorder()`/`new Player()`。
- UI 直连基础设施：`Timeline.razor` 调 `Store.Save/Load` + `Engine.ReplaceTimeline`；`ControlBar`/`Timeline` 读写 `EngineSettings`；`EnvHints`/`Home` 读 `HotkeyHost.Status`、`Store.FilePath`。
- 运行约束：Windows 交互式桌面的低级钩子；`ContentRootPath = AppContext.BaseDirectory`，默认 `timeline.json` 位于 exe 旁；构建时运行中的 `MacroTool.exe` 会锁定 apphost。
- `coverage.ps1` 目前只对 `MacroTool` 程序集行做 75% 门禁；其测试 exe 路径为 `MacroTool.Tests\bin\Debug\net10.0\MacroTool.Tests.exe`。

## Goals / Non-Goals

**Goals:**

- 依赖方向由 csproj 编译期强制：Domain 零依赖；Application → Domain（+ MEL.Abstractions）；Infrastructure → Application + Domain；Web 作为唯一组合根。
- 应用用例（`MacroEngine`/`Player`/`Recorder` 策略）与 Win32/文件机制完全分离，可在无 Windows 钩子的情况下单测。
- UI 只依赖 `MacroEngine` 门面，不再注入任何 Infrastructure 类型。
- 保持产品行为、配置键、时间线 JSON 格式、热键语义、`MacroTool.exe` 名称与 `timeline.json` 解析路径完全不变。

**Non-Goals:**

- 不引入 CQRS/MediatR/事件总线/DI 容器抽象层等额外模式；端口只覆盖真实存在的 OS/文件边界。
- 不拆测试项目、不迁移测试框架、不重写现有测试断言（除构造方式与 using 适配）。
- 不为 Infrastructure 设置独立覆盖率门禁（其 P/Invoke 包装由集成测试覆盖即可）。
- 不把 `Infrastructure` 的 TFM 改为 `net10.0-windows`（保持 `net10.0` + 运行时守卫，保留非 Windows 构建能力与现有测试 no-op 约定）。

## Decisions

### D1. 项目划分（4 + 1）与依赖方向

```
MacroTool.Domain          -- 零依赖
MacroTool.Application     -- Domain + Microsoft.Extensions.Logging.Abstractions
MacroTool.Infrastructure  -- Application + Domain（BCL only）
MacroTool.Web             -- 全部（唯一组合根，含 Components/Hosting/Program）
MacroTool.Tests           -- 全部
```

备选：3 项目（Domain+Application 合并为 `MacroTool.Core`）——省一个 csproj，但失去"Domain 零依赖"的编译期保证；单项目分层+架构测试——改动最小但违背本次"硬边界"目标。均否决。

### D2. 端口定义（Application 内，最小集）

```csharp
public interface IInputSink                 // 输入注入（Player 用）
{
    bool SendKey(int scan, bool up, bool ext);
    bool SendMouseDown(int x, int y);
    bool SendMouseUp();
}

public interface IInputCaptureSource : IDisposable   // 低级钩子机制（Recorder 用）
{
    event Action<int, int, bool, bool>? KeyCaptured;   // vk, scan, ext, up
    event Action<bool, int, int>? MouseCaptured;       // isDown, x, y
    void Start();
    void Stop();                                        // 停钩子与消息泵
}

public interface ITimelineRepository        // 时间线持久化
{
    string FilePath { get; }
    bool Exists { get; }
    TimelineLoadResult Load();
    TimelineSaveResult Save(MacroTimeline timeline, double speedHint);
}

public interface IGlobalHotkeys : IDisposable
{
    Action? OnF10 { get; set; }
    Action? OnF11 { get; set; }
    Action? OnF12 { get; set; }
    HotkeyStatus Status { get; }
    event Action? StatusChanged;
    void Start();
}
```

- 否决 `ISystemContext`：`IsElevated` 是启动时读取一次的配置事实，由组合根读取后以 `bool` 传入引擎，不值得一个端口。DPI 声明由组合根调用 Infrastructure 的 `WindowsProcessSetup.EnableDpiAwareness()`。
- 否决保留 `PlayerDelegates`：与 `IInputSink` 重复；测试改用 Moq/fake。
- 鼠标消息语义（`WM_LBUTTONDOWN/UP`）由 Infrastructure 翻译为 `isDown`，Application 不出现 Win32 常量；F10/F11/F12 的虚拟键码是产品语义，放 Application 常量。

### D3. `Recorder` 机制/策略拆分

- Application `Recorder`（保留名）：订阅 `IInputCaptureSource`，负责 F 键过滤、按键/鼠标去重、`Stopwatch` 计时、计数、排序与 `DropTrailingMouse`、产出 `MacroTimeline`。
- Infrastructure `LowLevelHookCapture`：专用线程 + `SetWindowsHookEx` + 消息泵 + `PostThreadMessage(WM_QUIT)` 停止；把钩子回调转成语义事件。
- 现有 `Recorder.ProcessKey/ProcessMouse` 测试改由 fake `IInputCaptureSource` 触发事件验证同一策略；`LowLevelHookCapture` 的"无输入产生空时间线"保留为 Windows-only 集成测试。

### D4. UI 门面化（CP0）

`MacroEngine` 增量（门面保持薄委托，不新增业务逻辑）：

- `EngineSnapshot` 增加 `Speed` / `Jitter` / `RepeatCount`；
- 新增 `string TimelinePath`（替代 `Store.FilePath`）、`event Action? HotkeyStatusChanged`（替代 `Hotkeys.StatusChanged` 订阅）；
- 新增 `TimelineSaveResult SaveTimeline(MacroTimeline)`：内部先 `repository.Save`，成功后 `ReplaceTimeline`（保持现顺序与失败不替换语义）；
- 新增 `TimelineLoadResult LoadTimelineFromDisk()`：只读盘，不替换引擎（保持 `Timeline.razor` Reload 只刷新编辑区视图的语义）。

页面调整：`Timeline` 用 `Engine.SaveTimeline/LoadTimelineFromDisk` 与快照取 Speed；`ControlBar` 从快照初始化输入并用 `Engine.UpdateSettings` 提交；`EnvHints`/`Home` 用 `Engine.TimelinePath/HotkeyStatus/HotkeyStatusChanged/IsElevated`。`LogViewer` 继续注入 `LogBuffer`（Application 类型，不违反边界）。

### D5. 命名与程序集

- 文件夹/csproj/命名空间：`MacroTool.Web`（`MacroTool.Web.Components`、`MacroTool.Web.Hosting`，`MacroOptions` 移入 Hosting）；显式 `<AssemblyName>MacroTool</AssemblyName>`，`MacroTool.exe`、appsettings 与 `timeline.json` 路径不变。
- 测试项目 `MacroTool.Tests` 与测试 exe 路径不变。
- `MacroTool.Application` 添加 `Microsoft.Extensions.Logging.Abstractions` 包引用；Domain 无任何包引用。

### D6. 强制手段分层

- csproj 强制：Domain 无引用、Application 不引用 Infrastructure/Web、Infrastructure 不引用 Web。
- csproj 无法强制：Web 内 `Components` 不引用 Infrastructure（组合根必须引用）。新增一个手写反射架构测试（不引 NetArchTest）：遍历 Web 程序集中 `MacroTool.Web.Components.*` 类型的成员/局部签名引用的类型，断言其程序集不为 `MacroTool.Infrastructure`。

### D7. 覆盖率门禁

`coverage.ps1` 改为解析 cobertura XML 中四个产品程序集（`MacroTool`、`MacroTool.Domain`、`MacroTool.Application`、`MacroTool.Infrastructure`）的 `line-rate`，按行数加权汇总后与 75% 比较；测试程序集排除。备选：每个程序集单独阈值——否决，Infrastructure 的 P/Invoke 包装天然低覆盖，单阈值会逼迫无意义测试。

### D8. 迁移顺序

必须先端口化再拆项目（否则 Application 独立编译时无法引用具体适配器）。三个检查点各自 build + test 全绿：

```
CP0  端口 + 门面（单项目内）：D2/D3/D4，唯一语义改动阶段
CP1  Domain 提取：纯类型搬迁，编译器指路
CP2  Application/Infrastructure 提取 + 原项目改名 MacroTool.Web（D5）
```

`MacroTool.Web` 由原项目原地改名而非新建：git 历史连续、README 发布路径集中在一处修改。

## Risks / Trade-offs

- [CP0 引入行为回归（存档顺序、Reload 不替换引擎、尾部过滤、热键状态广播）] → 每个语义点都有现有测试覆盖；新增 `SaveTimeline`/`LoadTimelineFromDisk` 后跑全量测试，并人工核对 `Timeline.razor` 的 Snackbar 分支与 `_dirty` 状态流。
- [多项目命名空间搬迁造成大 diff 与潜在机械错误] → CP1/CP2 只做移动与 using 修正，不夹带语义改动；靠编译器报错收敛，每步全量测试。
- [coverage.ps1 解析 cobertura 失败或加权实现偏差] → 门禁脚本改动作为独立任务并实际运行一次验证；保留快速回退到旧脚本的可能（重构不阻塞构建）。
- [门面随需求增长变成 God Object] → 门面只做委托与状态暴露；任何新用例先进 Application 服务再暴露，禁止在 `MacroEngine` 内堆基础设施细节。
- [测试构造变化面广（UiTestScope、MacroEngineTests、EngineHostingIntegrationTests）] → 适配器实现端口后，测试可继续用真实 `TimelineStore`（临时目录）与真实 `HotkeyHost`（不 Start），改动集中在 using 与构造函数签名。
- [Windows-only 集成测试在非交互桌面失败] → 保持 `OperatingSystem.IsWindows()` 守卫与 no-op 约定不变。

## Migration Plan

1. **CP0**（单项目）：
   - 新增 Application 端口与结果类型；Infrastructure 侧先原地实现（文件夹 `Application/Interop`、`Application/Hotkeys`、`Application/Storage` 暂不动）。
   - `Recorder` 拆分（策略 + `IInputCaptureSource`，新增 `LowLevelHookCapture`）；`Player` 改用 `IInputSink`；删除 `PlayerDelegates`。
   - `MacroEngine` 门面扩展；四个页面与 `UiTestScope`/测试适配。
   - 验证：`dotnet build MacroTool.slnx` + `dotnet test MacroTool.slnx`。
2. **CP1**：新建 `MacroTool.Domain`，搬 `MacroEvent.cs`、`EngineStateMachine.cs`、`Editing/*`，改 using；验证同上。
3. **CP2**：新建 `MacroTool.Application`（用例+端口）与 `MacroTool.Infrastructure`（适配器），原项目改名 `MacroTool.Web`（`AssemblyName` 保持）；更新 slnx/测试引用/IVT；验证同上。
4. **收尾**：架构测试、`coverage.ps1` 汇总门禁、README/AGENTS.md 同步。
5. 回滚策略：无运行时数据迁移；任一步失败即还原该步提交，时间线文件与配置不受影响。

## Open Questions

- 架构测试是否在规则增多后迁移到 NetArchTest：当前仅一条规则，手写反射足够，可推迟。
- Infrastructure 是否长期保持 `net10.0`（跨平台构建）：本次保持，未来若引入 Windows 专属 API 再评估。
