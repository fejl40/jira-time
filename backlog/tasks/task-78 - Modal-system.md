---
id: TASK-78
title: Modal system
status: Done
assignee: []
created_date: '2026-10-30 09:11'
updated_date: '2026-10-30 09:29'
labels: []
dependencies: []
ordinal: 123000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
One way to show modals across the app, built on the native <dialog> element (top layer, focus trap, inert page, Esc, focus returns to the opener). A per-tab Modals service opens a modal component and awaits its result; a ModalHost in MainLayout shows it; Modal.razor is the frame custom modals use. The backdrop is blurred and darkened. Several modals can be queued: a modal asked for while one is open waits and shows when the open one closes, unless it is opened on top of the current one (a modal opened from inside a modal).
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 Custom modals open through one service and return a result, null when cancelled
- [x] #2 Modals use the native dialog element with a blurred, darkened backdrop
- [x] #3 Modals asked for while one is open are queued and shown in order
- [x] #4 Helper attributes and a [Modal] class attribute cover closing, results, focus, busy state, size and dismissal
- [x] #5 The browser confirm() calls and Time off use the modal system
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
One modal system for the app (Components/Modals): a per-tab Modals service with a queue and modals on top, ModalHost in MainLayout drawing native <dialog>s on a blurred, darkened backdrop, the Modal frame, helper attributes and [Modal] defaults, ConfirmAsync replacing the browser confirm(), and Time off as a modal.
<!-- SECTION:FINAL_SUMMARY:END -->
