---
id: TASK-63
title: Split JiraClient into issue and worklog clients
status: To Do
assignee: []
created_date: '2026-10-28 07:13'
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
- [ ] #1 Worklog reads and writes live in their own client
- [ ] #2 Callers and tests use the right client
- [ ] #3 Behaviour is unchanged
<!-- AC:END -->
