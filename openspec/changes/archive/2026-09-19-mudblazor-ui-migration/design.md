## Context

动机见 `proposal.md - Why`。当前状态与约束（探索阶段已核实）：

- UI 已用 Mud 的部分：`MudButton`/`MudIconButton`、`MudSelect`、`MudTextField`、`MudNumericField`、`MudSwitch`、`MudDataGrid`、`MudIcon`、`MudText`、四个 Provider、`ShowMessageBoxAsync`/`ISnackbar`、`MacroTheme` 调色板。
- 裸 HTML + 自制 CSS：`app.css` 共 1124 行，其中顶栏、面板、12 栏网格、`.kv` 行、`.notice`、`.badge`、`.state-pill`、`.hints`、`.log-panel`、`.empty-state`、`.banner-recording`、`.tool-groups` 为手写；尾部约 180 行覆写 Mud 内部类（`.mud-input-outlined-border`、`.mud-button-root`、`.mud-switch-base` 等）。
- 测试与自定义类强耦合（`.state-pill`、`.dash-grid`、`.kv`、`.bar-field`、`.page`、`.nav-tabs`、`.page-head`、`.badge.is-warn`、`.log-panel`、`.mud-dialog*` 等约 40 处选择器）。
- MudBlazor 9.9.0 事实（已从包内 XML/CSS 核实）：`MudAppBar` 只有 `bool Fixed`（无 Position 枚举），基态 `position: relative`；`Fixed=true` 时为 `position: fixed`。`MudMainContent` 无条件 `padding-top: var(--mud-appbar-height)`，该值来自主题常量，不随实际高度变化。`MudToolBar` 支持 `WrapContent`（高度 auto + flex-wrap）。v9 无 `MudContainer`。`MudNavLink` 支持 `Href`/`Match`/`ActiveClass`，可独立使用。
- 布局现状：顶栏 `position: sticky` + backdrop blur，内容会换行、录制横幅会改变其下方布局；`LogViewer` 的 `Tall` 用 `calc(100vh - 230px)`。

## Goals / Non-Goals

**Goals:**

- 结构上使用 `MudLayout`/`MudAppBar`/`MudToolBar`/`MudMainContent`，同时保留吸顶、换行与动态高度行为。
- 在 Mud 有合适组件的区域完成替换，并删除对应自制 CSS。
- 通过类名兼容让 161 个既有测试尽量零改动地继续充当行为护栏。
- 把"优先 Mud 组件"写成仓库约定。

**Non-Goals:**

- 不采用 `Fixed=true` 的 Mud 默认固定顶栏，不为窄屏改单行布局。
- 不迁移 `.kv`、`.hints`、`Metric`、`.log-panel`、`ReconnectModal`（Mud 无对应组件，或替换后 CSS 不减少、语义更绕）。
- 不引入截图/Playwright 视觉回归设施；视觉验收为人工目检。
- 不改变任何行为、文案与 spec 语义；不做主题换肤或暗色模式。

## Decisions

### D1. 顶栏定位：`Fixed="false"` + CSS sticky

`MudAppBar` 在流内（基态 `position: relative`），用一条自定义规则 `.topbar { position: sticky; top: 0; backdrop-filter: blur(12px); }` 恢复吸顶；`MudMainContent` 覆写 `padding-top: 0`。

- 备选：`Fixed=true` + MudMainContent 自动补高——否决。补高值是主题常量，顶栏换行或录制横幅出现时高度变化，内容会被遮挡；窄屏则必须放弃换行。
- 备选：不吸顶（最纯 Mud）——否决。录制/回放控制在滚动后不可见，是 UX 退化。

### D2. 布局壳与组件边界

`MainLayout` 渲染 `MudLayout` → `MudAppBar`（`Fixed="false"`、`WrapContent="true"`、`ToolBarClass="bar-inner"`、`Gutters="false"`；`MudAppBar` 自带一层 `mud-toolbar-appbar`，**不再嵌套 `MudToolBar`**，否则外层固定高度会把换行内容挤出背景）→ `MudMainContent Class="page"`；`ControlBar` 保持"顶栏 + 录制横幅"两个根节点的结构，横幅留在 AppBar 之外（滚动语义不变）。页面 1240px 居中壳保留自制 `.page`（v9 无 `MudContainer`）。

### D3. 类名兼容迁移

迁移后的 Mud 组件统一用 `Class` 保留旧语义类，例如：

```razor
<MudAlert Class="notice is-warn" Severity="Severity.Warning" Dense="true">...</MudAlert>
<MudChip Class="badge is-warn" ...>未保存</MudChip>
<MudPaper Class="panel" Elevation="0">...</MudPaper>
```

测试选择器继续命中，迁移期间行为护栏不失效；若个别分组层数变化导致失配，做等价选择器替换，不放松断言。

### D4. 顶栏输入样式：吃 Mud 原生

`.bar-field` 内输入改用 Mud 原生 `Dense`/`Outlined`，删除下划线定制（约 60 行）。`Class="bar-field"` 包装保留（测试用 `.bar-field input`），宽度约束保留。

### D5. 导航：`MudNavLink`，保留回退路径

`MudNavLink` 支持 `Href`/`Match`/`ActiveClass`，用 `.nav-tabs` 包裹、以 `.nav-tabs .mud-nav-link` 收敛样式。若 ripple/内边距与 pill 设计冲突难以收敛，回退为现有 Router `NavLink`（不影响 L2 其余部分）。

### D6. 组件映射总表

| 现状 | 替换为 | 说明 |
|---|---|---|
| `.notice` / `.banner-recording` | `MudAlert` Dense + Severity | Info/Warning/Error 语义直接映射 |
| `.badge`、`.state-pill` | `MudChip`（+ 自制脉冲圆点） | Color 随状态；动画保留 |
| `.panel` / `.panel--flush` | `MudPaper` Elevation=0 | 纸面高光/边框保留少量 CSS |
| `.dash-grid` + `.col-N` | `MudGrid Spacing` + `MudItem xs/md` | 用 Mud 原生 flex 网格与断点；**不覆盖为 CSS Grid**（grid 区域会让 `MudItem` 的 `max-width:50%` 解析为半宽，面板被腰斩） |
| `.tool-groups` / `.tool-group` / `.tool-row` | `MudStack` | 列/行堆叠 |
| `.bar-sep` | `MudDivider Vertical` | |
| `.micro-label` / `.metric-label` | `MudText Typo=Typo.caption` | 自制区域内的标签保持原样 |
| `.nav-tab` | `MudNavLink` | 见 D5 |
| `.empty-state` 容器 | `MudPaper` + `MudText` | 图标/按钮已是 Mud |

### D7. 保留自制区域

品牌标识（`.brand*`）、日志终端（`.log-panel`/`.log-line`）、`Metric` 大数字、`.kv` 行、`.hints` 列表、`ReconnectModal`、`.page` 壳。理由：Mud 无对应组件，或替换后仍需等量 CSS 且语义更绕。

### D8. `LogViewer` 高度校准

`Tall` 模式的高度改为基于新壳的 `calc(100vh - var(--topbar-height) - <main 内边距>)`，用自定义变量表达，避免魔法数字；顶栏吸顶且在流内，实际滚动偏移与现状一致。

### D9. 主题与 CSS 收编

Mud 组件消费 `MacroTheme` palette；`app.css` 保留自定义 token 供自制区域使用。本轮"按需收编"，不强制把全部 CSS 变量迁入主题。CSS 清理按区域分批删除，每批跑测试 + 目检。

### D10. 验证与锁定

161 个测试为行为护栏；在 `ShellAndThemeTests` 增加少量断言（如渲染出 `.mud-appbar`、`.mud-main-content`），把"已迁移到 Mud 结构"锁定，防止无意回退。最终视觉由人工目检清单验收。

## Risks / Trade-offs

- [MudAppBar 动态高度与 `--mud-appbar-height` 常量补偿错位] → D1 选择在流内 sticky + padding 0，规避常量依赖；实现后分别检查顶栏换行、横幅出现/消失、窄屏三种状态。
- [Mud 组件包装层改变 DOM，bUnit 选择器失配] → D3 类名兼容；失配做等价替换；全量测试验证。
- [v9 内部类覆写与新组件样式冲突（如 `.mud-button-root` 位移影响 `MudSwitch`）] → 收窄覆写到残留自制区域，优先 Mud 原生表现；冲突点记录到目检清单。
- [无视觉回归测试] → 人工目检清单：吸顶/换行、横幅、状态芯片脉冲、日志面板高度、对话框标题与正文、禁用态、窄屏 760/1000/1100px。
- [`MudNavLink` 重样式成本高于预期] → D5 回退路径，不回滚其余部分。
- [CSS 清理误删自制区域引用] → 分批删除 + 每批测试 + 保留 token 区。

## Migration Plan

1. `MainLayout` 换壳（MudLayout/MudAppBar/MudToolBar/MudMainContent + sticky/padding 覆盖），ControlBar 内部不动 → build + 全量测试 + 目检。
2. `ControlBar` 组件化（AppBar/ToolBar/NavLink/Divider/Stack + 输入去下划线 + 横幅 MudAlert）。
3. 仪表盘与共享组件（Home/EnvHints/EngineStatusChip/Metric 容器）替换。
4. Timeline 页与 Logs（page-head/badge/notice/tool-groups/empty-state）。
5. CSS 清理 + `MacroTheme` 按需收编 + `LogViewer` 高度校准。
6. `AGENTS.md` 约定 + 最终 build/test + 人工目检。
7. 回滚策略：纯前端逐文件 revert；无数据/API 迁移。

## Open Questions

- `MudNavLink` 的样式收敛程度：实现时决定，不可行按 D5 回退。
- CSS 颜色 token 是否全量收编进 `MacroTheme`：本轮按需，留待后续观感统一时再评估。
