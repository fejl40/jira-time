---
id: TASK-71.12
title: Fold TimeProviderExtensions into Domain/Period.cs
status: To Do
assignee: []
created_date: '2026-10-29 07:52'
labels:
  - refactor
dependencies: []
parent_task_id: TASK-71
priority: medium
ordinal: 87000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
A 7-line file holds only Clock.Today(). (Found in the TASK-71 review.)
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 TimeProviderExtensions.cs is gone and Clock.Today() still compiles everywhere
- [ ] #2 Build and tests pass
<!-- AC:END -->
