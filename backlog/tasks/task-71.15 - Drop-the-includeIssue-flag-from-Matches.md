---
id: TASK-71.15
title: Drop the includeIssue flag from Matches
status: To Do
assignee: []
created_date: '2026-10-29 07:52'
labels:
  - refactor
dependencies: []
parent_task_id: TASK-71
priority: medium
ordinal: 90000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
WorklogMatching.Matches has a bool flag; production code only uses includeIssue: false. (Found in the TASK-71 review.)
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 No boolean parameter on Matches
- [ ] #2 WorklogSync behaviour unchanged; tests pass
<!-- AC:END -->
