---
id: TASK-71.34
title: One temp store fixture and fold tiny test helpers into TimeHarness
status: Done
assignee: []
created_date: '2026-10-29 07:52'
updated_date: '2026-10-29 08:35'
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
- [x] #1 One TempStore fixture with Reopen() replaces the hand-wired setups
- [x] #2 TestClock.cs, TestUsers.cs and TempDir.cs are folded into TimeHarness.cs (150 lines or fewer)
- [x] #3 Tests pass
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
TempStore (temp folder, FileDocumentStore, Folder(), Reopen(), Dispose) replaces TempDir and every hand-wired store; TestClock, TestUsers and TempStore now live in TimeHarness.cs (135 lines), which gained For(UserKey), Identity(CurrentUser) and Legacy so service tests (sign-in, calendar, retry queue, export values, data separation, legacy migration) go through the harness.
<!-- SECTION:FINAL_SUMMARY:END -->
