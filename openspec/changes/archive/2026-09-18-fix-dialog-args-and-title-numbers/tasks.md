# Tasks: fix-dialog-args-and-title-numbers

## 1. 对话框参数归位

- [x] 1.1 对照项目已安装 MudBlazor 包确认 `IDialogService.ShowMessageBoxAsync` 前两参数为 `(title, markupMessage)`，然后交换 `Timeline.razor` 三处调用（Trim / Scale / Reload）的前两个实参：标题在前（"修剪时间线" / "时间缩放" / "重新载入"），说明长句在后。验证：`dotnet build` 通过，三个确认框弹出时短名在大标题、长句在正文。

## 2. 显示字体栈去 Georgia

- [x] 2.1 将 `MacroTheme.cs` 的 `DisplayFonts` 改为 `["Sitka Text", "Sitka", "Segoe UI", "Microsoft YaHei UI", "serif"]`。验证：`dotnet test` 中主题相关测试（含 `ShellAndThemeTests` 的 FontFamily 断言）通过。
- [x] 2.2 将 `app.css:39` 的 `--font-display` 同步改为同一字体栈。验证：浏览器中 page-title 与对话框标题无 Georgia 渲染，数字等高（DevTools computed font-family 为 Sitka Text 或回退 Segoe UI）。

## 3. 测试覆盖与回归

- [x] 3.1 在 `TimelinePageTests.Scale_WithConfirmDialog_HalvesTimes` 中补断言：弹出对话框的 `.mud-dialog-title` 文本为"时间缩放"，说明句位于标题之外。验证：`dotnet test` 全绿。
- [x] 3.2 全量回归：`dotnet test`，确认既有 Trim/Reload 对话框测试与全部 UI 测试不受参数交换与字体调整影响。（期间经用户确认删除了必然失败的调试遗留 `ScratchMarkupTests.Dump_Switch_Markup`，属上一 change 的遗留物；最终 140/140 通过）

## 4. 验证 spec 场景

- [x] 4.1 手工核对 `ui-dialog-confirmation` spec 场景：三个确认框标题/正文归位；对话框标题与 page-title 中数字（如有）高度一致、"0"不呈 o 形。验证：对照 spec 场景逐条通过。（用户已手工核对确认）
