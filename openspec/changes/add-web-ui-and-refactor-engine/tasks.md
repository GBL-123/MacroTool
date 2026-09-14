## 1. 工程骨架与配置

- [x] 1.1 新建 `MacroTool.Tests`（xUnit，引用 `MacroTool` 工程）并加入 `MacroTool.slnx`；验证：`dotnet build MacroTool.slnx` 成功
- [ ] 1.2 添加 `MacroOptions`（`Macro` 配置节：Speed/Jitter/TimelinePath/OpenBrowser）与 `appsettings.json` 的 `Urls`（`http://localhost:5047`）、`AllowedHosts` 收紧为 `localhost`；验证：构建成功，且无效配置回退默认值的行为有单测

## 2. 引擎重构（暂不接 UI）

- [x] 2.1 按 design 重组 `Application/` 为 Interop / Engine / Hotkeys / Storage / Editing / Hosting 并统一命名空间，删除控制台入口 `Start.cs` 与 `--speed/--jitter` 解析；验证：`dotnet build` 通过，全项目无 `Console.WriteLine`（原生/引擎层）
- [x] 2.2 落地 `EngineSettings`（运行时倍速/抖动，初值来自 `MacroOptions`）与 `MacroEngine` 状态机（开始/停止录制、开始/停止回放、状态与事件）；验证：构建通过，状态迁移与忽略规则有单测覆盖
- [x] 2.3 改造 `Recorder`：保留钩子线程模型，暴露点击/按键/时长进度，`Stop(bool dropTrailingMouse)` 实现 300ms 尾部鼠标事件过滤；验证：构建通过，过滤窗口的选取逻辑有单测
- [x] 2.4 改造 `Player`：每轮开始读取 `EngineSettings`（下一轮生效），完成轮次上报，停止时释放悬挂按键/鼠标；验证：构建通过
- [x] 2.5 改造 `HotkeyHost`：自持线程完成注册与消息泵，F12 降级 Ctrl+F12，F10/F11 失败时降级运行并暴露状态；验证：运行后 F10/F11/F12 生效，占用冲突场景手动验证
- [x] 2.6 改造 `TimelineStore`：DI 服务、`Macro:TimelinePath` 可配（默认 exe 目录）、保存前写单份 `.bak`、错误返回结果对象；验证：临时目录单测覆盖保存/备份/损坏文件加载
- [x] 2.7 新增 `MacroHostedService` 与退出流程：启动热键、停止录制（有事件则保存）、停止回放、注销钩子；验证：构建通过，`关闭服务`/F12 退出后进程结束且无挂起
- [x] 2.8 实现 `TimelineEditing` 纯函数（动作对配对、删除、按起点修剪、平移钳制、缩放、升序重排、`DurationMs = max(T)`）与颠倒配对警告；验证：xUnit 覆盖各操作与不变量，全部通过
- [x] 2.9 `Program.cs` 装配：`SetProcessDPIAware` 置顶、DI 注册、`MacroHostedService`、移除 HTTPS 重定向、静态资源与 InteractiveServer；验证：`dotnet run` 启动无异常，日志显示热键注册结果
- [x] 2.10 `BrowserLauncher`：`ApplicationStarted` 按 `Macro:OpenBrowser` 打开默认浏览器，`appsettings.Development.json` 关闭以避免与 launchSettings 重复；验证：发布运行自动打开，开发运行不重复打开

## 3. 控制面板 UI

- [x] 3.1 `MainLayout` 接入 `MudThemeProvider` / `MudPopoverProvider` / `MudDialogProvider` / `MudSnackbarProvider` 与 Drawer 导航、全局状态条；验证：页面无 Mud 运行时错误，跨页面状态可见
- [x] 3.2 仪表盘（`Home.razor` 替换模板内容）：状态卡、开始/停止录制与回放、全部停止、实时计数（500ms 拉取）、可回放性与禁用逻辑；验证：手动走查空闲/录制/回放三态按钮可用性
- [x] 3.3 参数面板：倍速/抖动校验与提示、回放中改动下一轮生效；验证：手动验证非法值被拒、2 倍速在下一轮体现
- [x] 3.4 日志流：`LogBuffer` 注入、时间戳与级别、自动滚动、新标签页显示历史（≥100 条）；验证：双标签页同时观察，引擎事件约 1 秒内出现
- [x] 3.5 环境提示：退出热键与注册状态、未提权提示、使用须知（窗口不可移动/窗口化/管理员/反作弊）；验证：手动走查，制造热键冲突时出现警告
- [x] 3.6 关闭服务：确认对话框接入引擎退出流程；验证：确认后进程退出，取消后状态不变

## 4. 时间线编辑器

- [x] 4.1 动作对表格（`MudDataGrid` 虚拟化）：类型/起点/时长/详情、展开查看原始 down/up、未配对标记、VK→键名映射（常用键，冷门显示 `VK 0x..`）、概要（动作与事件数、总时长）；验证：加载含未配对事件的时间线手动核对
- [x] 4.2 删除整对与删除未配对项，列表与概要即时更新并标记未保存；验证：手动删除后与 `TimelineEditing` 单测结果一致
- [x] 4.3 头尾修剪：确认框显示将移除的动作数量、按动作起点判定、绝不拆对；验证：手动验证边界（按下在区间内、抬起在区间外）
- [x] 4.4 平移/缩放 UI：全部或选中、负时间钳制到 0、编辑后重排与总时长重算、颠倒警告；验证：手动对照单测预期
- [x] 4.5 保存/备份/重新载入：保存写主文件并留 `.bak`、失败提示可重试、重载二次确认丢弃未保存更改；验证：手动检查 `.bak` 内容为保存前版本
- [x] 4.6 仅空闲可编辑：录制/回放中禁用编辑并说明原因，空闲恢复；验证：录制中与回放中手动走查
- [x] 4.7 保存后引擎立即生效：面板提示未保存时回放仍用旧时间线；验证：保存后按 F11 回放的是编辑后内容

## 5. 集成验证与文档

- [ ] 5.1 手工冒烟：录制透传、F10 停止不过滤、面板停止过滤按钮点击、循环回放与倍速/抖动、关闭全部浏览器后录制/回放继续、退出时保存录制；验证：逐项记录结果
- [x] 5.2 全量测试：`dotnet test MacroTool.slnx` 全部通过
- [x] 5.3 README 改写为浏览器控制面板用法：启动与 URL、热键、appsettings 配置、编辑器与 `.bak`、移除的 `--speed/--jitter`、原有使用须知保留；验证：按 README 从零启动走通
- [x] 5.4 发布构建：`dotnet build MacroTool.slnx -c Release` 与 `dotnet publish` 通过；验证：发布产物运行可用
