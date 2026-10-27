---
id: TASK-53
title: Read logged time from Jira instead of local entries
status: Done
assignee: []
created_date: '2026-10-27 14:01'
updated_date: '2026-10-27 14:28'
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
- [x] #1 Opening a period shows the user's Jira worklogs for it: issue, day, hours and comment
- [x] #2 Worklogs logged in Jira or WorklogPRO show the same as ones logged here, with no From Jira distinction
- [x] #3 Work the app just saved shows even if Jira's search has not caught up
- [x] #4 Totals, missing hours, progress, suggestions and earlier-in-period rows work from Jira data
- [x] #5 Tests cover loading, mapping and the search-lag merge
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
TimeLog reads the user's Jira worklogs for a range (cached a minute) and lays the retry queue over them. Worklog ids give stable entry ids; no From Jira label. RecentWorklogIssues makes issues saved on in the last 15 minutes read directly while the search index lags. When Jira can't be read the sheet shows only queued work, hours are unknown and days can't be edited.
<!-- SECTION:FINAL_SUMMARY:END -->
