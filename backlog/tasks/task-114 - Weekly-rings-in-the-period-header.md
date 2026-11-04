---
id: TASK-114
title: Weekly rings in the period header
status: Done
assignee: []
created_date: '2026-11-02 18:25'
updated_date: '2026-11-04 09:15'
labels:
  - ui
  - wow
dependencies: []
priority: medium
ordinal: 187000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Each week of the shown period gets a ring in the header that closes as its logged hours reach what the week needs, like a fitness watch. A ring that closes while the user watches gives a small sparkle.
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
Each week of the shown period has a ring in the period header that closes as its hours reach what it needs, with a sparkle when one closes while watched. Rule in Domain/WeekRings.cs (tested), label in Format.WeekRing (tested), component WeekRings.razor, styles Styles/rings.css, sparkle in motion.js. Checked in the browser light/dark, 1400 px and 390 px, reduced motion, Jira down.
<!-- SECTION:FINAL_SUMMARY:END -->
