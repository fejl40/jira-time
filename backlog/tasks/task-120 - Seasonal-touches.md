---
id: TASK-120
title: Seasonal touches
status: Done
assignee: []
created_date: '2026-11-02 18:25'
updated_date: '2026-11-04 09:22'
labels:
  - ui
  - wow
dependencies: []
priority: medium
ordinal: 206000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Light snow drifts down the page in December, and on Danish holidays a string of lights hangs along the header.
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
Seasonal touches: snow in December and lights on Danish holidays (Domain/SeasonTouches.cs, Snowfall.razor, HolidayLights.razor, Styles/touches.css). Checked in the browser on 14 Dec, 25 Dec and 1 Jan (clock moved for the check only), light/dark, wide/phone and reduced motion.
<!-- SECTION:FINAL_SUMMARY:END -->
