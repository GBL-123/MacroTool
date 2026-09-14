# Design: fix-dialog-args-and-title-numbers

## Context

MudBlazor `IDialogService.ShowMessageBoxAsync` 的签名为 `(title, markupMessage, yesText, cancelText, ...)`，第一参数是标题。`Timeline.razor` 三处调用（Trim / Scale / Reload）把长句传在第一参数，导致截图所见"长句变大标题、短名成正文"。字体方面，对话框标题走 MudBlazor Typography 的 H6、页面标题走 `.page-title`，两者最终都命中 `DisplayFonts` / `--font-display`，其首选字体 Georgia 只有 old-style figures（数字高低错落），且其系统版字形不含 lining figures，无法通过 font-feature 强制修正。C#（MacroTheme.cs）与 CSS（app.css:39）各有一份显示字体定义，分别服务于 MudBlazor 组件与自定义 CSS 类，需同步修改。

## Goals / Non-Goals

**Goals:**

- 三处确认对话框标题/正文归位，按钮文案不变。
- 显示字体栈中数字恒为等高（lining）字形，去掉 Georgia 首选。
- 两个字体定义（C# / CSS）保持一致。

**Non-Goals:**

- 不重构对话框调用方式（不引入 MessageBoxOptions 包装）。
- 不改动 `.mono` / `--font-mono` 等宽字体链（其 Cascadia 数字正常）。
- 不处理 UI 字体（Segoe UI 数字正常）。

## Decisions

1. **参数交换而非改用 options 对象**：三处调用只需交换前两个实参，改动最小、可读性不变。MudBlazor 的 options 对象重载适合复杂对话框，此处不必要。
2. **显示字体栈改为 `Sitka Text` 打头，彻底移除 Georgia**：`["Sitka Text", "Sitka", "Segoe UI", "Microsoft YaHei UI", "serif"]`。Sitka 为衬线、数字为 lining figures，观感最接近原 Georgia 的编辑体意图。备选"仅调序保留 Georgia 殿后"被否决——任何命中 Georgia 的环境都会重新出现 old-style 数字；备选"Georgia + font-variant-numeric: lining-nums"被否决——Georgia 系统版不含 lining 字形，该 CSS 无效。若目标机器无 Sitka，回退 Segoe UI（无衬线），数字依然正常，可接受。
3. **同步修改两份定义**：MacroTheme.cs 的 `DisplayFonts` 与 app.css 的 `--font-display` 同步换成同一栈，避免 drift。
4. **测试不按标题文本定位按钮**：现有对话框测试以 yesText/cancelText（"修剪"/"缩放"/"取消"）定位，参数交换不影响；仅在 Scale 测试补一条断言 `.mud-dialog-title` 文本为"时间缩放"，覆盖 spec 场景。

## Risks / Trade-offs

- [MudBlazor 版本差异导致参数名/顺序不同] → 实施前对照项目安装的 MudBlazor 包内 `IDialogService` 签名再动手；截图行为已实证当前版本第一参数为标题。
- [移除 Georgia 改变标题视觉基调] → Sitka 同为衬线编辑体，差异小；若用户日后想要 serif 以外的观感，仅改字体栈一处即可。
- [C# 与 CSS 两份定义未来可能再漂移] → 本次同栈落地，并在 tasks 中要求同时提交两处；不做进一步抽象（收益不抵引入共享常量/生成机制的复杂度）。

## Migration Plan

无数据或接口变更。全部为前端呈现层修改，随应用重启即生效；回滚 = revert 提交。

## Open Questions

无。
