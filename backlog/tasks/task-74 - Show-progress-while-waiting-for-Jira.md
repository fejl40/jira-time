---
id: TASK-74
title: Show progress while waiting for Jira
status: To Do
assignee: []
created_date: '2026-10-30 07:47'
updated_date: '2026-10-30 07:56'
labels:
  - ui
dependencies: []
priority: medium
ordinal: 114000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Carl (2026-10-30): when the app waits for Jira (loading a period or the export, saving a day, Save all, Sync, issue search, signing in), the UI should show that something is happening.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 Every wait on Jira shows feedback where the user is looking (spinner, busy button text or a loading bar), and buttons that would repeat the request are disabled meanwhile
- [ ] #2 Loading a period and the export page show a loading state instead of stale or empty content
- [ ] #3 Feedback appears only for waits long enough to notice (no flicker on fast responses)
- [ ] #4 Works in light and dark mode and with reduced motion; checked in the browser against a slow mock Jira
<!-- AC:END -->

## Implementation Notes

<!-- SECTION:NOTES:BEGIN -->
Overlap with TASK-75.5 (Carl, 2026-10-30): Jira request toasts show that Jira is busy in general, but this task's busy buttons and loading states where the user is looking are still needed.
<!-- SECTION:NOTES:END -->
