---
id: TASK-117
title: A bigger finish when a period becomes complete
status: Done
assignee: []
created_date: '2026-11-02 18:25'
updated_date: '2026-11-04 09:32'
labels:
  - ui
  - wow
dependencies:
  - TASK-111
  - TASK-112
priority: medium
ordinal: 197000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Before the existing confetti, light sweeps across the grid column by column and the background (aurora or sky) glows for a moment.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 Off (or still) when the system asks for reduced motion
- [x] #2 Looks right in light and dark mode, on phones and wide screens
- [x] #3 Rules that decide what shows are in Domain or a plain class and covered by tests
- [x] #4 data-* attributes let a browser check find it; checked in the browser
- [x] #5 Build has 0 warnings, tests pass, wwwroot/app.css committed with the styles
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
A bigger finish when a period becomes complete: light sweep, background glow, then confetti. motion.js now exports confetti(from) and firstTime(key); finish.js plays the finish. data-finish-sweep and data-glowing for checks; checked by completing a period with Fill period + Save all, and on Aurora, Sky and Plain, light/dark, desktop/phone, reduced motion.
<!-- SECTION:FINAL_SUMMARY:END -->
