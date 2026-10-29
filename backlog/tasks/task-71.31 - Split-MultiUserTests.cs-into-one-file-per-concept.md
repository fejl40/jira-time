---
id: TASK-71.31
title: Split MultiUserTests.cs into one file per concept
status: Done
assignee: []
created_date: '2026-10-29 07:52'
updated_date: '2026-10-29 08:41'
labels:
  - refactor
dependencies: []
parent_task_id: TASK-71
priority: medium
ordinal: 106000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
360 lines with four unrelated classes. (Found in the TASK-71 review.)
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 MultiUserTests.cs is gone; each new file holds one class of 250 lines or fewer
- [x] #2 No test declares its own jira.example.com connection
- [x] #3 Test count unchanged
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
MultiUserTests.cs is split into UsersTests.cs (UserKey and UserAccount, one class), JiraIdentityTests.cs, DataSeparationTests.cs and LegacyDataMigrationTests.cs, all under 135 lines; connections come from TimeHarness.Server and the test count is unchanged.
<!-- SECTION:FINAL_SUMMARY:END -->
