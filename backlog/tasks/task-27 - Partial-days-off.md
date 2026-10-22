---
id: TASK-27
title: Partial days off
status: Done
assignee: []
created_date: '2026-10-22T19:36:00Z'
updated_date: '2026-10-22T19:39:00Z'
labels: []
dependencies: []
priority: medium
ordinal: 27000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Mark day off is all or nothing. Allow part of a day off (doctor, leaving early), which lowers the hours the day needs.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 A day off can be a number of hours; the whole day is the default
- [x] #2 A partial day off lowers the day's expected hours, so missing hours and suggestions use the rest
- [x] #3 The day view shows the time off and it can be undone
- [x] #4 Covered by tests
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
DayOff got optional Hours. Time off… in the day view opens reason + hours (default the whole day). Hours below the day's hours keep it a workday with lower Expected and show 'Vacation · 3 h off' with Undo; the whole day (or more) is a day off as before. A suggestion made for the old hours is replaced. Tests in WorkCalendarTests.
<!-- SECTION:FINAL_SUMMARY:END -->
