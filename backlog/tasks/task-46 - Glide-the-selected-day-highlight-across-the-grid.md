---
id: TASK-46
title: Glide the selected day highlight across the grid
status: Done
assignee: []
created_date: '2026-10-27 10:54'
updated_date: '2026-10-27 11:09'
labels: []
dependencies: []
ordinal: 46000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Animation: the blue highlight glides across the grid to the newly selected day.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 One highlight moves to the selected column
- [x] #2 It glides when the day changes and jumps on first show
- [x] #3 Off when the system asks for reduced motion
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
The selected column no longer gets its own background; one highlight element in the grid is placed over it by jiraMotion.glide after each render (and on resize). The first placement jumps, later ones glide.
<!-- SECTION:FINAL_SUMMARY:END -->
