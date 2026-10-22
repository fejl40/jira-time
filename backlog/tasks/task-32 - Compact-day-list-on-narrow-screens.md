---
id: TASK-32
title: Compact day list on narrow screens
status: Done
assignee: []
created_date: '2026-10-22T19:36:00Z'
updated_date: '2026-10-22T20:01:00Z'
labels: []
dependencies: []
priority: low
ordinal: 32000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
The period grid needs horizontal scrolling on a phone. Show a list of days instead on narrow screens.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 On narrow screens a list of days with logged, expected and missing hours replaces the grid
- [x] #2 Tapping a day opens it in the day view
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
Below the sm breakpoint the grid is hidden and a list of days shows the date, weekend/holiday/time off, logged / expected hours, missing hours and dots for unsaved and not in Jira. Tapping a day selects it. Checked at 390 px wide.
<!-- SECTION:FINAL_SUMMARY:END -->
