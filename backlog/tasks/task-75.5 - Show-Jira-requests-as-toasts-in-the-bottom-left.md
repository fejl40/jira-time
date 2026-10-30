---
id: TASK-75.5
title: Show Jira requests as toasts in the bottom left
status: To Do
assignee: []
created_date: '2026-10-30 07:56'
labels:
  - feature
dependencies:
  - TASK-75.1
parent_task_id: TASK-75
priority: medium
ordinal: 120000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Carl (2026-10-30): every Jira request shows as a toast in the bottom left corner, newest at the bottom and oldest on top, fading out over time. A component in MainLayout subscribes to the TASK-75.1 live feed for the signed-in user while the tab is open, pushed over the Blazor Server circuit. Each toast shows the user action, method and short path, a spinner while waiting, then a green or red status with the time taken. Toasts fade after about 5 seconds; errors stay about 15 seconds and can be closed; hovering pauses the fade. Clicking a toast opens the Logs tab with that request expanded. Bursts (e.g. Save all) show at most about 5 toasts plus a '+N more' line.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 Every Jira request shows a toast: newest at the bottom, oldest on top, spinner while waiting, then status and time taken
- [ ] #2 Toasts fade after about 5 s, errors after about 15 s or when closed; hover pauses the fade; reduced motion removes them without animation
- [ ] #3 Clicking a toast opens the Logs tab with that request expanded
- [ ] #4 Bursts show at most about 5 toasts plus a '+N more' line; other users' requests never show
- [ ] #5 Toasts don't cover the day editor's buttons on narrow screens; light and dark mode; checked in the browser
<!-- AC:END -->
