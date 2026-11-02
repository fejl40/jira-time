---
id: TASK-117
title: A bigger finish when a period becomes complete
status: To Do
assignee: []
created_date: '2026-11-02 18:25'
labels:
  - ui
  - wow
dependencies:
  - TASK-111
  - TASK-112
priority: medium
ordinal: 197000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Before the existing confetti, light sweeps across the grid column by column and the background (aurora or sky) glows for a moment.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 Off (or still) when the system asks for reduced motion
- [ ] #2 Looks right in light and dark mode, on phones and wide screens
- [ ] #3 Rules that decide what shows are in Domain or a plain class and covered by tests
- [ ] #4 data-* attributes let a browser check find it; checked in the browser
- [ ] #5 Build has 0 warnings, tests pass, wwwroot/app.css committed with the styles
<!-- AC:END -->
