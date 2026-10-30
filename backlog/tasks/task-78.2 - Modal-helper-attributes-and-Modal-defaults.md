---
id: TASK-78.2
title: 'Modal helper attributes and [Modal] defaults'
status: Done
assignee: []
created_date: '2026-10-30 09:11'
updated_date: '2026-10-30 09:19'
labels: []
dependencies:
  - TASK-78.1
parent_task_id: TASK-78
ordinal: 125000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Behaviour a custom modal declares in markup, handled by modal.js and ModalHost: data-modal-close (cancel), data-modal-result="x" (close with x), data-modal-autofocus (focus on open), data-modal-busy (buttons off and Waiting spinner while true). Per-modal defaults as a class attribute: [Modal(Size = Small|Medium|Large, Dismiss = true|false)].
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 data-modal-close and data-modal-result close the modal with no result or the given value
- [x] #2 data-modal-autofocus gets focus when the modal opens
- [x] #3 data-modal-busy disables the modal's buttons and dismissal and shows the Waiting spinner
- [x] #4 [Modal] sets the size and whether Esc and backdrop clicks close it
- [x] #5 Tests cover the attribute defaults and result handling
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
modal.js handles data-modal-close (cancel), data-modal-result="x" (close with x, read as the caller's result type by ModalResult: bool, numbers, enums, strings), data-modal-autofocus (focused on open) and data-modal-busy (ModalHost writes it on the dialog while ModalContext.Busy; the script then ignores Esc, backdrop and close buttons, and the Modal frame disables its fieldset and shows the Waiting spinner). [Modal(Size = Small|Medium|Large, Dismiss = true|false)] sets the width (modal-small / modal-large) and whether Esc and the backdrop cancel. Tests cover the defaults and result handling.
<!-- SECTION:FINAL_SUMMARY:END -->
