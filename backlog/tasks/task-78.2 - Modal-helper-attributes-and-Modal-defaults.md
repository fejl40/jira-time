---
id: TASK-78.2
title: 'Modal helper attributes and [Modal] defaults'
status: To Do
assignee: []
created_date: '2026-10-30 09:11'
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
- [ ] #1 data-modal-close and data-modal-result close the modal with no result or the given value
- [ ] #2 data-modal-autofocus gets focus when the modal opens
- [ ] #3 data-modal-busy disables the modal's buttons and dismissal and shows the Waiting spinner
- [ ] #4 [Modal] sets the size and whether Esc and backdrop clicks close it
- [ ] #5 Tests cover the attribute defaults and result handling
<!-- AC:END -->
