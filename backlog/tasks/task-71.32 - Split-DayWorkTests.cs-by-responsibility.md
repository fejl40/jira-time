---
id: TASK-71.32
title: Split DayWorkTests.cs by responsibility
status: Done
assignee: []
created_date: '2026-10-29 07:52'
updated_date: '2026-10-29 08:42'
labels:
  - refactor
dependencies: []
parent_task_id: TASK-71
priority: medium
ordinal: 107000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
347 lines covering change tracking and row edits. (Found in the TASK-71 review.)
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 Two files of 250 lines or fewer, one responsibility each
- [x] #2 Helpers defined once
- [x] #3 All DayWork tests still run
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
DayWorkTests.cs (change tracking, totals, rows to save, remove/restore, row states; 240 lines) keeps the shared Day, Saved, Draft and Suggestion helpers, which DayWorkEditTests.cs (row edits, added rows, finding a row, fill the rest, copy a day; 203 lines) uses through using static; all 28 tests still run.
<!-- SECTION:FINAL_SUMMARY:END -->
