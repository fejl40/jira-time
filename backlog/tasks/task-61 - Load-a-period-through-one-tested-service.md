---
id: TASK-61
title: Load a period through one tested service
status: Done
assignee: []
created_date: '2026-10-28 07:13'
updated_date: '2026-10-28 07:18'
labels:
  - refactor
dependencies: []
priority: medium
ordinal: 61000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Timesheet.razor reads logged time, days, the previous period reminder, recent issues and comments, sprint and earlier issues and statuses itself. Move this into a PeriodLoader service that returns everything the page shows, so the page wires parts together and the loading rules are tested.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 A service loads a period's data (time, days, previous period reminder, picks, statuses) in one call
- [x] #2 Timesheet.razor uses it and keeps no loading rules
- [x] #3 Tests cover the loader, including Jira being unreadable
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
PeriodLoader loads a period's time, days, sprint, earlier issues, recent picks and the previous period reminder in one call; IssueStatuses keeps the statuses the page shows and looks up new ones. Timesheet.razor only wires the results.
<!-- SECTION:FINAL_SUMMARY:END -->
