---
id: TASK-71.27
title: Name repeated styles in app.css
status: To Do
assignee: []
created_date: '2026-10-29 07:52'
labels:
  - refactor
dependencies: []
parent_task_id: TASK-71
priority: medium
ordinal: 102000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Long class strings for the amber notice, panel surface, issue dot, status dots and light/dark text colours are copied across components. (Found in the TASK-71 review.)
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 Each repeated string lives once in app.css as a named class or utility
- [ ] #2 The pages look the same in light and dark mode
- [ ] #3 Build passes
<!-- AC:END -->
