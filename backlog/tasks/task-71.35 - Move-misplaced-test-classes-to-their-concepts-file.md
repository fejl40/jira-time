---
id: TASK-71.35
title: Move misplaced test classes to their concept's file
status: To Do
assignee: []
created_date: '2026-10-29 07:52'
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
- [ ] #1 TimeEntryTests.cs and MonthExportTests.cs each hold one class
- [ ] #2 RetryQueueTests.cs 250 lines or fewer
- [ ] #3 Test count unchanged
<!-- AC:END -->
