## 1. 布局壳（L2 骨架）

- [x] 1.1 `MainLayout.razor` 用 `MudLayout` 包裹 `ControlBar` 与 `MudMainContent Class="page"`（`MudMainContent` 的 `padding-top` 覆写为 0）；验证 build 成功且全量测试通过
- [x] 1.2 `app.css` 为 `MudAppBar` 加 sticky 覆盖（`position: sticky; top: 0;` 保留 backdrop blur），顶栏仍在文档流内；验证吸顶行为与改造前一致（滚动页面目检）
- [x] 1.3 在 `ShellAndThemeTests` 增加结构锁定断言（渲染出 `.mud-appbar`、`.mud-main-content`）；验证该测试类通过

## 2. ControlBar 顶栏组件化

- [x] 2.1 `header.topbar` 换为 `MudAppBar Fixed="false" Class="topbar"`，内部 `.bar-inner` 换为 `MudToolBar WrapContent="true" Class="bar-inner"`；验证 `ControlBarTests` 通过（保留 `.topbar/.bar-inner` 类）
- [x] 2.2 `.bar-sep` 换 `MudDivider Vertical`、指标与参数分组换 `MudStack`（保留 `.bar-metrics`/`.bar-params`/`.bar-field` 类）；验证 `ControlBarTests` 的 `.bar-metrics`、`.bar-field input` 选择器继续命中
- [x] 2.3 导航换 `MudNavLink`（`Href`/`Match`/`ActiveClass`，`.nav-tabs` 包裹）；验证 `ControlBarTests` 的 `.nav-tabs` 文本断言通过；若样式收敛成本过高按 D5 回退为 Router `NavLink` 并在提交说明中记录
- [x] 2.4 `.bar-field` 输入改 Mud 原生 `Dense`/`Outlined`，删除下划线覆写；验证 `ControlBarTests`（`.bar-field input`、`MudSelect`）通过并目检输入框外观
- [x] 2.5 录制横幅换 `MudAlert Class="banner-recording banner-in-frame"`（Severity 映射），保持在 AppBar 之外；验证录制中横幅出现的位置与滚动行为不变（目检）

## 3. 仪表盘与共享组件

- [x] 3.1 `Home.razor`：`.dash-grid`/`.col-*` 换 `MudGrid`/`MudItem`、`.panel` 换 `MudPaper Class="panel"`、`.notice` 换 `MudAlert`；验证 `OverviewPageTests` 通过（`.dash-grid`/`.kv`/`.metrics`）
- [x] 3.2 `EnvHints.razor`：`.panel`/`.notice` 换 `MudPaper`/`MudAlert`，`.kv`/`.hints` 保持自制；验证 `SharedComponentTests`、`ShellAndThemeTests` 通过
- [x] 3.3 `EngineStatusChip.razor` 换 `MudChip Class="state-pill"`（Color 随状态，保留 `state-dot` 脉冲动画）；验证 `SharedComponentTests`、`ControlBarTests` 的 `.state-pill` 断言通过
- [x] 3.4 `Metric.razor` 的标签/数值改用 `MudText` Typo 表达（保留 `.metric-label`/`.metric-value` 类），容器布局保留；验证 `SharedComponentTests` 通过

## 4. 编辑器与日志页

- [x] 4.1 `Timeline.razor`：`.page-head` 换 `MudText` Typo（保留 `.page-head`）、`.badge` 换 `MudChip`、`.notice` 换 `MudAlert`、`.tool-groups/.tool-group/.tool-row` 换 `MudStack`、空态容器换 `MudPaper`；验证 `TimelinePageTests` 全部通过（`.page-head`/`.badge.is-warn`/`.notice.is-warn`/`.tool-groups`）
- [x] 4.2 `Logs.razor` 头部与 `LogViewer` 外壳换 `MudPaper`/`MudText`，`.log-panel` 终端保持自制；`Tall` 高度改用变量化 `calc()`；验证 `ShellAndThemeTests`（`.log-panel`）通过并目检高度

## 5. CSS 清理与主题收编

- [x] 5.1 分批删除 `app.css` 中被组件替代的规则（topbar/panel/notice/badge/grid/tool-groups/下划线覆写等），保留品牌、日志终端、Metric、`.kv`、`.hints` 与 token 区；每批验证 build + 全量测试通过
- [x] 5.2 剩余 Mud 内部类覆写收窄到必要范围（`mud-button-root`/`mud-switch-base` 等冲突点复核），`MacroTheme` 按需收编被替换区域的 palette/typography；验证全量测试通过并目检按钮/开关/输入/表格
- [x] 5.3 记录清理前后 `app.css` 行数变化作为收尾交付物；确认无死规则残留（对删除的类名做一次全仓引用扫描）

## 6. 文档与最终验证

- [x] 6.1 `AGENTS.md` 增加 UI 约定：优先 MudBlazor 组件，自制 CSS 仅限品牌/日志终端等无对应组件区域；验证约定段落存在且与设计一致
- [x] 6.2 `README.md` 无需改动（行为与截图性描述未变）；核对发布/使用文档中无被本次改造影响的描述
- [x] 6.3 最终验证：`dotnet build MacroTool.slnx`、`dotnet test MacroTool.slnx` 全绿；人工目检清单（吸顶/换行/窄屏 760/1000/1100px、录制横幅、状态芯片脉冲、日志面板高度、对话框标题与正文、禁用态、12 栏响应式）
