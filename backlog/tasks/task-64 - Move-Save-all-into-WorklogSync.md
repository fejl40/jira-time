---
id: TASK-64
title: Move Save all into WorklogSync
status: To Do
assignee: []
created_date: '2026-10-28 07:13'
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
- [ ] #1 One method saves every unsaved day of a sheet and reports saved, invalid and changed-in-Jira days
- [ ] #2 Timesheet.razor uses it
- [ ] #3 Tests cover the outcome
<!-- AC:END -->
