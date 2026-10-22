---
id: TASK-21
title: Keyboard shortcuts in the timesheet
status: Done
assignee: []
created_date: '2026-10-22T19:35:00Z'
updated_date: '2026-10-22T19:55:00Z'
labels: []
dependencies: []
priority: high
ordinal: 21000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Logging a day should work without the mouse.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 Ctrl/Cmd+S saves the open day, also while typing in a field
- [x] #2 Left and right arrows (outside fields) move to the previous and next day
- [x] #3 N (outside fields) adds a row
- [x] #4 Opening a day focuses the first hours field
- [x] #5 Enter in an hours or comment field moves to the next row's hours
- [x] #6 Shortcuts are listed in the UI
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
wwwroot/keys.js listens on the document and calls the timesheet back (attached via a DotNetObjectReference): Ctrl/Cmd+S blurs the current field (so its change is sent first) and saves the open day, ←/→ move days outside fields, N adds a row and focuses its issue picker, Enter in hours/comment jumps to the next row's hours, Esc leaves a field. Picking a day in the grid header focuses its first hours field (arrow navigation doesn't, so arrows keep working). The keys are listed under the day view. Checked in the browser.
<!-- SECTION:FINAL_SUMMARY:END -->
