---
id: TASK-71.31
title: Split MultiUserTests.cs into one file per concept
status: To Do
assignee: []
created_date: '2026-10-29 07:52'
labels:
  - refactor
dependencies: []
parent_task_id: TASK-71
priority: medium
ordinal: 106000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
360 lines with four unrelated classes. (Found in the TASK-71 review.)
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 MultiUserTests.cs is gone; each new file holds one class of 250 lines or fewer
- [ ] #2 No test declares its own jira.example.com connection
- [ ] #3 Test count unchanged
<!-- AC:END -->
