---
id: TASK-78
title: Modal system
status: In Progress
assignee: []
created_date: '2026-10-30 09:11'
updated_date: '2026-10-30 09:14'
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
- [ ] #1 Custom modals open through one service and return a result, null when cancelled
- [ ] #2 Modals use the native dialog element with a blurred, darkened backdrop
- [ ] #3 Modals asked for while one is open are queued and shown in order
- [ ] #4 Helper attributes and a [Modal] class attribute cover closing, results, focus, busy state, size and dismissal
- [ ] #5 The browser confirm() calls and Time off use the modal system
<!-- AC:END -->
