---
id: TASK-43
title: Settle saved cells and send a tick to the Jira status
status: Done
assignee: []
created_date: '2026-10-27 10:54'
updated_date: '2026-10-27 11:02'
labels: []
dependencies: []
ordinal: 43000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Animation: on Save all or Save day the dashed draft cells turn into solid numbers, and a tick travels from each saved cell to the sync status.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 Saved cells settle from draft to solid
- [x] #2 A tick flies from saved cells to the period status when Jira got the work
- [x] #3 Off when the system asks for reduced motion
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
The page remembers the days of the last Save day or Save all; their saved cells play motion-settle (draft blue to plain). Days Jira got completely are marked data-sent, and after the reload jiraMotion.flyTicks sends a check mark from each to the period status, which flashes green when they arrive.
<!-- SECTION:FINAL_SUMMARY:END -->
