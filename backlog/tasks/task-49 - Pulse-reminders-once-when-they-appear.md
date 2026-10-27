---
id: TASK-49
title: Pulse reminders once when they appear
status: Done
assignee: []
created_date: '2026-10-27 10:54'
updated_date: '2026-10-27 11:12'
labels: []
dependencies: []
ordinal: 49000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Animation: the end-of-period and previous-period reminders pulse once when they appear, never on a loop.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 Reminders pulse once on appearing
- [x] #2 No looping animation
- [x] #3 Off when the system asks for reduced motion
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
Both reminders use motion-pulse: an amber glow that spreads out once (300 ms) when the reminder appears, including on the first screen, and never loops.
<!-- SECTION:FINAL_SUMMARY:END -->
