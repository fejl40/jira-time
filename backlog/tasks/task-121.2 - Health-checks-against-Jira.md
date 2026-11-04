---
id: TASK-121.2
title: Health checks against Jira
status: Done
assignee: []
created_date: '2026-11-04 09:06'
updated_date: '2026-11-04 09:17'
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
- [x] #1 Each check passes and fails as expected against FakeJira
- [x] #2 A missing permission or a 401 gives the fix sentence
- [x] #3 Requests run under the Health check JiraAction
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
Services/JiraHealth runs the checks one by one through JiraClient/JiraWorklogClient (JiraHttp): /myself with the profile time zone, the worklog search of the last 7 days, the sprint search and the issue picker, mypermissions?permissions=WORK_ON_ISSUES (on an issue found earlier), the browser time zone against the Jira profile's, and serverInfo against the server clock. Only GETs, all under the Health check action. JiraHealthTests covers passes, 401, 403, a missing permission, a moved day, a skewed clock, Jira offline, a 429 waited out and the log action.
<!-- SECTION:FINAL_SUMMARY:END -->
