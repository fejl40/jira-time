---
id: TASK-62
title: Read app links in one place
status: To Do
assignee: []
created_date: '2026-10-28 07:13'
labels:
  - refactor
dependencies: []
priority: low
ordinal: 62000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
MainLayout (is this the timesheet?) and the timesheet's leave guard (which period does a link go to?) each parse URLs. Links should both build and read timesheet links, with tests.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 Links reads a relative URL into the period and day it shows, or none
- [ ] #2 MainLayout and the leave guard use it
- [ ] #3 Tests cover the parsing
<!-- AC:END -->
