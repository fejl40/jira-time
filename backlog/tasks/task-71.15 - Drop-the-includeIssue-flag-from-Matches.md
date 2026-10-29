---
id: TASK-71.15
title: Drop the includeIssue flag from Matches
status: Done
assignee: []
created_date: '2026-10-29 07:52'
updated_date: '2026-10-29 07:56'
labels:
  - refactor
dependencies: []
parent_task_id: TASK-71
priority: medium
ordinal: 90000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
WorklogMatching.Matches has a bool flag; production code only uses includeIssue: false. (Found in the TASK-71 review.)
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 No boolean parameter on Matches
- [x] #2 WorklogSync behaviour unchanged; tests pass
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
Matches now always compares day, hours and comment only, the way WorklogSync used it; the flag and its issue-key tests are gone.
<!-- SECTION:FINAL_SUMMARY:END -->
