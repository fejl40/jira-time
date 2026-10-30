---
id: TASK-98
title: Use the browser's time zone for today and worklog start times
status: To Do
assignee: []
created_date: '2026-10-30 13:06'
labels: []
dependencies: []
priority: low
ordinal: 163000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Today (which day and period open, what counts as up to today) and the 09:00 start sent with each worklog use the server's time zone (TimeProvider.LocalTimeZone). On a shared server a user in another zone gets the wrong day near midnight and a wrong offset on their worklogs. At sign-in, read the browser's zone (Intl.DateTimeFormat().resolvedOptions().timeZone) and keep it with the signed-in user of the tab; use it for today and for the worklog start time, falling back to the server's zone when the browser gives none or one the server doesn't know. Colleagues running the app locally see no change, since their browser and server zones match.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 The tab keeps the browser's time zone with the signed-in user, read at sign-in
- [ ] #2 Today on the timesheet, the export and the period summary uses that zone
- [ ] #3 The worklog start time (09:00 on the row's day) is sent with that zone's offset
- [ ] #4 The server's zone is used when the browser gives none or an unknown one
- [ ] #5 Tests cover a user whose zone differs from the server's: today near midnight and the start time offset
- [ ] #6 AGENTS.md (Logging a day) says which zone is used
- [ ] #7 dotnet build has 0 warnings and dotnet test passes
<!-- AC:END -->
