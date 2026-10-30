---
id: TASK-75
title: Jira request log tab
status: To Do
assignee: []
created_date: '2026-10-30 07:50'
updated_date: '2026-10-30 07:56'
labels:
  - feature
dependencies: []
priority: medium
ordinal: 115000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Carl (2026-10-30): a tab with detailed logs of every Jira API request and response, each with the user action that caused it. Each request expands to show its details, with the request and response JSON expandable. Logs are stored locally as JSONL. Every request also shows live as a toast in the bottom left, and the Logs tab updates live, both pushed from the server over the Blazor circuit. Done through TASK-75.1 to TASK-75.5.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 Every Jira request and response is logged to local JSONL with the user action that caused it
- [ ] #2 A Logs tab shows them so a request can be traced back to what the user did
- [ ] #3 Subtasks TASK-75.1 to TASK-75.5 are done
<!-- AC:END -->
