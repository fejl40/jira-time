---
id: TASK-68
title: Make days off clear at a glance
status: Done
assignee: []
created_date: '2026-10-29 07:43'
updated_date: '2026-10-30 08:27'
labels:
  - ui
dependencies: []
priority: medium
ordinal: 72000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Days marked off for vacation or sickness should stand out in the timesheet so you can see them without opening the day.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 Days off look clearly different from workdays in the grid and the day list (phones)
- [x] #2 The reason (vacation, sick, other) shows at a glance, and partial days off are visible too
- [x] #3 Works in light and dark mode; checked in the browser
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
Time off now stands out per reason (DayOffLook in Components, .day-off-* styles in Styles/app.css, DayInfo.Reason in Domain): vacation ☀️ amber, sick 🤒 rose, other 🗓️ violet. A whole day off stripes its grid column (header to footer) and phone-list entry with the icon under the date; a partial day tints them and puts the icon next to the weekday. The phone list shows a short label (Vacation, Time off 3 h), the day header a tinted badge (Vacation · whole day, Sick · 4 h off), the grid header the same as tooltip. Saved hours on a whole day off stay readable, struck through next to the dashed 0 (draft pills now have a solid background), until Save day removes them (TASK-76). Checked in Chromium in light and dark, wide grid and 390 px phone list, with vacation and sick whole days and a partial other day.
<!-- SECTION:FINAL_SUMMARY:END -->
