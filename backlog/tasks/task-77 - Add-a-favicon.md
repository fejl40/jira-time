---
id: TASK-77
title: Add a favicon
status: Done
assignee: []
created_date: '2026-10-30 09:05'
updated_date: '2026-10-30 09:07'
labels: []
dependencies: []
ordinal: 122000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
The browser tab shows the default icon. Add an app icon: a clock face over a timesheet tick, in the header's dark navy with a blue accent, readable at 16 px and in light and dark browser tabs.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 An SVG favicon is served and linked from App.razor
- [x] #2 A PNG apple-touch-icon is linked for phones
- [x] #3 The icon is readable at 16 px on light and dark tabs
- [x] #4 A test checks the head links the icons
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
SVG favicon (navy rounded square, white clock face with a sky-blue ring and navy hands) plus a 180 px apple-touch-icon and theme-color, linked through @Assets in App.razor. FaviconTests checks the links and files.
<!-- SECTION:FINAL_SUMMARY:END -->
