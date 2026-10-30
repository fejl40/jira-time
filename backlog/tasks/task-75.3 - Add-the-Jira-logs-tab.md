---
id: TASK-75.3
title: Add the Jira logs tab
status: To Do
assignee: []
created_date: '2026-10-30 07:50'
updated_date: '2026-10-30 07:56'
labels:
  - feature
dependencies:
  - TASK-75.1
  - TASK-75.2
  - TASK-75.4
  - TASK-75.5
parent_task_id: TASK-75
priority: medium
ordinal: 118000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
A Logs page in the top bar showing the signed-in user's Jira log, newest first, grouped by user action. Each request is one row with method, path, status, duration and time; a row expands into the detail view from TASK-75.4. Filters: day, errors only, text search. A Download button for the day's .jsonl and a Clear logs button. Big days stay fast (read lazily or page).
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 Logs tab in the top bar lists requests grouped by user action, newest first, with status colours
- [ ] #2 Each request row expands and collapses in place to show the TASK-75.4 detail view
- [ ] #3 Filter by day, errors only and text; download the day's .jsonl; clear logs
- [ ] #4 Users only ever see their own logs (tested)
- [ ] #5 Light and dark mode; checked in the browser against the mock Jira
- [ ] #6 While the tab is open, new requests appear live and in-progress ones update when they finish
- [ ] #7 A link from a toast opens the tab with that request expanded
<!-- AC:END -->

## Implementation Notes

<!-- SECTION:NOTES:BEGIN -->
Live updates (Carl, 2026-10-30): the Logs tab subscribes to the TASK-75.1 feed while open. New requests appear at once in their action group, requests in progress show a spinner and update when they finish. Opening a request from a toast (TASK-75.5) scrolls to and expands it.
<!-- SECTION:NOTES:END -->
