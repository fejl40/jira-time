---
id: TASK-113
title: Frosted glass cards with a light that follows the mouse
status: Done
assignee: []
created_date: '2026-11-02 18:25'
updated_date: '2026-11-04 09:25'
labels:
  - ui
  - wow
dependencies: []
priority: medium
ordinal: 184000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Cards become slightly see-through over the background (backdrop blur), and a soft light follows the mouse across the grid and cards, like a torch under frosted glass.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 Off (or still) when the system asks for reduced motion
- [x] #2 Looks right in light and dark mode, on phones and wide screens
- [x] #3 Rules that decide what shows are in Domain or a plain class and covered by tests
- [x] #4 data-* attributes let a browser check find it; checked in the browser
- [x] #5 Build has 0 warnings, tests pass, wwwroot/app.css committed with the styles
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
Frosted glass over Aurora and Sky (Backdrop.UsesGlass, tested) and a light that follows the mouse. data-glass and data-lit for checks; checked light/dark, desktop/phone, modals, touch and reduced motion.
<!-- SECTION:FINAL_SUMMARY:END -->
