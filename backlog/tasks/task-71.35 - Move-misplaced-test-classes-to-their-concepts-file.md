---
id: TASK-71.35
title: Move misplaced test classes to their concept's file
status: Done
assignee: []
created_date: '2026-10-29 07:52'
updated_date: '2026-10-29 08:43'
labels:
  - refactor
dependencies: []
parent_task_id: TASK-71
priority: medium
ordinal: 110000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
RetryQueueStoreTests sits in TimeEntryTests.cs; EpicExportStoreTests sits in MonthExportTests.cs. (Found in the TASK-71 review.)
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 TimeEntryTests.cs and MonthExportTests.cs each hold one class
- [x] #2 RetryQueueTests.cs 250 lines or fewer
- [x] #3 Test count unchanged
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
RetryQueueStoreTests is folded into RetryQueueTests (208 lines), EpicExportStoreTests moved to EpicExportStoreTests.cs, and SaveAllTests (found in SaveDayTests.cs) to SaveAllTests.cs; every test file now holds one class (TimeHarness.cs keeps its helper types by design) and the test count is unchanged.
<!-- SECTION:FINAL_SUMMARY:END -->
