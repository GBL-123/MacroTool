## 1. UT 基建

- [x] 1.1 在 `MacroTool.Tests.csproj` 补回对 `MacroTool` 的 ProjectReference；验证：`dotnet build MacroTool.slnx` 成功
- [x] 1.2 建立覆盖率命令与报告产出（CodeCoverage 收集 + ReportGenerator HTML，`coverage.ps1`）；验证：能生成报告并读出主程序集行覆盖率数字

## 2. 可测性重构（公共行为不变）

- [x] 2.1 Recorder：钩子回调拆出 `ProcessKey`/`ProcessMouse` 纯方法（去重、计数、入队、F10/F11/F12 排除全在纯方法内），回调只做封送，`Stop()` 无钩子时也能取出队列；验证：构建通过 + 纯方法单测覆盖去重/排除/计数
- [x] 2.2 Player：回放循环的输入注入抽为可注入委托（`PlayerDelegates`，默认仍走 InputSender 真注入），`MacroEngine` 增加可选直通参数；验证：构建通过 + 注入委托单测跑完整时间线（轮次上报、悬挂按键/鼠标释放、注入失败事件）

## 3. 测试重建与覆盖

- [x] 3.1 移植纯逻辑测试到 xunit.v3：MacroOptions.Normalize、EngineStateMachine、EngineSettings、LogBuffer、KeyNames、MacroEvent/Timeline、Recorder 尾部过滤、TimelineEditing 全操作与不变量；验证：全部通过
- [x] 3.2 TimelineStore 文件 I/O 测试（临时目录：保存/加载往返、`.bak` 备份、损坏文件、路径解析）；验证：全部通过
- [x] 3.3 MacroEngine 局部测试（UpdateSettings 校验、ReplaceTimeline 于 Idle/忙时、GetSnapshot、经注入委托进入回放态）；验证：全部通过
- [x] 3.4 bUnit 组件测试：控制条走带按钮可用性（状态驱动禁用）、参数校验与 Apply、LogViewer 快照与订阅、EngineStatusChip/EnvHints/Metric 渲染；验证：全部通过
- [x] 3.5 bUnit 页面测试：概览页渲染、时间线编辑器渲染与编辑交互（删除/修剪确认/平移/缩放/保存至临时目录，不点击会安装钩子的按钮）；验证：全部通过
- [x] 3.6 覆盖率门禁：MacroTool 主程序集行覆盖 ≥ 75%（以 ReportGenerator 报告为准）；验证：报告数字达标，不足时按报告补测，不注水

## 4. 浅色主题

- [x] 4.1 `MacroTheme` 切换为 PaletteLight（对应取色），token 表在 `app.css` 整体替换为浅色值（画布/面板/文字/淡彩状态色/强调黑）；验证：构建通过，页面无残留暗色变量
- [x] 4.2 组件样式适配：状态药丸、badge、notice、日志面板、录制横幅、DataGrid 表头/行、对话框与 Snackbar 浅色观感；验证：CDP 截图核对主要状态
- [x] 4.3 favicon 与 `theme-color` meta 更新为浅色基调；验证：发布运行查看图标

## 5. 控制条布局（方向 B）

- [x] 5.1 `MainLayout` 重构为 sticky 控制条：品牌+导航标签 | 状态药丸+指标组 | 走带按钮组 | 参数（倍速/抖动/应用） | 关闭服务，分段分隔线；验证：三条路由下控制条一致且常驻
- [x] 5.2 移除左侧抽屉与大状态卡；`Home` 改为概览页（指标 + 时间线摘要 + 环境与热键），新增 `/logs` 日志页；验证：路由切换正常，旧组件清理干净
- [x] 5.3 窄窗口分段换行策略（< ~1100px 按段换行，段内不拆）；验证：窄视口截图核对
- [x] 5.4 全流程 CDP 截图验证（浅色控制条、全宽编辑器、录制态警示横幅与实时计数、日志页）；验证：截图核对无回归

## 6. 最终验收

- [x] 6.1 全量测试与构建：`dotnet test MacroTool.slnx` 全部通过，`dotnet build -c Release` 0 警告；验证：命令输出
- [x] 6.2 覆盖率与发布产物：覆盖率报告 ≥ 75% 复核 + `dotnet publish` 运行可用（浅色界面、控制条、编辑器）；验证：报告与运行截图
