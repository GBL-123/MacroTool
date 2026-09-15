# Design: fix-empty-recording-overwrite

## Context

`StopRecordingInternal`（MacroEngine.cs:271-300）无条件执行 `_timeline = timeline` 与 `_store.Save(...)`。控制面板发起的停止会丢弃尾部 300ms 鼠标事件（`Recorder.Stop(dropTrailingMouse)`），因此一次只含鼠标操作的录制可能在被过滤后恰好为空；F10 误触亦然。`timeline.json.bak` 只是事后补救，不是防线。动机见 proposal.md - Why。

## Goals / Non-Goals

**Goals:**

- 空录制停止时：磁盘文件与内存当前时间线均保持原状，并留下可观察的说明性日志。
- 非空录制停止：行为完全不变。
- 退出路径（F12 / 关闭服务）复用同一保护。

**Non-Goals:**

- 不加确认对话框：F10 停止无 UI 上下文，停止操作不应被阻塞（且 0 事件停止本身不是危险操作）。
- 不提供"用空录制清空时间线"的功能；清空仍可通过编辑器删除全部动作实现。
- 不改编辑器保存路径（`TimelineStore.Save` 的 .bak 语义不变）。

## Decisions

1. **守卫放在 `StopRecordingInternal`，以过滤后的最终内容判空**：`recorder.Stop(fromUi)` 返回的时间线是尾部过滤后的最终版本，`Events.Count == 0` 即"本来会写盘的内容为空"。空则跳过赋值与保存。
   - 备选"以原始捕获数判空"被否决：过滤结果才是落盘内容，过滤后为空同样不该覆盖。
2. **空录制停止不触发 `TimelineChanged`，仅触发 `StateChanged`**：时间线没有任何变化（仍是先前的那份），触发 TimelineChanged 会让 UI 做无谓的重载；状态从录制回到空闲必须广播。
3. **无既有时间线时同样不落盘**：空文件没有使用价值，且"文件不存在 = 无时间线"已是引擎的既有语义（启动加载需求）。避免出现"存在一个空时间线"的中间态。
4. **日志措辞区分两种情形**：有既有时间线 → 说明"未捕获任何操作，已保留原有时间线（未覆盖）"；没有 → 说明"未捕获任何操作，未保存"。均经既有 LogBuffer 通道，满足日志广播需求。

## Risks / Trade-offs

- [用户想通过空录制清空时间线] → 该用法从来不是显式功能；README 明确指向编辑器清空方式。
- [集成测试依赖真实低层钩子（约 120ms 窗口），任何按键/鼠标输入会干扰判定] → 与既有 `Recorder_StartStopWithoutInput_ProducesEmptyTimeline` 相同的注意事项；测试仅断言"文件与内存时间线不变"，对杂散输入不敏感（杂散输入只会让录制非空、走正常保存路径——那会改变文件，测试需重跑；在任务中注明）。
- [判空逻辑与输出格式耦合（保存时才序列化）] → 判空基于事件数而非序列化结果，不受 JSON 格式变化影响。

## Migration Plan

无数据迁移。回滚 = revert 提交；期间若已发生空录制被丢弃的情况，`.bak` 机制保持原样不受影响。

## Open Questions

无。
