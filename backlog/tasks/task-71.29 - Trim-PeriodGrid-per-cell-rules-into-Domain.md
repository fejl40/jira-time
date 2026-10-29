---
id: TASK-71.29
title: 'Trim PeriodGrid: per-cell rules into Domain'
status: Done
assignee: []
created_date: '2026-10-29 07:52'
updated_date: '2026-10-29 08:15'
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
- [x] #1 PeriodRow.On(day) and a tested SyncProblems helper
- [x] #2 PeriodGrid 180 lines or fewer with no LINQ in markup
- [x] #3 Grid looks unchanged
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
PeriodRow.On(day) and a tested TimeEntry.SyncProblems (with the Not sent to Jira yet fallback) hold the per-cell rules; the cell markup moved to Sheet/PeriodCell.razor and footer classes are FooterDayCell/FooterTotalCell; PeriodGrid is 172 lines and its rendered table HTML is identical (compared in the browser, including a not-in-Jira cell).
<!-- SECTION:FINAL_SUMMARY:END -->
