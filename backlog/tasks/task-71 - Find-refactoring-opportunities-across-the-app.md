---
id: TASK-71
title: Find refactoring opportunities across the app
status: Done
assignee: []
created_date: '2026-10-29 07:47'
updated_date: '2026-10-29 08:46'
labels:
  - refactor
dependencies: []
priority: high
ordinal: 75000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Fan out reviewers over every part of the app and tests, measured against PRINCIPLES.md. Each finding becomes its own solution task (subtasks of this one), and then all of them are done. Covers TASK-70.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 Every folder of src/JiraTime and the tests was reviewed against PRINCIPLES.md
- [x] #2 Each finding has its own task with a proposed solution and acceptance criteria
- [x] #3 All finding tasks are done; build and tests pass
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
Six reviewers covered Domain, Jira services, user/storage services, pages, sheet/shared UI and tests. 35 findings became TASK-71.1 to 71.35, plus TASK-71.36 for a store race found on the way; all done. 363 tests pass; export and Save all checked in the browser.
<!-- SECTION:FINAL_SUMMARY:END -->
