---
id: TASK-71.1
title: Split WorklogSync and move day-save rules into Domain
status: To Do
assignee: []
created_date: '2026-10-29 07:52'
labels:
  - refactor
dependencies: []
parent_task_id: TASK-71
priority: medium
ordinal: 76000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
WorklogSync.cs is 258 lines and mixes planning a day save with writing entries through the retry queue. Move validation, the 24h total and the gone/changed/touched diff into a Domain method (e.g. DayWork.PlanSave) and add TimeEntry.WorklogIssueKey and MovedToAnotherIssue for the repeated `JiraWorklogIssueKey ?? IssueKey` rule. Remove the redundant return in ChangedInJiraAsync. (Found in the TASK-71 review.)
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 WorklogSync.cs is at most 200 lines; methods about 30 lines
- [ ] #2 The save plan lives in Domain with direct tests
- [ ] #3 `?? entry.IssueKey` no longer appears in WorklogSync
- [ ] #4 SaveDayTests and RetryQueueTests pass unchanged
<!-- AC:END -->
