---
id: TASK-71.16
title: Add up hours with one TimeEntry.Total helper
status: Done
assignee: []
created_date: '2026-10-29 07:52'
updated_date: '2026-10-29 07:54'
labels:
  - refactor
dependencies: []
parent_task_id: TASK-71
priority: medium
ordinal: 91000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
`Aggregate(TimeSpan.Zero, ...)` is copied about a dozen times and Components call PeriodSheet.Total for entries unrelated to a sheet. (Found in the TASK-71 review.)
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 One TimeEntry.Total (and a TimeSpan Sum if needed) replaces every copy
- [x] #2 PeriodSheet has no static Total or HasSyncProblem
- [x] #3 Build and tests pass
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
TimeEntry.Total, TimeEntry.HasSyncProblem and a TimeSpan Sum extension replace the Aggregate copies in Domain and Components; WorklogSync.cs keeps its copy until its rewrite lands.
<!-- SECTION:FINAL_SUMMARY:END -->
