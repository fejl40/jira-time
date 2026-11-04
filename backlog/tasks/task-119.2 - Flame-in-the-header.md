---
id: TASK-119.2
title: Flame in the header
status: Done
assignee: []
created_date: '2026-11-02 18:25'
updated_date: '2026-11-04 09:16'
labels:
  - ui
  - wow
dependencies: []
parent_task_id: TASK-119
priority: medium
ordinal: 205000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
A flame icon with the count, flickering gently; it grows a size at 5, 10 and 20 days, and a tooltip explains it.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 No flame below 2 days
- [x] #2 Flicker stops with reduced motion
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
Components/Shared/StreakFlame.razor in the header (signed in): an SVG flame with gradient, gentle flicker and glow (Styles/touches.css), the count, a tooltip; sizes at 5, 10 and 20 days; flares once when the streak grows while the tab is open. Still with reduced motion.
<!-- SECTION:FINAL_SUMMARY:END -->
