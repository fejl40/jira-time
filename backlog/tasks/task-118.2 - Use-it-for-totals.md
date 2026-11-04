---
id: TASK-118.2
title: Use it for totals
status: Done
assignee: []
created_date: '2026-11-02 18:25'
updated_date: '2026-11-04 09:25'
labels:
  - ui
  - wow
dependencies: []
parent_task_id: TASK-118
priority: medium
ordinal: 202000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Replace the counting totals in the period header and week totals with the flip board.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 Header and week totals flip on save and sync
- [x] #2 Numbers jump straight to the value with reduced motion
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
The period header's logged and missing hours (data-period-logged, data-period-missing) and the grid's week totals (data-week-logged) use FlipNumber instead of Count. They flip on save and sync (and when the header moves to another period); with reduced motion or before the first screen has settled the tiles show the new value at once.
<!-- SECTION:FINAL_SUMMARY:END -->
