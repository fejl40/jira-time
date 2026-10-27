---
id: TASK-45
title: Slide the grid when changing period
status: Done
assignee: []
created_date: '2026-10-27 10:54'
updated_date: '2026-10-27 11:07'
labels: []
dependencies: []
ordinal: 45000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Animation: the grid slides left or right with the period buttons and the title cross-fades.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 The grid slides in from the side matching the direction
- [x] #2 The period title cross-fades
- [x] #3 Off when the system asks for reduced motion
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
The day list and grid sit in a wrapper keyed by period, which slides in from the right for a later period and the left for an earlier one (Motion.Slide). The period title is keyed too and fades in. The page clips horizontal overflow so the slide never adds a scrollbar.
<!-- SECTION:FINAL_SUMMARY:END -->
