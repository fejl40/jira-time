---
id: TASK-74
title: Show progress while waiting for Jira
status: To Do
assignee: []
created_date: '2026-10-30 07:47'
updated_date: '2026-10-30 08:03'
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
- [ ] #5 The indicator is one shared component; busy states decided outside markup are tested
<!-- AC:END -->

## Implementation Notes

<!-- SECTION:NOTES:BEGIN -->
Overlap with TASK-75.5 (Carl, 2026-10-30): Jira request toasts show that Jira is busy in general, but this task's busy buttons and loading states where the user is looking are still needed.

Gaps found by another thread (folded in from branch claude/project-thread-4o3bnu): moving to another period keeps the old period on screen with no sign Jira is being read; the first load is a plain line of text; Sync, Save day, Save all, Try again and Connect only change their label and nothing marks the affected grid, day or row as pending; Settings (save work hours) and Export (load month, save values per epic, change month) have no busy state or disabled buttons. Wanted: one small shared 'waiting for Jira' indicator in Components/Shared that follows reduced motion, used wherever a request is in flight, with the starting button disabled.
<!-- SECTION:NOTES:END -->
