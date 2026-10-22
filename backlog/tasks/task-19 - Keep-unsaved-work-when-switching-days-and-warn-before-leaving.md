---
id: TASK-19
title: Keep unsaved work when switching days and warn before leaving
status: Done
assignee: []
created_date: '2026-10-22T19:35:00Z'
updated_date: '2026-10-22T19:48:00Z'
labels: []
dependencies: []
priority: high
ordinal: 19000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Edits to saved rows are lost when another day is picked in the grid, and drafts are dropped without a warning when changing period, reloading or leaving the page. Keep every day's unsaved edits while the page is open and ask before throwing them away.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 Edits to saved rows survive picking another day and coming back
- [x] #2 Days with unsaved changes are marked in the grid
- [x] #3 Changing period, going to Settings, reloading or closing the tab asks first when something is unsaved
- [x] #4 Covered by tests
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
New Domain/DayWork holds a day's unsaved changes: edited copies of saved rows, removed ids, new rows, suggestion ids and unreadable hours. The timesheet keeps one per day while the period is open and DayEditor edits it, so switching days keeps everything. The grid shows a blue dot on days with changes, changed hours dashed (old value struck through), and an Unsaved row with the day totals after saving. A NavigationLock asks before leaving the period, going elsewhere, reloading or closing the tab when the user changed something; untouched suggestions don't count. Tests in DayWorkTests; checked in the browser.
<!-- SECTION:FINAL_SUMMARY:END -->
