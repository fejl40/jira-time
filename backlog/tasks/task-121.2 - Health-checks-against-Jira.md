---
id: TASK-121.2
title: Health checks against Jira
status: To Do
assignee: []
created_date: '2026-11-04 09:06'
labels:
  - jira
  - settings
dependencies: []
parent_task_id: TASK-121
priority: medium
ordinal: 212000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
A service runs the checks: myself (token and user), a worklog search for the last week, the sprint and picker searches, the worklog permission (mypermissions WORK_ON_ISSUES / ADD_WORKLOG style check, nothing written), and the browser time zone against the server clock. Tested with TimeHarness and FakeJira, including failures.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 Each check passes and fails as expected against FakeJira
- [ ] #2 A missing permission or a 401 gives the fix sentence
- [ ] #3 Requests run under the Health check JiraAction
<!-- AC:END -->
