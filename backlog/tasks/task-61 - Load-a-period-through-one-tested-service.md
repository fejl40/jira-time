---
id: TASK-61
title: Load a period through one tested service
status: To Do
assignee: []
created_date: '2026-10-28 07:13'
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
- [ ] #1 A service loads a period's data (time, days, previous period reminder, picks, statuses) in one call
- [ ] #2 Timesheet.razor uses it and keeps no loading rules
- [ ] #3 Tests cover the loader, including Jira being unreadable
<!-- AC:END -->
