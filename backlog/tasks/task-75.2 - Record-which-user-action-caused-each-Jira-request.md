---
id: TASK-75.2
title: Record which user action caused each Jira request
status: To Do
assignee: []
created_date: '2026-10-30 07:50'
labels:
  - feature
dependencies:
  - TASK-75.1
parent_task_id: TASK-75
priority: medium
ordinal: 117000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
An ambient action scope (e.g. AsyncLocal) that the UI opens around each user action, with a readable name and an id that groups its requests: open a period, change period, Sync now, Save day (with the date), Save all, Fill period, issue search (with the query), pick an issue (status lookup), open the export (with the month), sign in and connect. Requests outside any action are logged as 'Background'.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 Every user action that reaches Jira opens a scope with a readable name and an id shared by its requests
- [ ] #2 Requests from parallel actions and from other users' circuits never get the wrong action
- [ ] #3 Tests cover the scope flowing through async calls and the Background fallback
<!-- AC:END -->
