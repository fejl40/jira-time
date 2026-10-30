---
id: TASK-75.1
title: Log every Jira request and response to JSONL
status: To Do
assignee: []
created_date: '2026-10-30 07:50'
updated_date: '2026-10-30 07:53'
labels:
  - feature
dependencies: []
parent_task_id: TASK-75
priority: medium
ordinal: 116000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
One place sees every Jira call: JiraHttp (or a DelegatingHandler on the JiraClient and JiraWorklogClient HttpClients). Write one JSON line per call: time, id, owner, user action (TASK-75.2), method, path and query, request body, status, response body, duration, and the error message for failures and timeouts. Files: data/logs/jira/<owner>/yyyy-MM-dd.jsonl, one per user per day, appended safely when requests run in parallel. Never write the token or the Authorization header. Cap large bodies (e.g. 64 KB) and mark them truncated. Delete files older than 30 days.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 Every Jira call, including failures, timeouts and unreachable Jira, writes one JSON line with the fields above
- [ ] #2 No token or Authorization header ever appears in a log file (tested)
- [ ] #3 Files are per user per day under data/logs/jira/, parallel writes don't corrupt lines, files older than 30 days are deleted
- [ ] #4 Large bodies are capped and marked truncated
- [ ] #5 Tests through TimeHarness and FakeJira; AGENTS.md 'Where data lives' updated
- [ ] #6 Also logged: full URL, status reason, request and response headers without Authorization or cookies, content type, response size, truncated flags
<!-- AC:END -->

## Implementation Notes

<!-- SECTION:NOTES:BEGIN -->
Carl (2026-10-30) wants each request expandable with more detail (TASK-75.4), so also log: full URL, status reason phrase, request and response headers (Authorization and cookies removed), content type, response size in bytes, and whether each body was truncated.
<!-- SECTION:NOTES:END -->
