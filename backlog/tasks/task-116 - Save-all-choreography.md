---
id: TASK-116
title: Save all choreography
status: Done
assignee: []
created_date: '2026-11-02 18:25'
updated_date: '2026-11-04 09:22'
labels:
  - ui
  - wow
dependencies: []
priority: medium
ordinal: 194000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
When Save all saves, each saved row flies from the modal into its day in the grid, the week ripples cell by cell, then the existing tick flies to the Jira status.
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
After Save all, saved days' lines fly from the modal into their days, each saved week ripples cell by cell, then the ticks fly to the period status, all within about 900 ms after the save returns. Rules in Components/Sheet/SaveMotion.cs (SaveMotion, SaveChoreography) with SaveMotionTests; script wwwroot/flight.js; styles in Styles/motion.css. Checked in light, dark, a narrow scrolled grid, a phone and reduced motion (nothing plays).
<!-- SECTION:FINAL_SUMMARY:END -->
