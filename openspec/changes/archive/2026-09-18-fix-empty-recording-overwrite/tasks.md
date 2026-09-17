# Tasks: fix-empty-recording-overwrite

## 1. 引擎空录制保护

- [x] 1.1 修改 `MacroEngine.StopRecordingInternal`：录制停止后若 `timeline.Events.Count == 0`，跳过 `_timeline` 赋值与 `_store.Save`，按有无既有时间线写出两种说明性日志，仅触发 `StateChanged`；非空时行为不变（保存成功/失败日志、`StateChanged` 与 `TimelineChanged` 保持原样）。验证：`dotnet build` 通过。

## 2. 测试

- [x] 2.1 在 `EngineHostingIntegrationTests` 新增 Windows 集成测试：临时目录预置含 N 个事件的时间线文件 → 构造引擎（真实 HotkeyHost 不启动）→ `StartRecording` → 无输入等待约 120ms → `StopRecording(fromUi: true)` → 断言文件内容与最后写入时间均未变、`GetSnapshot()` 的事件数与时长仍为原值、日志含"未捕获"说明条目。验证：`dotnet test` 全绿（该用例安装真实低层钩子，运行期间任何键鼠输入会使其失败，重跑即可）。
- [x] 2.2 回归确认：既有 `Recorder_StartStopWithoutInput_ProducesEmptyTimeline`（只测 Recorder 对象）与 `MacroEngineTests` 全部不受影响。验证：`dotnet test` 140+ 用例全绿。

## 3. 文档

- [x] 3.1 更新 `README.md`「数据文件」一节：说明"每次录制整体覆盖"改为"有事件的录制整体覆盖；未捕获任何操作的录制不会覆盖既有时间线"。验证：与实测行为一致。
