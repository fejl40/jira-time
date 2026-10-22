---
id: TASK-33
title: Click a grid cell to edit that issue on that day
status: Done
assignee: []
created_date: '2026-10-22T19:36:00Z'
updated_date: '2026-10-22T19:55:00Z'
labels: []
dependencies: []
priority: low
ordinal: 33000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Clicking a cell should go straight to that issue's hours for the day.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 Clicking a cell with work selects the day and focuses that row's hours
- [x] #2 Clicking an empty cell adds a row for that issue and focuses its hours
- [x] #3 An added row left empty is ignored by Save day
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
Clicking a grid cell selects the day (with its suggestion) and focuses the hours of that issue's row; when the day has no row for the issue an empty draft is added and focused. Empty new rows (no hours, no comment) are ignored by Save day and don't count as changes (DayWork.IsBlank). Checked in the browser.
<!-- SECTION:FINAL_SUMMARY:END -->
