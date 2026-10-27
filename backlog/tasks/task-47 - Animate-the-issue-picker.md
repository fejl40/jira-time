---
id: TASK-47
title: Animate the issue picker
status: Done
assignee: []
created_date: '2026-10-27 10:54'
updated_date: '2026-10-27 11:10'
labels: []
dependencies: []
ordinal: 47000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Animation: the picker list opens with groups sliding in one after another, and the issue dot grows a little when an issue is picked.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 Picker groups slide in one after another
- [x] #2 The row's issue dot pops when an issue is picked
- [x] #3 Off when the system asks for reduced motion
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
Picker entries use motion-rise, staggered per group, so groups slide up one after another when the list opens. The row's issue dot is keyed by issue and gets motion-grow only on the row whose issue was just picked.
<!-- SECTION:FINAL_SUMMARY:END -->
