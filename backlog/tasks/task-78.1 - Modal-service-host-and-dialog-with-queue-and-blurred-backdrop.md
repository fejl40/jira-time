---
id: TASK-78.1
title: 'Modal service, host and dialog with queue and blurred backdrop'
status: To Do
assignee: []
created_date: '2026-10-30 09:11'
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
- [ ] #1 ShowAsync returns the modal's result, or null when cancelled
- [ ] #2 A modal asked for while another is open waits and opens when it closes, in order
- [ ] #3 A modal opened on top shows above the current one and the one below comes back when it closes
- [ ] #4 The backdrop is blurred and darkened, in light and dark mode
- [ ] #5 Esc and a click on the backdrop cancel unless the modal forbids it
- [ ] #6 Changing page closes open and queued modals as cancelled
- [ ] #7 Tests cover results, cancel, queue order, on-top and close-all
<!-- AC:END -->
