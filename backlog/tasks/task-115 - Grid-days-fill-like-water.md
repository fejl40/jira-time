---
id: TASK-115
title: Grid days fill like water
status: Done
assignee: []
created_date: '2026-11-02 18:25'
updated_date: '2026-11-04 09:16'
labels:
  - ui
  - wow
dependencies: []
priority: medium
ordinal: 191000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Each day column in the grid fills from the bottom to its logged hours against what the day needs, with a small wave on the surface when the hours change.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 Off (or still) when the system asks for reduced motion
- [x] #2 Looks right in light and dark mode, on phones and wide screens
- [x] #3 Rules that decide what shows are in Domain or a plain class and covered by tests
- [x] #4 data-* attributes let a browser check find it; checked in the browser
- [x] #5 Build has 0 warnings, tests pass, wwwroot/app.css committed with the styles
<!-- AC:END -->

## Implementation Notes

<!-- SECTION:NOTES:BEGIN -->
The glass is the day's header cell in the grid (and a small glass next to each day's hours in the phone day list): water across the whole column would sit behind the hours and hurt readability. It replaces the thin progress bar. The water shows saved hours (Jira), not unsaved drafts.
<!-- SECTION:NOTES:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
Each day header in the grid is a glass of water filled to saved hours against needed hours (emerald when full), with a one-off wave when the level changes and a glow when the day becomes complete; the phone day list shows a small glass per day. Rules in Domain/DayFill.cs with tests; data-day-fill, data-fill-level and data-fill-wave for browser checks; checked in light, dark, phone and reduced motion.
<!-- SECTION:FINAL_SUMMARY:END -->
