---
id: TASK-112.3
title: Sky layers and stars
status: Done
assignee: []
created_date: '2026-11-02 18:25'
updated_date: '2026-11-04 09:19'
labels:
  - ui
  - wow
dependencies: []
parent_task_id: TASK-112
priority: medium
ordinal: 183000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Gradient layers for each phase cross-fade; at night faint stars twinkle (CSS, a fixed set of positions), and dark mode always uses the night layers.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 Stars only at night or in dark mode
- [x] #2 Twinkle stops with reduced motion
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
One gradient layer per phase, cross-fading on --sky-<phase> opacities (4 s); three box-shadow star sets twinkle out of step, shown with the night layer and always in dark mode, which shows only the night layers. Twinkle off with reduced motion.
<!-- SECTION:FINAL_SUMMARY:END -->
