# AGENTS.md

Windows-only macro recorder (mouse/keyboard record & replay for games) with a localhost Blazor Server web console. Solution: `MacroTool.slnx` → `MacroTool.Domain/` (pure model/logic), `MacroTool.Application/` (use cases + ports), `MacroTool.Infrastructure/` (Win32/file adapters), `MacroTool.Web/` (Blazor UI + composition root, assembly name `MacroTool`) + `MacroTool.Tests/`. Requires .NET SDK 10.0.400+. `README.md` (Chinese) is the authoritative product doc (hotkeys, config, timeline format, publish commands).

## Commands

```powershell
dotnet build MacroTool.slnx
dotnet test MacroTool.slnx
dotnet test MacroTool.Tests\MacroTool.Tests.csproj --no-build -- --filter-class "MacroTool.Tests.EngineStateMachineTests"
.\coverage.ps1   # build + tests with coverage; gates aggregate line coverage of the 4 product assemblies at 75% (report: coverage/report/index.html)
```

- **Builds fail with MSB3027 if the app is running** — `MacroTool.exe` locks its own apphost. Ask the user to close the running instance (or stop it via the web panel power button) before building.
- `global.json` does **not** pin the SDK; it only selects the Microsoft.Testing.Platform (MTP) runner for `dotnet test`. Tests run in-process as an exe (`OutputType=Exe`), so `dotnet test` and running `MacroTool.Tests\bin\Debug\net10.0\MacroTool.Tests.exe` both work.
- MTP args go after `--` in `dotnet test` (`--filter-class`, `--filter-method`, `--filter-namespace`, `--filter`, `--list-tests`).
- On this machine, adding `--nologo` to `dotnet test` makes the MTP runner report zero tests (exit 5); omit it for test runs.
- No CI, no lint/format config — verification is build + tests + `coverage.ps1`.

## Tests

- xUnit v3 + Moq + bUnit; tests exercise app internals via `InternalsVisibleTo` (Web + Infrastructure). 161 tests, ~2s.
- UI tests use `UiTestScope` (`MacroTool.Tests/UiTestScope.cs`): bUnit context with the real engine/store/log/hotkey singletons wired to a temp dir, JSInterop in Loose mode. Reuse it rather than building DI by hand.
- `EngineHostingIntegrationTests` hits real Windows APIs:
  - `Recorder_StartStopWithoutInput_ProducesEmptyTimeline` installs a real low-level input hook for ~120ms — any stray keypress/mouse event (e.g. the user typing) fails it. Rerun before debugging.
  - `HostedService_StartAndStop_BootsHotkeysAndLogsStatus` briefly registers global hotkeys F10/F11/F12; registration fails silently while the app instance is running (the test tolerates that).
- Both are guarded by `OperatingSystem.IsWindows()` and no-op elsewhere.

## Architecture

- Dependencies point inward and are enforced by project references: `Domain` (zero deps) ← `Application` (use cases + ports) ← `Infrastructure` (adapters); `Web` is the only composition root, and `MacroTool.Tests/ArchitectureTests.cs` guards that `MacroTool.Web.Components.*` never touches `MacroTool.Infrastructure`.
- `MacroTool.Web/Program.cs` is the only wiring point: registers `LogBuffer`, `EngineSettings`, and the ports (`IGlobalHotkeys` → `HotkeyHost`, `ITimelineRepository` → `TimelineStore`, `IInputSink` → `Win32InputSink`, `Func<IInputCaptureSource>` → `LowLevelHookCapture`), then constructs `MacroEngine`; hosted services `MacroHostedService` (boots engine + hotkeys, loads timeline) and `BrowserLauncher` live in `MacroTool.Web/Hosting/`. `ContentRootPath = AppContext.BaseDirectory`, so config and the default `timeline.json` resolve next to the exe (in `bin/...` during dev).
- `MacroTool.Domain/` (namespace `MacroTool.Domain`, editing under `MacroTool.Domain.Editing`) holds `MacroEvent`/`MacroTimeline`, the state machine, and timeline editing algorithms. `MacroTool.Application/` (`MacroTool.Application.Engine` + `.Ports`) holds `MacroEngine`, `EngineSettings`, `LogBuffer`, `Player`, `Recorder` policy (F-key filtering, dedup, trailing-mouse drop); it never references Infrastructure or the UI.
- `MacroTool.Infrastructure/Interop/` (`Native.cs`, `InputSender.cs`, `Win32InputSink`, `LowLevelHookCapture`, `WindowsEnvironment`) is the only place with user32 P/Invoke (SendInput, RegisterHotKey, low-level hooks); `.Storage/TimelineStore` is the JSON repository and `.Hotkeys/HotkeyHost` the global-hotkey thread. Recording/replay requires an interactive desktop session — never try to "run tests" of the recorder in a service/CI context.
- UI components talk only to the `MacroEngine` facade (snapshot + events + `SaveTimeline`/`LoadTimelineFromDisk`); `LogBuffer` is the only other Application type they inject. The web panel must never click-simulate into its own recording (README: page clicks during recording get filtered).

## Workflow

- Non-trivial work is spec-driven via OpenSpec: proposals/design/tasks live in `openspec/changes/<name>/`, completed changes go to `openspec/changes/archive/`. Use the repo-local `openspec-*` skills (propose / apply / update / sync / archive) instead of hand-rolling changes.
- Keep `README.md` in sync when changing user-visible behavior (hotkeys, config keys, timeline JSON schema, limits) — it's the product's only documentation.
