---
id: TASK-40
title: Animate numbers counting to their new value
status: Done
assignee: []
created_date: '2026-10-27 10:54'
updated_date: '2026-10-27 10:57'
labels: []
dependencies: []
ordinal: 40000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Part of the animation set Carl asked for, at twice the speed first proposed (75-150 ms). Sets up the shared pieces: speed tokens, reduced motion, and a small motion script. Totals, week sums and hours missing count up or down instead of jumping.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 Shared motion CSS and script with 75 ms and 150 ms speeds
- [x] #2 Day, week, period and missing totals count to the new value
- [x] #3 Off when the system asks for reduced motion
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
Styles/motion.css holds the speed tokens (75 ms, 150 ms) and turns animation off for reduced motion; wwwroot/motion.js counts any element with data-count to its new value, from what is on screen. A Count component renders those numbers; the header, grid totals, week totals, missing hours, day list, day header and day editor total use it. Animations wait for html.motion-ready so the first screen draws still.
<!-- SECTION:FINAL_SUMMARY:END -->
