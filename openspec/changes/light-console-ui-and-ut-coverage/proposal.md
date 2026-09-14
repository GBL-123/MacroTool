## Why

控制面板当前是暗色主题 + 网格布局，用户需要浅色主题并把布局重构为"控制条"（走带控制隐喻）。同时 UT 工程已被重建为 xunit.v3（MTP）+ bUnit + Moq + CodeCoverage 新框架（旧测试文件已被清除，且指向主工程的 ProjectReference 丢失），需要重建测试资产并把 MacroTool 主程序集行覆盖率提升到 ≥ 75%。

## What Changes

- **BREAKING**: 移除暗色主题，全面切换为浅色（暖骨白画布 + 白卡片 + 1px 细边 + 超扁平阴影 + 淡彩状态色）；不提供主题切换
- 布局重构（控制条方向）：sticky 顶部控制条整合品牌、导航标签、状态药丸、走带按钮组（录制/回放/停止）、录制实时指标、运行参数与关闭服务；移除左侧抽屉与大状态卡
- 内容区全宽路由化：概览（指标 + 时间线摘要 + 环境与热键）、时间线编辑器、日志三个导航目标；表格与日志获得全宽
- UT 基建：补回 `MacroTool.Tests` → `MacroTool` 的 ProjectReference；在 xunit.v3 + bUnit + Moq 框架下重建并扩充测试
- 可测性重构（公共行为不变）：Recorder 钩子回调拆出事件处理纯方法；Player 回放循环的输入注入抽为可注入委托（默认仍为真注入）
- 覆盖率门禁：`Microsoft.Testing.Extensions.CodeCoverage` 收集 + `ReportGenerator` 出 HTML 报告，MacroTool 主程序集行覆盖 ≥ 75%，验收命令可重复执行

## Capabilities

### New Capabilities
<!-- 本变更为视觉呈现与测试资产变更，不改变任何行为契约：控制面板现有各 requirement
     在新布局与浅色主题下全部继续成立，故声明 skip_specs: true，不发明假需求。 -->
（无 —— 本 change 通过 `.openspec.yaml` 声明 `skip_specs: true`）

### Modified Capabilities
（无）

## Impact

- 受影响代码：`MacroTool/Components/*`（MainLayout、Home、Timeline、Shared/*、Theme/MacroTheme）、`MacroTool/wwwroot/css/app.css`、`MacroTool/wwwroot/favicon.svg`、`MacroTool/Application/Engine/Recorder.cs` 与 `Player.cs`（仅内部结构调整）、`MacroTool.Tests/*`（重建）
- 不受影响：引擎公共行为与状态机语义、`timeline.json` 格式、热键语义、退出流程
- 依赖：沿用已还原的 bUnit 2.11.3、xunit.v3（mtp-v2）、Moq、Microsoft.Testing.Extensions.CodeCoverage、ReportGenerator；不新增运行时依赖
- 验收：主程序集行覆盖 ≥ 75%（以 ReportGenerator 报告为准）
