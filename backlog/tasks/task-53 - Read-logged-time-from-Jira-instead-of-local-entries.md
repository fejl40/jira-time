---
id: TASK-53
title: Read logged time from Jira instead of local entries
status: To Do
assignee: []
created_date: '2026-10-27 14:01'
labels:
  - jira-source-of-truth
dependencies: []
priority: high
ordinal: 53000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Jira becomes the source of truth for time logged on issues. The timesheet loads a period's work from the user's Jira worklogs (worklog id as the entry's identity) instead of data/documents/time-entries. Days off, work hours and user settings stay in the app. Jira's search index can lag right after a write, so worklogs the app just wrote are merged in until the search finds them.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 Opening a period shows the user's Jira worklogs for it: issue, day, hours and comment
- [ ] #2 Worklogs logged in Jira or WorklogPRO show the same as ones logged here, with no From Jira distinction
- [ ] #3 Work the app just saved shows even if Jira's search has not caught up
- [ ] #4 Totals, missing hours, progress, suggestions and earlier-in-period rows work from Jira data
- [ ] #5 Tests cover loading, mapping and the search-lag merge
<!-- AC:END -->
