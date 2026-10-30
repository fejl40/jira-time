---
id: TASK-78.3
title: Confirm modal replaces the browser confirm()
status: To Do
assignee: []
created_date: '2026-10-30 09:11'
labels: []
dependencies:
  - TASK-78.2
parent_task_id: TASK-78
ordinal: 126000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Modals.ConfirmAsync(title, text, confirmLabel, danger) built on the modal system. Use it for Clear logs and for leaving the timesheet with unsaved changes inside the app. Closing or reloading the tab keeps the browser's own warning (beforeunload can't be custom).
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 ConfirmAsync shows a title, text and a confirm and cancel button, with a danger style option
- [ ] #2 Clear logs asks with the confirm modal
- [ ] #3 Leaving the timesheet with unsaved changes inside the app asks with the confirm modal
- [ ] #4 No JS confirm() calls are left
<!-- AC:END -->
