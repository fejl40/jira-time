---
id: TASK-111
title: Aurora background that follows the period's progress
status: To Do
assignee: []
created_date: '2026-11-02 18:25'
labels:
  - ui
  - wow
dependencies: []
priority: medium
ordinal: 176000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Soft blurred colour blobs drift slowly behind the page. Their colours follow how complete the shown period is so far: cool blue when little is logged, through green, to gold when nothing up to today is missing. CSS only (transforms and opacity), so it costs almost nothing to run.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 Off (or still) when the system asks for reduced motion
- [ ] #2 Looks right in light and dark mode, on phones and wide screens
- [ ] #3 Rules that decide what shows are in Domain or a plain class and covered by tests
- [ ] #4 data-* attributes let a browser check find it; checked in the browser
- [ ] #5 Build has 0 warnings, tests pass, wwwroot/app.css committed with the styles
<!-- AC:END -->
