---
id: TASK-71.6
title: Write JiraWorklogClient's worklog mapping and URL once
status: Done
assignee: []
created_date: '2026-10-29 07:52'
updated_date: '2026-10-29 08:02'
labels:
  - refactor
dependencies: []
parent_task_id: TASK-71
priority: medium
ordinal: 81000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
The RawJiraWorklog to JiraWorklog mapping is written twice and the worklog URL built five times. (Found in the TASK-71 review.)
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 `new JiraWorklog(` appears once
- [x] #2 Worklog URL building appears once
- [x] #3 TimeLogTests and SaveDayTests pass
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
JiraWorklogClient maps raw worklogs in one ToWorklog method and builds worklog URLs in one WorklogUrl method.
<!-- SECTION:FINAL_SUMMARY:END -->
