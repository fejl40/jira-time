---
id: TASK-71.2
title: Inject TimeProvider where services call DateTime.UtcNow
status: Done
assignee: []
created_date: '2026-10-29 07:52'
updated_date: '2026-10-29 07:55'
labels:
  - refactor
dependencies: []
parent_task_id: TASK-71
priority: medium
ordinal: 77000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
WorklogSync (UpdatedUtc), JiraIdentity (LastVerifiedUtc) and LegacyDataMigration (backup ids and times) call DateTime.UtcNow, which PRINCIPLES.md forbids. (Found in the TASK-71 review.)
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 No DateTime.UtcNow or DateTime.Now in Services
- [x] #2 TimeHarness and other tests pass their TestClock
- [x] #3 A test asserts UpdatedUtc equals the test clock after a save
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
WorklogSync, JiraIdentity and LegacyDataMigration take a TimeProvider instead of calling DateTime.UtcNow; tests pass a TestClock and a new test checks UpdatedUtc after a save.
<!-- SECTION:FINAL_SUMMARY:END -->
