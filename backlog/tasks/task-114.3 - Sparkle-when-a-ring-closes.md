---
id: TASK-114.3
title: Sparkle when a ring closes
status: Done
assignee: []
created_date: '2026-11-02 18:25'
updated_date: '2026-11-04 09:15'
labels:
  - ui
  - wow
dependencies: []
parent_task_id: TASK-114
priority: medium
ordinal: 190000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
A ring that becomes full while the user watches sparkles once (through Milestones, so it isn't repeated on reload).
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 Sparkles once per ring closing, not on page load
- [x] #2 No sparkle with reduced motion
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
WeekRings notices rings closing with Milestones<DateOnly> (reset per period, so nothing at first look or on reload) and calls MotionScript.SparkleAsync: the ring pops, a halo spreads and ten sparks fly out (wwwroot/motion.js sparkle). Speed 0 under reduced motion, so nothing sparkles.
<!-- SECTION:FINAL_SUMMARY:END -->
