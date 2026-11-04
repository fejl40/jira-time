---
id: TASK-116.2
title: Week ripple
status: Done
assignee: []
created_date: '2026-11-02 18:25'
updated_date: '2026-11-04 09:22'
labels:
  - ui
  - wow
dependencies: []
parent_task_id: TASK-116
priority: medium
ordinal: 196000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
The cells of each saved week pulse one after another (staggered 40 ms), before the tick reaches the Jira status.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 Ripple order follows the days
- [x] #2 Whole choreography under about 900 ms; off with reduced motion
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
SaveChoreography.For gives each saved week's cells in day order, 40 ms apart from 300 ms; weeks ripple side by side; ticks leave as the last cell pulses and share out what is left of 900 ms. Cells lift 2 px and glow green (.motion-ripple). Nothing plays with reduced motion.
<!-- SECTION:FINAL_SUMMARY:END -->
