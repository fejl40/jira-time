---
id: TASK-38
title: 'Use one clock, one connection source and remove dead code'
status: Done
assignee: []
created_date: '2026-10-27 09:03'
updated_date: '2026-10-27 09:05'
labels: []
dependencies: []
ordinal: 38000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
The timesheet's today is a static field computed once per app run, so it goes stale after midnight. Pages mix ConnectionState and CurrentUser for the connection. Some code is no longer used and Documents.cs holds unrelated types.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 Today comes from TimeProvider everywhere, so it is correct after midnight
- [x] #2 Pages read the Jira connection from CurrentUser
- [x] #3 Unused code is removed
- [x] #4 Documents.cs is split into one file per subject
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
Today is read from TimeProvider each time (the timesheet's static today went stale after midnight). Pages read the connection from CurrentUser, and a new SignInSession owns restoring, connecting and signing out. Removed the unused Autocomplete component, Format.Time, DayFiller.ProposePeriod and DayWork.Clear. Documents.cs is split into Jira.cs, TimeEntry.cs, Period.cs and LegacyData.cs.
<!-- SECTION:FINAL_SUMMARY:END -->
