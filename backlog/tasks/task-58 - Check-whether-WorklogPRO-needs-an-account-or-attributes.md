---
id: TASK-58
title: Check whether WorklogPRO needs an account or attributes
status: To Do
assignee: []
created_date: '2026-10-27 14:02'
labels:
  - jira-source-of-truth
dependencies: []
priority: medium
ordinal: 58000
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
WorklogPRO reads standard Jira worklogs, and stores its extras (account, attributes) in the worklogpro worklog property. Check on the real Jira whether worklogs saved here show in WorklogPRO and whether an account or attribute is required. If it is, let the user pick it and send it with each worklog.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [ ] #1 Confirmed on the real Jira whether app worklogs show in WorklogPRO
- [ ] #2 If WorklogPRO requires an account or attribute, the app lets the user set it and sends it in the worklogpro property, with tests
<!-- AC:END -->
