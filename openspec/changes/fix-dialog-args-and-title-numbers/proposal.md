# Proposal: fix-dialog-args-and-title-numbers

## Why

时间线编辑页的三处 `ShowMessageBoxAsync` 调用把标题和正文传反了（MudBlazor 签名是 `(title, markupMessage)`），导致长句以大字标题呈现、短标题反而成了正文，观感混乱。同一截图中还暴露了第二个问题：对话框标题使用 Georgia（`DisplayFonts` 首选字体），其默认 old-style figures 让数字高低错落（如"4043 ms"中的 0 像 o），在数字为主的确认框里可读性差。

## What Changes

- 修正 `Timeline.razor` 三处 `ShowMessageBoxAsync` 调用（Trim / Scale / Reload）的参数顺序：短标题为第一参数，长文本为第二参数。
- 调整显示字体栈（`DisplayFonts` / `--font-display`），不再让 Georgia 打头，使标题中的数字为 lining figures（等高数字），如把 `Sitka Text` 提到首位或直接以 `Segoe UI` 打头；CSS 与 C# 两份定义同步修改。

## Capabilities

### New Capabilities

- `ui-dialog-confirmation`: 确认对话框的标题与内容呈现规则——标题为简短操作名，正文为说明长句；对话框与页面标题中数字以等高（lining）字形显示。

### Modified Capabilities

（无——项目尚无既有 spec。）

## Impact

- `MacroTool/Components/Pages/Timeline.razor`（三处对话框调用）
- `MacroTool/Components/Theme/MacroTheme.cs`（`DisplayFonts`）
- `MacroTool/wwwroot/css/app.css`（`--font-display`）
- 既有 UI 测试可能断言了对话框文本顺序，需相应核对（`MacroTool.Tests`）。
