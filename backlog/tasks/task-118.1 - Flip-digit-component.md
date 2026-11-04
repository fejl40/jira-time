---
id: TASK-118.1
title: Flip digit component
status: Done
assignee: []
created_date: '2026-11-02 18:25'
updated_date: '2026-11-04 09:25'
labels:
  - ui
  - wow
dependencies: []
parent_task_id: TASK-118
priority: medium
ordinal: 201000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
A component that shows a number as digit tiles; a changed digit flips with a top and bottom half animation, unchanged digits stay still. Keeps the formatting of Format (7.5 h).
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 Only changed digits flip
- [x] #2 Screen readers get the plain number (aria-label), not the tiles
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
Components/Shared/FlipNumber.razor shows hours (Format.Hours, with Zero and Unit like Count) as digit tiles; FlipBoard (Components/FlipBoard.cs, tested in FlipBoardTests) lines old and new numbers up on the decimal point and marks which tiles turn, so only changed digits flip (top half folds down, then bottom half in, staggered from the right). role=img with aria-label gives screen readers the plain number; tiles are aria-hidden. Styles in Styles/flip.css.
<!-- SECTION:FINAL_SUMMARY:END -->
