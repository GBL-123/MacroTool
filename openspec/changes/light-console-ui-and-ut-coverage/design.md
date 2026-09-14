## Context

动机与范围见 proposal.md。形状约束：

- `MacroTool.Tests` 已被重建为 xunit.v3（`xunit.v3.mtp-v2` + `UseMicrosoftTestingPlatformRunner`）+ bUnit 2.11.3 + Moq + `Microsoft.Testing.Extensions.CodeCoverage` + `ReportGenerator`，但缺少指向 `MacroTool` 的 ProjectReference，测试只剩模板桩
- 现有 UI：暗色 token（`wwwroot/css/app.css`）+ `MudTheme`（PaletteDark）+ 12 列网格 + 左侧抽屉；组件已 token 化，换主题主要是换值 + 少量类名调整
- 主程序集约 2580 行；原生封送（Native/InputSender/Player 回放循环/Recorder 钩子线程/Program 顶层语句）约 350 行执行行事实上不可测，不重构的行覆盖理论上限约 77%
- 引擎公共行为契约（macro-engine / control-panel / timeline-editor deltas，在既有 change 中）不因本 change 改变

## Goals / Non-Goals

**Goals:**

- 浅色主题全面替代暗色（token 换值 + MudTheme PaletteLight），无主题切换
- 布局重构为 sticky 控制条（方向 B）：控制/参数/状态常驻顶部，内容区全宽
- 主程序集行覆盖 ≥ 75%，报告可重复生成
- 为可测试性做两处内部重构，公共行为不变

**Non-Goals:**

- 不做主题切换/持久化；不改引擎公共 API 与行为契约；不动 `timeline.json` 格式
- 不追求分支覆盖率；不测真实钩子安装与 SendInput 注入（人工冒烟覆盖）
- 不为覆盖率而测 `Program.cs` 顶层语句与原生 P/Invoke 声明

## Decisions

### 1. 布局：sticky 控制条（方向 B）

```
+------------------------------------------------------------------------------------------+
| M MacroTool  [ 概览 | 时间线 | 日志 ]   ● 状态  走带(录制/回放/停止)  指标组  参数(倍速/抖动/应用) [⏻] |
+------------------------------------------------------------------------------------------+  <- sticky, 分段分隔线
|  (录制警示横幅，仅录制时出现)                                                                |
|  路由内容区（max-width 容器，全宽）                                                          |
|    /        概览：指标 + 时间线摘要 + 环境与热键                                              |
|    /timeline 时间线编辑器：工具栏 + 全宽动作对表格                                            |
|    /logs    日志：全宽终端面板                                                               |
+------------------------------------------------------------------------------------------+
```

- 分段用细竖线分隔：品牌+导航 | 状态+指标 | 走带 | 参数 | 关闭；窄窗口（< ~1100px）按段换行，段内不拆
- 走带按钮按状态切换可用性（与现逻辑一致）；录制指标为 mono 小计数器组（点击/按键/已录；回放时轮数/已播）
- 大状态卡、左侧抽屉、ParameterPanel/EngineStatusChip 独立组件并入控制条后移除或降级为条内片段
- 备选（已否决）：工作台左栏（ stole 表格 20% 宽度，导航目标只有 3 个，竖栏价值低）

### 2. 浅色 token（minimalist-ui 纸感为基底）

| Token | 值 |
|---|---|
| 画布 | `#F7F6F3`；内容区 Radial 微光（暖，opacity ≤ 0.03）+ 颗粒噪点保留 |
| 面板 | `#FFFFFF`，`1px solid #E9E8E4`，radius 12/8px，阴影几乎无（hover `0 2px 8px rgba(0,0,0,0.04)`） |
| 正文/次要 | `#2F3437` / `#787774`（非纯黑） |
| 强调（主按钮/焦点环） | 黑 `#111111` 实底白字，hover `#333` / active scale(0.98)；琥珀仅作小面积点缀（badge/焦点） |
| 状态 | 录制 `#FDEBEC` 底 + `#9F2F2D` 字；回放 `#EDF3EC` + `#346538`；警示 `#FBF3DB` + `#956400`；信息 `#E1F3FE` + `#1F6C9F`（淡彩底+深字） |
| 字体 | 标题衬线 `Georgia, 'Sitka Text', serif`（拉丁）+ CJK 回退 YaHei；正文 `Segoe UI Variable/Segoe UI`；数据 `Cascadia Mono` + tabular-nums |
| 日志面板 | 纸面反色改浅灰底 `#FAFAF8` + 细边，级别色同状态淡彩系 |
| `MudTheme` | `PaletteLight` 对应取色，`IsDarkMode=false`；Dialog/Snackbar/Tooltip 跟随浅色 |

旧暗色 CSS 直接被新 token 表替换（同名变量，组件类名基本不动）。

### 3. 测试矩阵与框架映射

| 层 | 内容 | 框架 |
|---|---|---|
| 纯逻辑 | MacroOptions.Normalize、EngineStateMachine、EngineSettings、LogBuffer、KeyNames、MacroEvent/Timeline、Recorder 尾部过滤、TimelineEditing 全操作与不变量 | xunit.v3 `[Fact]`/`[Theory]` |
| 文件 I/O | TimelineStore 保存/备份/损坏文件/路径解析（临时目录） | xunit.v3 |
| 引擎局部 | MacroEngine.UpdateSettings 校验、ReplaceTimeline（Idle）、GetSnapshot | xunit.v3（真实协作者，临时目录） |
| 回放循环 | Player 以注入委托跑完整时间线：时序近似、轮次上报、悬挂释放、注入失败事件 | xunit.v3 + 委托桩 |
| 录制处理 | Recorder 拆出的 ProcessKey/ProcessMouse 纯方法：去重、计数、F10/F11/F12 排除 | xunit.v3 |
| 组件 | EngineStatusChip、ParameterPanel 校验与 Apply、LogViewer 快照+订阅、EnvHints、Metric、控制条渲染与走带可用性 | bUnit（JSInterop 用 bUnit 模拟） |
| 页面 | 概览页渲染、时间线编辑器渲染与编辑交互（删除/修剪确认/平移/缩放/保存到临时目录），不点击会安装钩子的按钮 | bUnit + Moq/真实引擎 |

bUnit 侧用真实 `MacroEngine` 实例（TimelineStore 指向临时目录），构造仅触一次 `IsUserAnAdmin`；不调用 `Start()`、不点击"开始录制/回放"。

### 4. 可测性重构（内部，公共行为不变）

```
Recorder:
  钩子回调(封送) --拆出--> internal ProcessKey(int vk,int scan,bool ext,bool up)
                                  internal ProcessMouse(int msg,int x,int y)
  回调仅做 PtrToStructure 后转发；去重/计数/入队/F键排除全在纯方法内
  Stop() 在未 Start 时也取出队列（测试与引擎行为一致：无钩子时返回已入队事件）

Player:
  ctor(MacroTimeline, EngineSettings, PlayerDelegates? delegates = null)
  默认 null => 走 InputSender 真注入；测试注入计数委托
  MacroEngine ctor 增加可选 PlayerDelegates? 直通参数（默认 null），
  使 bUnit/单测可经 StartPlayback 进入回放态而不触发真注入
```

### 5. 覆盖率工具链与验收命令

```
dotnet test MacroTool.Tests --coverage --coverage-output-format cobertura --coverage-output coverage.cobertura.xml
ReportGenerator -reports:**/coverage.cobertura.xml -targetdir:coveragereport -reporttypes:Html
```

- 口径：`MacroTool` 主程序集 line coverage ≥ 75%（ReportGenerator Summary 折算）；测试工程不计入
- 已知盲区（预期不覆盖）：`Program.cs` 顶层、`Native.cs` 封送、`InputSender`、`Player` 真注入分支、`HotkeyHost` 线程泵、`BrowserLauncher.OpenBrowser` 成功路径
- 若总结算低于 75%：优先补 bUnit 交互路径与编辑器分支，不注水测试

## Risks / Trade-offs

- [bUnit 与 xunit.v3/MTP 组合的兼容边角] → 已验证还原与构建；首个组件测试先行验证渲染管线，再铺量
- [Home/概览页 500ms 轮询 Timer 在 bUnit 中不稳定] → 组件内定时器保留，测试只断言渲染与事件处理，不依赖轮询时序
- [razor 生成代码的覆盖统计口径] → 以 ReportGenerator 对程序集的汇总为准，不等价于源码行数
- [75% 压线] → Recorder/Player 重构提供主要增量；仍不足时按报告定位未覆盖分支补测
- [控制条在窄窗口拥挤] → 分段换行策略 + 关键操作优先；编辑器表格获得横向滚动兜底
- [移除大状态卡后录制状态显著性下降] → 状态药丸 + 走带按钮变色 + 警示横幅三层冗余

## Migration Plan

- 无数据迁移；暗色 token 文件被直接替换，回滚 = 回退提交
- 实施顺序：UT 基建（引用 + 工具链）→ 可测性重构 → 测试重建与覆盖 → 浅色主题 → 控制条布局 → 门禁验收

## Open Questions

- 衬线标题在纯 CJK 环境的观感（Georgia 只覆盖拉丁）——实现时以实际渲染微调字重与字距，不影响结构
