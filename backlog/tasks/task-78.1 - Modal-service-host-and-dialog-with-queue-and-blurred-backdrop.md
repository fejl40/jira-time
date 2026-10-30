---
id: TASK-78.1
title: 'Modal service, host and dialog with queue and blurred backdrop'
status: Done
assignee: []
created_date: '2026-10-30 09:11'
updated_date: '2026-10-30 09:19'
labels: []
dependencies: []
parent_task_id: TASK-78
ordinal: 124000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Components/Modals/: Modals (scoped per tab: ShowAsync<TModal,TResult>(parameters, onTop), queue, stack, close all on navigation), ModalHost (in MainLayout, renders the open modal(s) via DynamicComponent in <dialog>), Modal.razor (frame: title, body, footer slots; light and dark; full-width sheet on phones), wwwroot/modal.js (showModal/close, report Esc and backdrop clicks), styles in Styles/ for ::backdrop (blur + darken) and open/close motion that switches off with reduced motion. Queue rules in a tested class.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 ShowAsync returns the modal's result, or null when cancelled
- [x] #2 A modal asked for while another is open waits and opens when it closes, in order
- [x] #3 A modal opened on top shows above the current one and the one below comes back when it closes
- [x] #4 The backdrop is blurred and darkened, in light and dark mode
- [x] #5 Esc and a click on the backdrop cancel unless the modal forbids it
- [x] #6 Changing page closes open and queued modals as cancelled
- [x] #7 Tests cover results, cancel, queue order, on-top and close-all
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
Components/Modals: Modals service (scoped per tab, ShowAsync<TModal,TResult>, null/default when cancelled), ModalQueue (one open at a time, FIFO waiting list, onTop stacks, CloseAll), ModalContext (Close, Cancel, Busy, WhileBusyAsync) cascaded to the modal, ModalHost in MainLayout drawing each open modal in a native <dialog> via DynamicComponent, Modal.razor frame (title, body, footer; bottom sheet on phones). wwwroot/modal.js opens with showModal, reports Esc and backdrop clicks to the server, plays the closing animation. Backdrop blurred and darkened in light and dark; open/close motion in Styles/motion.css, off with reduced motion. Changing page closes every modal as cancelled. Tests in ModalsTests.
<!-- SECTION:FINAL_SUMMARY:END -->
