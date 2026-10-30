---
id: TASK-74
title: Show loading feedback while Jira requests run
status: To Do
assignee: []
created_date: '2026-10-30 07:50'
labels:
  - ux
dependencies: []
priority: medium
ordinal: 114000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Carl (2026-10-30): when waiting for Jira requests to fire there should be UI feedback.

Today some waits only swap a button's text ("Saving…", "Checking…", "Searching Jira…") and some show nothing at all. Gaps found:

- Moving to another period (‹ Today ›, links) keeps the old period on screen with no sign that Jira is being read, until the new one swaps in.
- The first load is a plain line of text ("Reading your time from Jira…").
- Sync, Save day, Save all, Try again and Connect only change their label; nothing marks the affected grid, day or row as pending.
- Settings (save work hours) and Export (load month, save values per epic, change month) run requests without a busy state or disabled buttons.

Wanted: one small, shared way to show "waiting for Jira" (a spinner or progress state in Components/Shared, quick and respecting reduced motion like the rest of Styles/motion.css), used wherever a Jira request is in flight, with the button that started it disabled so it can't fire twice.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 Changing period shows that Jira is being read (the current grid dims or a progress state shows) until the new period is loaded
- [ ] #2 Save day, Save all, Sync, Try again and Connect show a spinner and are disabled while their request runs
- [ ] #3 Settings and Export show a busy state and disable their buttons while loading or saving
- [ ] #4 The indicator is one shared component and follows reduced motion
- [ ] #5 Tests cover the busy states where they are decided outside markup
- [ ] #6 Checked in the browser, light and dark
<!-- AC:END -->
