---
id: TASK-71.10
title: Give owned documents one way to be read
status: Done
assignee: []
created_date: '2026-10-29 07:52'
updated_date: '2026-10-29 08:05'
labels:
  - refactor
dependencies: []
parent_task_id: TASK-71
priority: medium
ordinal: 85000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
The 'get by id, then check it is mine' check is hand-written in WorkCalendarService, EpicExportStore and RetryQueue. (Found in the TASK-71 review.)
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 An IOwnedDocument marker and one helper return null for another user's document
- [x] #2 No hand-written Owner check after GetAsync remains in Services
- [x] #3 The helper has a test; DataSeparationTests pass
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
IOwnedDocument (Domain/Users.cs) marks DayOff, UserSettings, EpicExportValues and TimeEntry; CurrentUser.GetMineAsync and Owns replace the hand-written owner checks in WorkCalendarService, EpicExportStore and RetryQueue, with a test. UserAccount is keyed by the owner itself, so it doesn't implement it.
<!-- SECTION:FINAL_SUMMARY:END -->
