---
id: TASK-75.2
title: Record which user action caused each Jira request
status: Done
assignee: []
created_date: '2026-10-30 07:50'
updated_date: '2026-10-30 08:15'
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
- [x] #1 Every user action that reaches Jira opens a scope with a readable name and an id shared by its requests
- [x] #2 Requests from parallel actions and from other users' circuits never get the wrong action
- [x] #3 Tests cover the scope flowing through async calls and the Background fallback
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
JiraAction is an AsyncLocal scope with a readable name and an id; JiraAction.Start inside a running action joins it, so a reload after Save day or Save all stays in that action. The recorder stamps each request with the current action (Background outside any). Scopes: Open period (PeriodLoader, covers open/change period and reloads), Save day <date> and Search issues “query” and Pick issue <key> (DayEditor), Save all and Sync now (Timesheet), Time off / Remove time off <date> (DayPanel, which reload from Jira), Open export <month> / Try again: export <month> (ExportPage), Sign in and Connect to Jira (SignInSession). Fill period and Settings never reach Jira. Requests made while signing in wait for their action to learn the owner (CurrentUser.Set claims it) and go to signed-out when Jira refuses the token. Tests: JiraActionTests (flow through awaits and Task.Run, joining, Background, parallel actions in separate tabs, sign-in owner).
<!-- SECTION:FINAL_SUMMARY:END -->
