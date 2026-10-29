---
id: TASK-71.3
title: Write the unknown-key search fallback once in JiraClient
status: To Do
assignee: []
created_date: '2026-10-29 07:52'
labels:
  - refactor
dependencies: []
parent_task_id: TASK-71
priority: medium
ordinal: 78000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
GetIssuesAsync and ReadEpicsAsync both carry the same 'one unknown key fails the query, retry one by one' try/catch. (Found in the TASK-71 review.)
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 The fallback catch appears once in JiraClient.cs
- [ ] #2 EpicLookupTests and status lookup tests pass unchanged
<!-- AC:END -->
