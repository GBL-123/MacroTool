# Proposal: fix-empty-recording-overwrite

## Why

录制停止时会无条件把（可能为空的）新时间线整体覆盖到磁盘。一次"0 事件"的录制——比如误触一次 F10 立即再停、或页面停止时尾部 300ms 鼠标事件被过滤后恰好为空——会**静默抹掉已保存的时间线**。本机已实际发生两次，均靠 `timeline.json.bak` 才救回数据。空录制没有任何保留价值，覆盖是纯损失。

## What Changes

- 引擎停止录制时增加判空保护：
  - 捕获到 ≥1 个事件：行为不变（保存到时间线文件并作为当前时间线）。
  - 捕获 0 个事件：**不写盘、不替换当前时间线**，保留磁盘与内存中的既有时间线，并产生一条说明性日志（未捕获操作、已保留原时间线；无既有时间线时说明未保存）。
- 日志沿用既有日志通道（无需新 UI）。
- README「数据文件」一节更新：说明空录制不覆盖。

## Capabilities

### New Capabilities

（无）

### Modified Capabilities

- `macro-engine`: 「时间线加载与替换」需求补充空录制保护——0 事件录制停止时不得改写时间线文件、不得替换当前时间线。

## Impact

- `MacroTool/Application/Engine/MacroEngine.cs`（`StopRecordingInternal`）
- `README.md`（数据文件一节）
- 测试：新增引擎级集成用例（空录制停止后文件与内存时间线保持不变）；既有 `Recorder_StartStopWithoutInput_ProducesEmptyTimeline` 只针对 Recorder 本身，不受影响
