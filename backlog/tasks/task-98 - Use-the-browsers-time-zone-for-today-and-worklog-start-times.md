---
id: TASK-98
title: Use the browser's time zone for today and worklog start times
status: Done
assignee: []
created_date: '2026-10-30 13:06'
updated_date: '2026-10-30 13:25'
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
- [x] #1 The tab keeps the browser's time zone with the signed-in user, read at sign-in
- [x] #2 Today on the timesheet, the export and the period summary uses that zone
- [x] #3 The worklog start time (09:00 on the row's day) is sent with that zone's offset
- [x] #4 The server's zone is used when the browser gives none or an unknown one
- [x] #5 Tests cover a user whose zone differs from the server's: today near midnight and the start time offset
- [x] #6 AGENTS.md (Logging a day) says which zone is used
- [x] #7 dotnet build has 0 warnings and dotnet test passes
<!-- AC:END -->

## Implementation Notes

<!-- SECTION:NOTES:BEGIN -->
The zone travels on the signed-in connection (JiraConnection.TimeZone, not stored in the browser), so JiraWorklogClient uses it for the start offset and CurrentUser.TimeZone exposes it; Clock.Today(zone) is the one place today is computed. The Jira request log (file per server day, Logs page, toast link) stays on the server's day. Chrome reports ICU's names (Asia/Calcutta, Europe/Kiev); a server whose tzdata lacks those old aliases (minimal Linux containers) doesn't know them and uses its own zone, as the fallback says. AC 6 (AGENTS.md) is applied by the coordinator.
<!-- SECTION:NOTES:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
At sign-in (restore and connect) the tab reads the browser's time zone (wwwroot/timezone.js, Intl.DateTimeFormat) and keeps it on the signed-in connection; CurrentUser.TimeZone exposes it. Today on the timesheet, recent work, the export and Settings uses Clock.Today(user.TimeZone); worklogs start at 09:00 with that zone's offset. No or an unknown zone falls back to the server's. Tests: UserTimeZoneTests (today near midnight both ways, recent work range, start offsets, fallback) and SignInSessionTests (zone kept, unknown ignored). Browser-checked with Playwright in America/New_York and Asia/Tokyo against a server in Pacific/Kiritimati.
<!-- SECTION:FINAL_SUMMARY:END -->
