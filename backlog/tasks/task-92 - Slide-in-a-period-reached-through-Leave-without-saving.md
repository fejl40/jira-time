---
id: TASK-92
title: Slide in a period reached through Leave without saving
status: To Do
assignee: []
created_date: '2026-10-30 13:00'
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
- [ ] #1 A period reached by confirming Leave without saving? slides in from the side it comes from, like any other period change
- [ ] #2 Staying (Cancel) keeps the period on screen with no slide
- [ ] #3 Reduced motion still turns the slide off
- [ ] #4 A test covers the slide direction after leaving, where the cause allows one
- [ ] #5 Browser check: ‹ and › with unsaved changes, then Leave, show the slide
- [ ] #6 dotnet build has 0 warnings and dotnet test passes
<!-- AC:END -->
