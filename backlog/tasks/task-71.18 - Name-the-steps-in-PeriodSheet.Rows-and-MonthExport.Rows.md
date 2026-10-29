---
id: TASK-71.18
title: Name the steps in PeriodSheet.Rows and MonthExport.Rows
status: Done
assignee: []
created_date: '2026-10-29 07:52'
updated_date: '2026-10-29 07:56'
labels:
  - refactor
dependencies: []
parent_task_id: TASK-71
priority: medium
ordinal: 93000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Both are long LINQ chains, against PRINCIPLES.md. (Found in the TASK-71 review.)
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 No chain longer than about 4 steps
- [x] #2 PeriodSheet and MonthExport tests pass unchanged
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
PeriodSheet.Rows builds each issue row in a named RowOf step and MonthExport.Rows uses a DayRows step, so no chain is longer than about four steps; the tests pass unchanged.
<!-- SECTION:FINAL_SUMMARY:END -->
