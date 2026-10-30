---
id: TASK-78.3
title: Confirm modal replaces the browser confirm()
status: Done
assignee: []
created_date: '2026-10-30 09:11'
updated_date: '2026-10-30 09:25'
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
- [x] #1 ConfirmAsync shows a title, text and a confirm and cancel button, with a danger style option
- [x] #2 Clear logs asks with the confirm modal
- [x] #3 Leaving the timesheet with unsaved changes inside the app asks with the confirm modal
- [x] #4 No JS confirm() calls are left
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
Modals.ConfirmAsync(title, text, confirmLabel, danger) shows ConfirmModal (small, title, text, Cancel and a confirm button; danger makes it red and starts the focus on Cancel). Clear logs and leaving the timesheet with unsaved changes inside the app ask with it; NavigationLock keeps the browser's own warning for closing or reloading the tab. No JS confirm() is left. The timesheet's keyboard shortcuts are off while a modal is open, and Ctrl+S never opens the browser's Save then. ModalHost lets a closed modal finish leaving before the next queued one opens, so the focus returns to the page first; modal.js puts it back on the opener if the browser can't.
<!-- SECTION:FINAL_SUMMARY:END -->
