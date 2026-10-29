---
id: TASK-66.1
title: Read each issue's epic from Jira
status: Done
assignee: []
created_date: '2026-10-29 07:36'
updated_date: '2026-10-29 07:37'
labels:
  - feature
dependencies: []
parent_task_id: TASK-66
priority: high
ordinal: 67000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
For the month export: find the epic of each logged issue (parent epic, Epic Link field, or a sub-task's parent's epic), cached briefly.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 Epic comes from the parent when it is an epic, else from the Epic Link field
- [x] #2 A sub-task gets its parent's epic
- [x] #3 Issues Jira can't read or with no epic get none
- [x] #4 Tests cover the lookup
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
JiraClient.GetEpicsAsync and GetEpicLinkFieldAsync read the epic from the parent epic, the Epic Link field, or a sub-task's parent; JiraIssueLookup.GetEpicsAsync caches per key. EpicLookupTests.
<!-- SECTION:FINAL_SUMMARY:END -->
