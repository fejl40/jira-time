---
id: TASK-71.6
title: Write JiraWorklogClient's worklog mapping and URL once
status: To Do
assignee: []
created_date: '2026-10-29 07:52'
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
- [ ] #1 `new JiraWorklog(` appears once
- [ ] #2 Worklog URL building appears once
- [ ] #3 TimeLogTests and SaveDayTests pass
<!-- AC:END -->
