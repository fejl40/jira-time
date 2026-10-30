---
id: TASK-75.3
title: Add the Jira logs tab
status: To Do
assignee: []
created_date: '2026-10-30 07:50'
labels:
  - feature
dependencies:
  - TASK-75.1
  - TASK-75.2
parent_task_id: TASK-75
priority: medium
ordinal: 118000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
A Logs page in the top bar showing the signed-in user's Jira log, newest first, grouped by user action. Each request shows method, path, status, duration and time. Opening one shows the request and response bodies, pretty-printed JSON. Filters: day, errors only, text search. A Download button for the day's .jsonl and a Clear logs button. Big days stay fast (read lazily or page).
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 Logs tab in the top bar lists requests grouped by user action, newest first, with status colours
- [ ] #2 A request opens to show request and response bodies, formatted
- [ ] #3 Filter by day, errors only and text; download the day's .jsonl; clear logs
- [ ] #4 Users only ever see their own logs (tested)
- [ ] #5 Light and dark mode; checked in the browser against the mock Jira
<!-- AC:END -->
