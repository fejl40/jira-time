---
id: TASK-75.4
title: Show each Jira request's details with expandable JSON
status: Done
assignee: []
created_date: '2026-10-30 07:53'
updated_date: '2026-10-30 08:17'
labels:
  - feature
dependencies:
  - TASK-75.1
parent_task_id: TASK-75
priority: medium
ordinal: 119000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Carl (2026-10-30): each request in the Logs tab expands to show more about it, and the request and response JSON can be expanded to see what actually happened. Detail view: full URL, method, status with reason, duration, start time, the user action, request and response headers (no token), content type and size, the error message, and truncation notes. Request body and response body each show as a collapsible JSON tree: objects and arrays fold and unfold, the top levels start open, with Expand all / Collapse all, Copy, and a raw-text fallback for bodies that aren't JSON (e.g. an HTML login page).
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 Expanding a request shows URL, method, status and reason, duration, time, user action, headers, content type, size and any error
- [x] #2 Request and response bodies show as JSON trees whose objects and arrays expand and collapse, with Expand all, Collapse all and Copy
- [x] #3 Bodies that are not JSON or were truncated show as raw text with a note
- [x] #4 Large responses (e.g. a 100-issue search) stay responsive: deep nodes render only when opened
- [ ] #5 Keyboard accessible, light and dark mode, checked in the browser
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
Components/Logs/RequestDetails shows URL, method, status and reason, time, duration, action (with id), content type, size, error, both header lists (folding, credentials never logged) and the two bodies. Components/Logs/JsonTree draws a body as a tree of buttons (aria-expanded, role=tree/treeitem) whose objects and arrays fold; the top two levels start open and children render only when their node is open, so big searches stay quick. Expand all, Collapse all and Copy (indented JSON, through the existing jiraExport.copy helper). Domain/JsonBody decides tree versus raw text: bodies that aren't JSON, or were cut at 64 KB, show as text with a note. Format.Duration and Format.Bytes write durations and sizes. Browser check is recorded with TASK-75.3, where the view is shown.
<!-- SECTION:FINAL_SUMMARY:END -->
