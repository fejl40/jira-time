---
id: TASK-92
title: Slide in a period reached through Leave without saving
status: Done
assignee: []
created_date: '2026-10-30 13:00'
updated_date: '2026-10-30 13:10'
labels:
  - bug
dependencies: []
priority: low
ordinal: 157000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
A new period slides in from the side it comes from (ShownPeriod.Slide, Motion.Slide). When the user has unsaved changes and moves to another period, the Leave without saving? modal asks first; after Leave the new period appears without the slide. Find why (for example the modal closing, or the order of navigation and render) and make it slide like any other period change.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 A period reached by confirming Leave without saving? slides in from the side it comes from, like any other period change
- [x] #2 Staying (Cancel) keeps the period on screen with no slide
- [x] #3 Reduced motion still turns the slide off
- [x] #4 A test covers the slide direction after leaving, where the cause allows one
- [x] #5 Browser check: ‹ and › with unsaved changes, then Leave, show the slide
- [x] #6 dotnet build has 0 warnings and dotnet test passes
- [x] #7 Modals.ShowAsync returns once the dialog has finished closing (immediately when it was never shown, e.g. a waiting modal cancelled or no host)
<!-- AC:END -->

## Implementation Notes

<!-- SECTION:NOTES:BEGIN -->
Cause: Modals.ShowAsync returns as soon as Leave is pressed, while the dialog still plays its closing animation (150 ms, blurred backdrop fading). The navigation and the new period start at once, so most of the slide (300 ms, ease-out) plays behind the fading backdrop. Fix: a modal's caller gets its result once the dialog has finished closing, as ShowAsync's summary already promises; then the period opens on a clear page. This applies to every modal (Save all, time off, confirms), which now continue up to 150 ms later, after the dialog is gone.
<!-- SECTION:NOTES:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
Cause: Modals.ShowAsync returned as soon as Leave was pressed, so the new period slid in behind the dialog's fading backdrop. Now a modal the browser showed returns its result only once ModalHost reports its dialog has left the page (ModalContext.Shown/Left/Gone); a modal never shown returns at once. The period then slides in on a clear page (browser check: the slide animation starts right after the dialog is gone, before it started while the dialog was still open). Cancel never navigates, so no slide; reduced motion makes both the closing animation and the slide instant as before. Every modal's caller now continues after the closing animation (up to 150 ms later). Two tests in ModalsTests; the timesheet browser script gives the same output as before; AGENTS.md Modals updated.
<!-- SECTION:FINAL_SUMMARY:END -->
