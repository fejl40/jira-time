---
id: TASK-42
title: Fill each day header like a progress bar
status: Done
assignee: []
created_date: '2026-10-27 10:54'
updated_date: '2026-10-27 10:59'
labels: []
dependencies: []
ordinal: 42000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Animation: each day header fills towards its expected hours and goes green with a small pop when complete.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 Each workday header has a bar showing logged against expected hours that grows smoothly
- [x] #2 A day turns green with a pop when it becomes complete, not on page load
- [x] #3 Off when the system asks for reduced motion
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
Each workday header in the grid has a thin bar for saved against expected hours (PeriodSheet.Progress), which grows smoothly. A Milestones<T> helper notices days that become complete while the period is shown, so only those turn green with a pop, not the ones complete on load.
<!-- SECTION:FINAL_SUMMARY:END -->
