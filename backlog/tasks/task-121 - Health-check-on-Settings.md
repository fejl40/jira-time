---
id: TASK-121
title: Health check on Settings
status: To Do
assignee: []
created_date: '2026-11-04 09:06'
labels:
  - jira
  - settings
dependencies: []
priority: medium
ordinal: 210000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Settings gets a Health check section that asks Jira, on demand, whether everything the app needs works: the token, reading the user's worklogs, the issue picker's searches, writing worklogs (permission only, nothing is written), and the time zone and clock. Each check shows a green tick or a plain sentence saying what is wrong and how to fix it. Requests go through JiraHttp and are logged under a Health check action.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 Settings shows the Health check with a Run button; each check shows passed, failed with a fix, or skipped
- [ ] #2 No check changes anything in Jira
- [ ] #3 Every request is logged under the Health check action and respects the rate limits
- [ ] #4 Works in light and dark mode, with the Waiting spinner while running
- [ ] #5 Build has 0 warnings, tests pass, wwwroot/app.css committed with the styles
<!-- AC:END -->
