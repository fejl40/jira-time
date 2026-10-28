---
id: TASK-63
title: Split JiraClient into issue and worklog clients
status: Done
assignee: []
created_date: '2026-10-28 07:13'
updated_date: '2026-10-28 07:15'
labels:
  - refactor
dependencies: []
priority: medium
ordinal: 63000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
JiraClient mixes issue searches (picker, sprint, period, statuses, myself) with worklog reads and writes. Split worklogs into their own client so each class has one job.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 Worklog reads and writes live in their own client
- [x] #2 Callers and tests use the right client
- [x] #3 Behaviour is unchanged
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
Moved worklog reads and writes and their helpers from JiraClient into a new JiraWorklogClient, registered the same way. TimeLog, WorklogSync and the tests now use it; behaviour is unchanged.
<!-- SECTION:FINAL_SUMMARY:END -->
