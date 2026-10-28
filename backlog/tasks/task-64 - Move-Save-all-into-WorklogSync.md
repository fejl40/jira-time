---
id: TASK-64
title: Move Save all into WorklogSync
status: Done
assignee: []
created_date: '2026-10-28 07:13'
updated_date: '2026-10-28 07:21'
labels:
  - refactor
dependencies: []
priority: low
ordinal: 64000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Timesheet.razor saves every unsaved day itself and sorts the outcome (saved, needs work, changed in Jira). Move the loop and the outcome into a tested method so the page only shows the result.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 One method saves every unsaved day of a sheet and reports saved, invalid and changed-in-Jira days
- [x] #2 Timesheet.razor uses it
- [x] #3 Tests cover the outcome
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
WorklogSync.SaveDaysAsync saves every unsaved day of a sheet and returns saved, needs-work and changed-in-Jira days with the day to show and the notice; Timesheet.razor only applies it.
<!-- SECTION:FINAL_SUMMARY:END -->
