---
id: TASK-71.29
title: 'Trim PeriodGrid: per-cell rules into Domain'
status: To Do
assignee: []
created_date: '2026-10-29 07:52'
labels:
  - refactor
dependencies: []
parent_task_id: TASK-71
priority: medium
ordinal: 104000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
PeriodGrid.razor filters entries per cell in markup and holds the sync-problem rule in CellTitle; footer cell classes repeat. (Found in the TASK-71 review.)
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 PeriodRow.On(day) and a tested SyncProblems helper
- [ ] #2 PeriodGrid 180 lines or fewer with no LINQ in markup
- [ ] #3 Grid looks unchanged
<!-- AC:END -->
