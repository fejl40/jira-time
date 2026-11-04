---
id: TASK-121
title: Health check on Settings
status: Done
assignee: []
created_date: '2026-11-04 09:06'
updated_date: '2026-11-04 09:18'
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
- [x] #1 Settings shows the Health check with a Run button; each check shows passed, failed with a fix, or skipped
- [x] #2 No check changes anything in Jira
- [x] #3 Every request is logged under the Health check action and respects the rate limits
- [x] #4 Works in light and dark mode, with the Waiting spinner while running
- [x] #5 Build has 0 warnings, tests pass, wwwroot/app.css committed with the styles
<!-- AC:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
Settings has a Health check: token, reading worklogs, issue picker, logging time (permission only), time zone and clock, each passed, failed with a fix sentence, or skipped. Read-only, logged under Health check, through JiraHttp's rate limits. Icons pop in, a progress bar fills, the running row is washed blue, and a run where everything passed glows once; all still under reduced motion.
<!-- SECTION:FINAL_SUMMARY:END -->
