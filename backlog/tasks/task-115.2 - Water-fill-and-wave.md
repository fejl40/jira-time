---
id: TASK-115.2
title: Water fill and wave
status: Done
assignee: []
created_date: '2026-11-02 18:25'
updated_date: '2026-11-04 09:16'
labels:
  - ui
  - wow
dependencies: []
parent_task_id: TASK-115
priority: medium
ordinal: 193000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
The fill rises with a transition; a short wave (SVG path animation) runs on the surface when the level changes, then settles.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 Wave runs once per change, not continuously
- [x] #2 Still fill with reduced motion
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
DayWater.razor: water rises by transform (700 ms), an SVG wave keyed by the change count runs once (1.2 s) and flattens; reduced motion leaves still water. Styles .day-water in Styles/motion.css.
<!-- SECTION:FINAL_SUMMARY:END -->
