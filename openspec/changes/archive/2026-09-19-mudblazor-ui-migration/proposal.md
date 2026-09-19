## Why

引入 MudBlazor 的初衷之一是尽量复用其组件，但当前 UI 仍是"Mud 控件 + 约 1100 行自制 CSS"的混合体：布局、顶栏、提示条、徽章、面板、网格等都是手写 HTML/CSS，其中约 180 行还在覆写 Mud 内部类。项目结构重构完成后组件已稳定、161 个测试可作为行为护栏，正是把 UI 收编到 MudBlazor 组件体系的时机。

## What Changes

- **结构性替换（L2）**：`MainLayout` 改为 `MudLayout` + `MudAppBar` + `MudToolBar` + `MudMainContent`；采用 `Fixed="false"` + 一行 CSS `position: sticky` 保留现有吸顶行为，`MudMainContent` 的 `padding-top` 覆写为 0 以适配动态高度顶栏；录制横幅保持在 AppBar 之外（含义与滚动行为不变）。
- **组件替换（L1）**：提示条/录制横幅 → `MudAlert`；徽章与状态芯片 → `MudChip`；面板 → `MudPaper`；仪表盘 12 栏网格 → `MudGrid`/`MudItem`；编辑器工具组 → `MudStack`；顶栏分隔线 → `MudDivider`；小标签 → `MudText Typo=Typo.caption`；路由导航 → `MudNavLink`（Href/Match/ActiveClass）；顶栏输入框改用 Mud 原生 Dense/Outlined 样式，删除下划线覆写。
- **类名兼容**：迁移后的 Mud 组件通过 `Class` 保留旧语义类（`notice`、`state-pill`、`dash-grid`、`bar-field`、`page`、`nav-tabs` 等），既有测试选择器继续命中，测试作为行为护栏尽量零改动。
- **CSS 收敛**：`app.css` 删除被组件替代的规则（预计 300~400 行），重新整理剩余的 Mud 内部类覆写；`LogViewer` 的 `100vh` 高度按新壳重新校准。
- **文档**：`AGENTS.md` 增加 UI 约定——优先使用 MudBlazor 组件，自制 CSS 仅用于品牌标识、日志终端等无对应组件的区域。
- 非目标：不改变任何用户可见行为与 spec 语义；不采用 Mud 默认固定顶栏（`Fixed=true`）；不迁移 `.kv` 行、`.hints` 列表、Metric 大数字、日志终端与 ReconnectModal；不引入截图/Playwright 视觉回归测试。
- **BREAKING**：无（纯前端实现重构，无 API、配置或行为变更）。

## Capabilities

### New Capabilities

（无）

### Modified Capabilities

（无——不改变任何 spec 级行为，仅替换 UI 实现。按 OpenSpec 约定在 `.openspec.yaml` 设置 `skip_specs: true` 显式跳过 delta spec，不伪造需求变更。）

## Impact

- 代码：`MacroTool.Web/Components/`（MainLayout、ControlBar、Home、Timeline、EnvHints、LogViewer、EngineStatusChip、Metric、Logs）、`MacroTool.Web/wwwroot/css/app.css`、`MacroTool.Web/Components/Theme/MacroTheme.cs`（按需收编变量）。
- 测试：`MacroTool.Tests` 的 bUnit 选择器基本不动（类名兼容）；如个别 Mud 组件包装层数变化导致选择器失配，仅做等价替换，不放松断言。
- 文档：`AGENTS.md` 增补 UI 约定；`README.md` 无需改动（行为与截图性描述未变）。
- 风险：无自动化视觉回归，最终外观需人工目检；MudAppBar 动态高度与 `--mud-appbar-height` 常量的补偿关系需要按设计文档校准。
