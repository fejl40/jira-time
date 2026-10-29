---
id: TASK-71.34
title: One temp store fixture and fold tiny test helpers into TimeHarness
status: To Do
assignee: []
created_date: '2026-10-29 07:52'
labels:
  - refactor
dependencies: []
parent_task_id: TASK-71
priority: medium
ordinal: 109000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Seven classes hand-wire a temp folder and store; TestClock, TestUsers and TempDir are tiny separate files. (Found in the TASK-71 review.)
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 One TempStore fixture with Reopen() replaces the hand-wired setups
- [ ] #2 TestClock.cs, TestUsers.cs and TempDir.cs are folded into TimeHarness.cs (150 lines or fewer)
- [ ] #3 Tests pass
<!-- AC:END -->
