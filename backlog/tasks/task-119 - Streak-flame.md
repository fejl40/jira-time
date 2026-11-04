---
id: TASK-119
title: Streak flame
status: Done
assignee: []
created_date: '2026-11-02 18:25'
updated_date: '2026-11-04 09:16'
labels:
  - ui
  - wow
dependencies: []
priority: medium
ordinal: 203000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
A small flame in the header counts the workdays in a row logged on time (complete by the end of the day), up to today.
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
Streak flame in the header: Domain/Streak.cs (rule, tested), Services/WorkStreak.cs (counted by PeriodLoader from the recent work it already reads), Components/Shared/StreakFlame.razor and Styles/touches.css. Checked in the browser in light/dark, wide/phone and reduced motion, including the flare after Save all.
<!-- SECTION:FINAL_SUMMARY:END -->
