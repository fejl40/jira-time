---
id: TASK-41
title: Drop Fill period suggestions into the grid day by day
status: Done
assignee: []
created_date: '2026-10-27 10:54'
updated_date: '2026-10-27 10:58'
labels: []
dependencies: []
ordinal: 41000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Animation: suggested hours drop into the grid cells one day after another, left to right.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 New suggestion cells drop in with a short delay per day, left to right
- [x] #2 Only newly added cells animate
- [x] #3 Off when the system asks for reduced motion
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
Draft cells use motion-drop; right after Fill period the grid staggers them 15 ms per day, left to right. Other new draft cells drop in at once. Existing cells don't replay.
<!-- SECTION:FINAL_SUMMARY:END -->
