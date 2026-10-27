---
id: TASK-52
title: Slow the animations to the first proposed speed
status: Done
assignee: []
created_date: '2026-10-27 13:54'
updated_date: '2026-10-27 13:56'
labels: []
dependencies: []
ordinal: 52000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Carl wants the animations at the speed first proposed: twice as long as now, 150 ms for small things and 300 ms for moves.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 Small animations take 150 ms and moves 300 ms, staggers and script animations scaled the same
- [x] #2 Reduced motion still turns them off
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
Motion tokens set to 150 ms (small), 300 ms (moves) and 30 ms stagger; script animations follow the tokens; reduced motion still turns everything off.
<!-- SECTION:FINAL_SUMMARY:END -->
