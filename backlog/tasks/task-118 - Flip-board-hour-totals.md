---
id: TASK-118
title: Flip-board hour totals
status: Done
assignee: []
created_date: '2026-11-02 18:25'
updated_date: '2026-11-04 09:25'
labels:
  - ui
  - wow
dependencies: []
priority: medium
ordinal: 200000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Hour totals (header status line, week totals) flip digit by digit like a station departure board instead of counting up.
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
Hour totals in the period header and the week totals flip digit by digit like a departure board. FlipBoard rule (tested), FlipNumber component, Styles/flip.css (transforms only). Checked in the browser light/dark, 1400 px and 390 px, with reduced motion (jumps straight to the value).
<!-- SECTION:FINAL_SUMMARY:END -->
